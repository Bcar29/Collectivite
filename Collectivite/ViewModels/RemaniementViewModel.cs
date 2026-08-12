using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion des remaniements budgétaires
    /// </summary>
    public class RemaniementViewModel : ViewModelBase
    {
        private string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";
        private bool _isLoading;
        private bool _isDialogOpen;
        private Remaniement _dialogRemaniement;
        private BudgetLine? _selectedBudgetLine;
        private int _selectedTabIndex;
        private bool _isDetailDialogOpen;
        private BudgetLine? _detailBudgetLine;

        private readonly List<BudgetLine> _allBudgetLines = new();

        // Pour la hiérarchie
        private List<BudgetLineHierarchyViewModel> _fullHierarchy = new();
        private readonly ExerciceService _exerciceService = ExerciceService.Instance;

        public RemaniementViewModel()
        {
            _dialogRemaniement = new Remaniement
            {
                Date = DateTime.Now,
                Montant = 0,
                Motif = "",
                TypeRemaniement = TypeRemaniement.en_plus
            };

            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand<BudgetLine>(async line => await OpenAddDialogAsync(line), line => line != null);
            OpenDetailsCommand = new RelayCommand<BudgetLine>(async line => await OpenDetailsDialogAsync(line), line => line != null);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Remaniement>(async rem => await DeleteAsync(rem));
            CloseDetailsCommand = new RelayCommand(_ => CloseDetailsDialog());
            ToggleExpandCommand = new RelayCommand<BudgetLineHierarchyViewModel>(ToggleExpand);

            LoadDataCommand.Execute(null);
        }

        // Permissions dynamiques
        public bool CanViewRemaniement => SessionManager.HasPermission("Remaniement.View");

        /// <summary>
        /// On peut créer un remaniement seulement si :
        /// - l'utilisateur a la permission Remaniement.Create
        /// - l'exercice courant n'est pas clôturé
        /// </summary>
        public bool CanCreateRemaniement =>
            SessionManager.HasPermission("Remaniement.Create") &&
            (_exerciceService.CurrentExercice?.EstCloture != true);

        public bool CanDeleteRemaniement => SessionManager.HasPermission("Remaniement.Delete");

        #region Collections exposées

        public ObservableCollection<BudgetLineHierarchyViewModel> BudgetLines { get; } = new();
        public ObservableCollection<Remaniement> DetailRemaniements { get; } = new();

        #endregion

        #region Propriétés bindables

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Remaniement DialogRemaniement
        {
            get => _dialogRemaniement;
            set => SetProperty(ref _dialogRemaniement, value);
        }

        public BudgetLine? SelectedBudgetLine
        {
            get => _selectedBudgetLine;
            set
            {
                SetProperty(ref _selectedBudgetLine, value);
                if (value != null)
                {
                    DialogRemaniement.IdBudgetLine = value.Id;
                    OnPropertyChanged(nameof(DialogRemaniement));
                }
                OnPropertyChanged(nameof(DialogTitle));
            }
        }

        public string DialogTitle
        {
            get
            {
                if (SelectedBudgetLine?.Nommenclature != null)
                {
                    return $"Remaniement - {SelectedBudgetLine.Nommenclature.Intitule}";
                }
                return "Nouveau remaniement";
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    ApplyFilter();
                }
            }
        }

        public decimal TotalRemaniements => _allBudgetLines.Sum(bl => bl.Remaniements?.Count ?? 0);
        public decimal TotalVariation => _allBudgetLines.Sum(bl => bl.VariationTotale);

        public bool IsDetailDialogOpen
        {
            get => _isDetailDialogOpen;
            set => SetProperty(ref _isDetailDialogOpen, value);
        }

        public BudgetLine? DetailBudgetLine
        {
            get => _detailBudgetLine;
            set => SetProperty(ref _detailBudgetLine, value);
        }

        #endregion

        #region Propriétés de totaux (3 valeurs par catégorie: Prévu, Variation, Définitif)

        // Recette Fonctionnement
        private decimal _totalRecetteFonctionnementPrevu;
        public decimal TotalRecetteFonctionnementPrevu
        {
            get => _totalRecetteFonctionnementPrevu;
            set => SetProperty(ref _totalRecetteFonctionnementPrevu, value);
        }

        private decimal _totalRecetteFonctionnementVariation;
        public decimal TotalRecetteFonctionnementVariation
        {
            get => _totalRecetteFonctionnementVariation;
            set => SetProperty(ref _totalRecetteFonctionnementVariation, value);
        }

        private decimal _totalRecetteFonctionnementDefinitif;
        public decimal TotalRecetteFonctionnementDefinitif
        {
            get => _totalRecetteFonctionnementDefinitif;
            set => SetProperty(ref _totalRecetteFonctionnementDefinitif, value);
        }

        // Recette Investissement
        private decimal _totalRecetteInvestissementPrevu;
        public decimal TotalRecetteInvestissementPrevu
        {
            get => _totalRecetteInvestissementPrevu;
            set => SetProperty(ref _totalRecetteInvestissementPrevu, value);
        }

        private decimal _totalRecetteInvestissementVariation;
        public decimal TotalRecetteInvestissementVariation
        {
            get => _totalRecetteInvestissementVariation;
            set => SetProperty(ref _totalRecetteInvestissementVariation, value);
        }

        private decimal _totalRecetteInvestissementDefinitif;
        public decimal TotalRecetteInvestissementDefinitif
        {
            get => _totalRecetteInvestissementDefinitif;
            set => SetProperty(ref _totalRecetteInvestissementDefinitif, value);
        }

        // Dépense Fonctionnement
        private decimal _totalDepenseFonctionnementPrevu;
        public decimal TotalDepenseFonctionnementPrevu
        {
            get => _totalDepenseFonctionnementPrevu;
            set => SetProperty(ref _totalDepenseFonctionnementPrevu, value);
        }

        private decimal _totalDepenseFonctionnementVariation;
        public decimal TotalDepenseFonctionnementVariation
        {
            get => _totalDepenseFonctionnementVariation;
            set => SetProperty(ref _totalDepenseFonctionnementVariation, value);
        }

        private decimal _totalDepenseFonctionnementDefinitif;
        public decimal TotalDepenseFonctionnementDefinitif
        {
            get => _totalDepenseFonctionnementDefinitif;
            set => SetProperty(ref _totalDepenseFonctionnementDefinitif, value);
        }

        // Dépense Investissement
        private decimal _totalDepenseInvestissementPrevu;
        public decimal TotalDepenseInvestissementPrevu
        {
            get => _totalDepenseInvestissementPrevu;
            set => SetProperty(ref _totalDepenseInvestissementPrevu, value);
        }

        private decimal _totalDepenseInvestissementVariation;
        public decimal TotalDepenseInvestissementVariation
        {
            get => _totalDepenseInvestissementVariation;
            set => SetProperty(ref _totalDepenseInvestissementVariation, value);
        }

        private decimal _totalDepenseInvestissementDefinitif;
        public decimal TotalDepenseInvestissementDefinitif
        {
            get => _totalDepenseInvestissementDefinitif;
            set => SetProperty(ref _totalDepenseInvestissementDefinitif, value);
        }

        #endregion

        #region Propriétés de totaux supplémentaires pour les lignes multiples

        // Recette Réel Investissement
        private decimal _totalRecetteReelsInvestissement;
        public decimal TotalRecetteReelsInvestissement
        {
            get => _totalRecetteReelsInvestissement;
            set => SetProperty(ref _totalRecetteReelsInvestissement, value);
        }

        private decimal _totalRecetteReelInvestissementDefinitif;
        public decimal TotalRecetteReelInvestissementDefinitif
        {
            get => _totalRecetteReelInvestissementDefinitif;
            set => SetProperty(ref _totalRecetteReelInvestissementDefinitif, value);
        }

        // Total Général Recettes Réels
        private decimal _totalGeneralRecettesReels;
        public decimal TotalGeneralRecettesReels
        {
            get => _totalGeneralRecettesReels;
            set => SetProperty(ref _totalGeneralRecettesReels, value);
        }

        private decimal _totalGeneralRecetteReelDefinitif;
        public decimal TotalGeneralRecetteReelDefinitif
        {
            get => _totalGeneralRecetteReelDefinitif;
            set => SetProperty(ref _totalGeneralRecetteReelDefinitif, value);
        }

        // Dépense Réel Fonctionnement
        private decimal _totalDepenseReelsFonctionnement;
        public decimal TotalDepenseReelsFonctionnement
        {
            get => _totalDepenseReelsFonctionnement;
            set => SetProperty(ref _totalDepenseReelsFonctionnement, value);
        }

        private decimal _totalDepenseReelFonctionnementDefinitif;
        public decimal TotalDepenseReelFonctionnementDefinitif
        {
            get => _totalDepenseReelFonctionnementDefinitif;
            set => SetProperty(ref _totalDepenseReelFonctionnementDefinitif, value);
        }

        // Total Général Dépenses Réels
        private decimal _totalGeneralDepensesReels;
        public decimal TotalGeneralDepensesReels
        {
            get => _totalGeneralDepensesReels;
            set => SetProperty(ref _totalGeneralDepensesReels, value);
        }

        private decimal _totalGeneralDepenseReelDefinitif;
        public decimal TotalGeneralDepenseReelDefinitif
        {
            get => _totalGeneralDepenseReelDefinitif;
            set => SetProperty(ref _totalGeneralDepenseReelDefinitif, value);
        }

        #endregion
        #region Commandes

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenDetailsCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CloseDetailsCommand { get; }
        public ICommand ToggleExpandCommand { get; }

        #endregion

        #region Gestion de la hiérarchie

        private void ToggleExpand(BudgetLineHierarchyViewModel? item)
        {
            if (item == null) return;

            item.ToggleExpanded();

            // Supprimer l'ancienne ligne de totaux
            var totalRow = BudgetLines.FirstOrDefault(x => x.IsTotalRow);
            if (totalRow != null)
            {
                BudgetLines.Remove(totalRow);
            }

            // Rafraîchir l'affichage
            RefreshDisplayedLines();

            // Rajouter la ligne de totaux
            AddTotalRowToDisplayedLines();
        }

        private List<BudgetLineHierarchyViewModel> BuildHierarchy(
            List<BudgetLine> budgetLines,
            NatureType nature,
            SectionType section)
        {
            var filteredLines = budgetLines
                .Where(bl => bl.Nommenclature.Nature == nature &&
                            bl.Nommenclature.Section == section)
                .ToList();

            var chapitres = filteredLines
                .Where(bl => bl.Nommenclature.ParentId == null)
                .Select(bl => CreateViewModel(bl, 0, filteredLines))
                .OrderBy(vm => vm.BudgetLine.Nommenclature.Chapitre)
                .ToList();

            return chapitres;
        }

        private BudgetLineHierarchyViewModel CreateViewModel(
            BudgetLine budgetLine,
            int level,
            List<BudgetLine> allLines)
        {
            var viewModel = new BudgetLineHierarchyViewModel(budgetLine, level);

            var children = allLines
                .Where(bl => bl.Nommenclature.ParentId == budgetLine.Nommenclature.Id)
                .Select(bl => CreateViewModel(bl, level + 1, allLines))
                .OrderBy(vm => GetOrderKey(vm.BudgetLine.Nommenclature))
                .ToList();

            foreach (var child in children)
            {
                child.Parent = viewModel;
                viewModel.Children.Add(child);
            }

            return viewModel;
        }

        private string GetOrderKey(Nommenclature n)
        {
            return $"{n.Chapitre ?? ""}|{n.Article ?? ""}|{n.Paragraphe ?? ""}|{n.SousParagraphe ?? ""}";
        }

        private List<BudgetLineHierarchyViewModel> FlattenHierarchy(
            IEnumerable<BudgetLineHierarchyViewModel> hierarchy)
        {
            var result = new List<BudgetLineHierarchyViewModel>();

            foreach (var item in hierarchy)
            {
                result.Add(item);
                if (item.IsExpanded && item.HasChildren)
                {
                    result.AddRange(FlattenHierarchy(item.Children));
                }
            }

            return result;
        }

        private void RefreshDisplayedLines()
        {
            var flatList = FlattenHierarchy(_fullHierarchy)
                .Where(vm => vm.IsVisible)
                .ToList();

            BudgetLines.Clear();
            foreach (var item in flatList)
            {
                BudgetLines.Add(item);
            }
        }

        #endregion

        #region Chargement / filtrage

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                if (!CanViewRemaniement)
                {
                    NotificationService.ShowWarning(
                        "Accès refusé : vous n'avez pas la permission de consulter les remaniements.");

                    BudgetLines.Clear();
                    return;
                }

                var service = new RemaniementService();
                var budgetLines = await service.GetBudgetLinesForValidatedBudgetAsync();

                _allBudgetLines.Clear();
                _allBudgetLines.AddRange(budgetLines);

                ApplyFilter();
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            BudgetLines.Clear();

            if (!_allBudgetLines.Any())
                return;

            // Déterminer nature et section selon l'onglet
            (NatureType nature, SectionType section) filter = SelectedTabIndex switch
            {
                0 => (NatureType.Recette, SectionType.Fonctionnement),
                1 => (NatureType.Recette, SectionType.Investissement),
                2 => (NatureType.Depense, SectionType.Fonctionnement),
                3 => (NatureType.Depense, SectionType.Investissement),
                _ => (NatureType.Recette, SectionType.Fonctionnement)
            };

            // ═══════════════════════════════════════════════════════════
            // CALCUL DES TOTAUX (Utiliser un service comme BudgetLineViewModel)
            // ═══════════════════════════════════════════════════════════

            var service = new BudgetLineService(); // Utiliser le même service

            // Recette Fonctionnement
            TotalRecetteFonctionnementPrevu = service.RecetteFonctionnementPrevu(_allBudgetLines);
            TotalRecetteFonctionnementDefinitif = service.RecetteFonctionnementDefinitif(_allBudgetLines);

            // Recette Investissement
            TotalRecetteInvestissementPrevu = service.RecetteInvestissementPrevu(_allBudgetLines);
            TotalRecetteInvestissementDefinitif = service.RecetteInvestissementDefinitif(_allBudgetLines);

            // Recette Réel Investissement
            TotalRecetteReelsInvestissement = service.TotalRecetteReelInvestissementPrevu(_allBudgetLines);
            TotalRecetteReelInvestissementDefinitif = service.TotalRecetteReelInvestissementDefinitif(_allBudgetLines);

            // Total Général Recettes Réels
            TotalGeneralRecettesReels = service.TotalGeneralRecetteReelPrevu(_allBudgetLines);
            TotalGeneralRecetteReelDefinitif = service.TotalGeneralRecetteReelDefinitif(_allBudgetLines);

            // Dépense Fonctionnement
            TotalDepenseFonctionnementPrevu = service.DepenseFonctionnementPrevu(_allBudgetLines);
            TotalDepenseFonctionnementDefinitif = service.DepenseFonctionnementDefinitif(_allBudgetLines);

            // Dépense Réel Fonctionnement
            TotalDepenseReelsFonctionnement = service.TotalDepenseReelFonctionnementPrevu(_allBudgetLines);
            TotalDepenseReelFonctionnementDefinitif = service.TotalDepenseReelFonctionnementDefinitif(_allBudgetLines);

            // Dépense Investissement
            TotalDepenseInvestissementPrevu = service.DepenseInvestissementPrevu(_allBudgetLines);
            TotalDepenseInvestissementDefinitif = service.DepenseInvestissementDefinitif(_allBudgetLines);

            // Total Général Dépenses Réels
            TotalGeneralDepensesReels = service.TotalGeneralDepenseReelPrevu(_allBudgetLines);
            TotalGeneralDepenseReelDefinitif = service.TotalGeneralDepenseReelDefinitif(_allBudgetLines);

            // Construire la hiérarchie
            _fullHierarchy = BuildHierarchy(_allBudgetLines, filter.nature, filter.section);

            // Afficher la vue aplatie
            RefreshDisplayedLines();

            // Ajouter la ligne de totaux
            AddTotalRowToDisplayedLines();

            OnPropertyChanged(nameof(TotalRemaniements));
            OnPropertyChanged(nameof(TotalVariation));
        }        
        #endregion

        #region Gestion des totaux

        private void AddTotalRowToDisplayedLines()
        {
            if (BudgetLines.Count == 0) return;

            var totalRows = new List<BudgetLineHierarchyViewModel>();

            switch (SelectedTabIndex)
            {
                case 0: // Recette - Fonctionnement
                    {
                        var totalBudgetLine = new BudgetLine
                        {
                            Nommenclature = new Nommenclature
                            {
                                Chapitre = "TOTAL",
                                Intitule = "Total Recettes de Fonctionnement"
                            },
                            MontantPrevu = TotalRecetteFonctionnementPrevu,
                            MontantDefinitif = TotalRecetteFonctionnementDefinitif
                        };
                        //MessageBox.Show(totalBudgetLine.MontantDefinitif.ToString());

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalBudgetLine, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 0
                        });
                    }
                    break;

                case 1: // Recette - Investissement
                    {
                        // LIGNE 1 : Total Recette Investissement
                        var totalRecetteInvest = new BudgetLine
                        {
                            Nommenclature = new Nommenclature
                            {
                                Chapitre = "",
                                Intitule = "Total Recettes d'Investissement"
                            },
                            MontantPrevu = TotalRecetteInvestissementPrevu,
                            MontantDefinitif = TotalRecetteInvestissementDefinitif
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalRecetteInvest, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 0
                        });

                        // LIGNE 2 : Total Recette Réels Investissement
                        var totalRecetteReelsInvest = new BudgetLine
                        {
                            Nommenclature = new Nommenclature
                            {
                                Chapitre = "",
                                Intitule = "Total Recettes Réels d'Investissement"
                            },
                            MontantPrevu = TotalRecetteReelsInvestissement,
                            MontantDefinitif = TotalRecetteReelInvestissementDefinitif
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalRecetteReelsInvest, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 1
                        });

                        // LIGNE 3 : Total Général Recettes Réels
                        var totalGeneralRecettes = new BudgetLine
                        {
                            Nommenclature = new Nommenclature
                            {
                                Chapitre = "TOTAL GÉNÉRAL",
                                Intitule = "Total Général des Recettes Réels"
                            },
                            MontantPrevu = TotalGeneralRecettesReels,
                            MontantDefinitif = TotalGeneralRecetteReelDefinitif
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalGeneralRecettes, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 2
                        });
                    }
                    break;

                case 2: // Dépense - Fonctionnement
                    {
                        // LIGNE 1 : Total Dépense Fonctionnement
                        var totalDepenseFonct = new BudgetLine
                        {
                            Nommenclature = new Nommenclature
                            {
                                Chapitre = "",
                                Intitule = "Total Dépenses de Fonctionnement"
                            },
                            MontantPrevu = TotalDepenseFonctionnementPrevu,
                            MontantDefinitif = TotalDepenseFonctionnementDefinitif
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalDepenseFonct, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 0
                        });

                        // LIGNE 2 : Total Dépense Réels Fonctionnement
                        var totalDepenseReelsFonct = new BudgetLine
                        {
                            Nommenclature = new Nommenclature
                            {
                                Chapitre = "",
                                Intitule = "Total Dépenses Réels de Fonctionnement"
                            },
                            MontantPrevu = TotalDepenseReelsFonctionnement,
                            MontantDefinitif = TotalDepenseReelFonctionnementDefinitif
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalDepenseReelsFonct, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 1
                        });
                    }
                    break;

                case 3: // Dépense - Investissement
                    {
                        // LIGNE 1 : Total Dépense Investissement
                        var totalDepenseInvest = new BudgetLine
                        {
                            Nommenclature = new Nommenclature
                            {
                                Chapitre = "",
                                Intitule = "Total Dépenses d'Investissement"
                            },
                            MontantPrevu = TotalDepenseInvestissementPrevu,
                            MontantDefinitif = TotalDepenseInvestissementDefinitif
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalDepenseInvest, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 0
                        });

                        // LIGNE 2 : Total Général Dépenses Réels
                        var totalGeneralDepenses = new BudgetLine
                        {
                            Nommenclature = new Nommenclature
                            {
                                Chapitre = "TOTAL GÉNÉRAL",
                                Intitule = "Total Général des Dépenses Réels"
                            },
                            MontantPrevu = TotalGeneralDepensesReels,
                            MontantDefinitif = TotalGeneralDepenseReelDefinitif
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalGeneralDepenses, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 2
                        });
                    }
                    break;
            }

            // Ajouter toutes les lignes de totaux
            foreach (var totalRow in totalRows)
            {
                BudgetLines.Add(totalRow);
            }
        }
        #endregion

        #region Dialog Remaniement

        private System.Threading.Tasks.Task OpenAddDialogAsync(BudgetLine? budgetLine)
        {
            if (budgetLine == null)
                return System.Threading.Tasks.Task.CompletedTask;

            SelectedBudgetLine = budgetLine;
            DialogRemaniement = new Remaniement
            {
                Date = DateTime.Now,
                Montant = 0,
                Motif = "",
                TypeRemaniement = TypeRemaniement.en_plus,
                IdBudgetLine = budgetLine.Id
            };

            IsDialogOpen = true;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private bool CanSave()
        {
            return DialogRemaniement != null &&
                   SelectedBudgetLine != null &&
                   DialogRemaniement.Montant > 0 &&
                   !string.IsNullOrWhiteSpace(DialogRemaniement.Motif);
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                if (SelectedBudgetLine == null)
                {
                    NotificationService.ShowInfo("Veuillez sélectionner une ligne budgétaire.");
                    return;
                }

                if (!CanCreateRemaniement)
                {
                    NotificationService.ShowWarning(
                        _accessDeniedMessage + "\nPermission requise : Remaniement.Create");
                    return;
                }

                var service = new RemaniementService();
                var budgetLineId = DialogRemaniement.IdBudgetLine;

                var (success, message, _) = await service.CreateRemaniementAsync(
                    DialogRemaniement,
                    DialogRemaniement.TypeRemaniement);

                if (success)
                {
                    NotificationService.ShowSuccess(message);
                }
                else
                {
                    NotificationService.ShowWarning(message);
                }

                if (success)
                {
                    IsDialogOpen = false;
                    await LoadDataAsync();
                    await RefreshDetailsIfNeeded(budgetLineId);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelDialog()
        {
            IsDialogOpen = false;
            DialogRemaniement = new Remaniement
            {
                Date = DateTime.Now,
                Montant = 0,
                Motif = "",
                TypeRemaniement = TypeRemaniement.en_plus
            };
            SelectedBudgetLine = null;
        }

        #endregion

        #region Détails remaniements

        private async System.Threading.Tasks.Task OpenDetailsDialogAsync(BudgetLine? budgetLine)
        {
            if (budgetLine == null)
                return;

            DetailBudgetLine = budgetLine;
            await LoadDetailRemaniementsAsync(budgetLine.Id);
            IsDetailDialogOpen = true;
        }

        private async System.Threading.Tasks.Task LoadDetailRemaniementsAsync(int budgetLineId)
        {
            try
            {
                var service = new RemaniementService();
                var details = await service.GetRemaniementsByBudgetLineAsync(budgetLineId);

                DetailRemaniements.Clear();
                foreach (var rem in details.OrderByDescending(r => r.Date))
                {
                    DetailRemaniements.Add(rem);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement des détails : {ex.Message}");
            }
        }

        private void CloseDetailsDialog()
        {
            IsDetailDialogOpen = false;
            DetailRemaniements.Clear();
            DetailBudgetLine = null;
        }

        #endregion

        #region Suppression

        private async System.Threading.Tasks.Task DeleteAsync(Remaniement? remaniement)
        {
            if (remaniement == null)
                return;

            var typeText = remaniement.TypeRemaniement == TypeRemaniement.en_plus ? "en PLUS" : "en MOINS";

            var confirm = MessageBox.Show(
                $"Supprimer ce remaniement ?\n\nType : {typeText}\nMontant : {remaniement.Montant:N0} GNF\nMotif : {remaniement.Motif}",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            if (!CanDeleteRemaniement)
            {
                NotificationService.ShowWarning(
                    _accessDeniedMessage + "\nPermission requise : Remaniement.Delete");
                return;
            }

            IsLoading = true;

            try
            {
                var service = new RemaniementService();
                var (success, message) = await service.DeleteRemaniementAsync(remaniement.Id);

                if (success)
                {
                    NotificationService.ShowSuccess(message);
                }
                else
                {
                    NotificationService.ShowWarning(message);
                }

                if (success)
                {
                    await LoadDataAsync();
                    await RefreshDetailsIfNeeded(remaniement.IdBudgetLine);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task RefreshDetailsIfNeeded(int budgetLineId)
        {
            if (IsDetailDialogOpen && DetailBudgetLine?.Id == budgetLineId)
            {
                await LoadDetailRemaniementsAsync(budgetLineId);
            }
        }

        #endregion
    }
}