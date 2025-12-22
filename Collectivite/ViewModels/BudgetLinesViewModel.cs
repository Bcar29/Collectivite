using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
// Ajoutez ces using en haut du fichier
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using Microsoft.Win32;

namespace Collectivite.ViewModels
{
    // ═══════════════════════════════════════════════════════════
    // 🆕 CLASSE VIEWMODEL POUR LA HIÉRARCHIE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel pour affichage hiérarchique des lignes budgétaires
    /// </summary>
    public class BudgetLineHierarchyViewModel : ViewModelBase
    {
        private bool _isExpanded;
        private bool _isVisible = true;

        public BudgetLine BudgetLine { get; set; }
        public ObservableCollection<BudgetLineHierarchyViewModel> Children { get; set; }
        public BudgetLineHierarchyViewModel? Parent { get; set; }

        /// <summary>
        /// Niveau hiérarchique (0=Chapitre, 1=Article, 2=Paragraphe, 3=SousParagraphe)
        /// </summary>
        public int Level { get; set; }
 
        /// <summary>
        /// Indique si l'élément est plié ou déplié
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpanderIcon));
                    UpdateChildrenVisibility();
                }
            }
        }

        /// <summary>
        /// Contrôle la visibilité de la ligne
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indique si l'élément a des enfants
        /// </summary>
        public bool HasChildren => Children != null && Children.Count > 0;

        /// <summary>
        /// Indentation basée sur le niveau
        /// </summary>
        public Thickness IndentationMargin => new Thickness(Level * 20, 0, 0, 0);

        /// <summary>
        /// Couleur de fond selon le niveau
        /// </summary>
        public string BackgroundColor
        {
            get
            {
                return Level switch
                {
                    0 => "#FFCDD2", // Rouge clair (Chapitre)
                    1 => "#FFF9C4", // Jaune clair (Article)
                    2 => "#C8E6C9", // Vert clair (Paragraphe)
                    _ => "Transparent" // Sous-paragraphe
                };
            }
        }

        /// <summary>
        /// Icône de pliage/dépliage
        /// </summary>
        public string ExpanderIcon
        {
            get
            {
                if (!HasChildren) return "";
                return IsExpanded ? "ChevronDown" : "ChevronRight";
            }
        }

        /// <summary>
        /// Poids de la police selon le niveau
        /// </summary>
        public FontWeight TextFontWeight
        {
            get
            {
                return Level switch
                {
                    0 => FontWeights.Bold,      // Chapitre
                    1 => FontWeights.SemiBold,  // Article
                    2 => FontWeights.Medium,    // Paragraphe
                    _ => FontWeights.Normal     // Sous-paragraphe
                };
            }
        }

        public BudgetLineHierarchyViewModel(BudgetLine budgetLine, int level)
        {
            BudgetLine = budgetLine;
            Level = level;
            Children = new ObservableCollection<BudgetLineHierarchyViewModel>();
            _isExpanded = true; // Par défaut tout est plié
        }

        /// <summary>
        /// Bascule l'état plié/déplié
        /// </summary>
        public void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        /// <summary>
        /// Met à jour la visibilité des enfants
        /// </summary>
        private void UpdateChildrenVisibility()
        {
            if (Children == null) return;

            foreach (var child in Children)
            {
                child.IsVisible = IsExpanded;
                if (!IsExpanded)
                {
                    // Si on replie, on replie aussi tous les descendants
                    child.IsExpanded = false;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 🆕 Propriété pour identifier une ligne de totaux
        private bool _isTotalRow;
        public bool IsTotalRow
        {
            get => _isTotalRow;
            set => SetProperty(ref _isTotalRow, value);
        }

        // 🆕 Niveau de la ligne de totaux (0 = total simple, 1 = sous-total, 2 = total général)
        private int _totalRowLevel;
        public int TotalRowLevel
        {
            get => _totalRowLevel;
            set => SetProperty(ref _totalRowLevel, value);
        }

        // 🆕 Couleur de fond selon le niveau de total
        public string TotalRowBackground => TotalRowLevel switch
        {
            0 => "#FFF9C4", // Jaune clair - Total simple
            1 => "#FFE082", // Jaune moyen - Sous-total
            2 => "#FFD54F", // Jaune foncé - Total général
            _ => "#FFFFFF"
        };
    }

    // ═══════════════════════════════════════════════════════════
    // VIEWMODEL PRINCIPAL
    // ═══════════════════════════════════════════════════════════

    public class BudgetLinesViewModel : ViewModelBase, IDisposable
    {
        private readonly BudgetLineService _service;
        private int _budgetPrimitifId;
        private BudgetPrimitif? _budgetPrimitif;
        private bool _isLoading;
        private bool _isDialogOpen;
        private bool _isEditMode;
        private BudgetLine? _currentLine;
        private Nommenclature? _selectedNomenclature;
        private string _montantPrevu = "0";
        private bool _isDisposed;
        private readonly ExerciceService _exerciceService;
        private readonly AuditService _auditService;
        private readonly AuthService _authService;

        // 🆕 Collections pour la hiérarchie
        private List<BudgetLineHierarchyViewModel> _fullHierarchy = new();

        #region proprietes
        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS - GÉNÉRAL
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<BudgetLineHierarchyViewModel> DisplayedLines { get; } = new();

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    _ = LoadForSelectedTabAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsBudgetValidated => _budgetPrimitif?.Status == BudgetPrimitif.Statusbudget.VALIDATED;
        public bool CanModifyBudget => !IsBudgetValidated;

        //propriete des totaux 
        private decimal _totalRecetteFonctionnement;
        public decimal TotalRecetteFonctionnement
        {
            get => _totalRecetteFonctionnement;
            set => SetProperty(ref _totalRecetteFonctionnement, value);
        }

        private decimal _totalRecetteInvestissement;
        public decimal TotalRecetteInvestissement
        {
            get => _totalRecetteInvestissement;
            set => SetProperty(ref _totalRecetteInvestissement, value);
        }

        private decimal _totalRecetteReelsInvestissement;
        public decimal TotalRecetteReelsInvestissement
        {
            get => _totalRecetteReelsInvestissement;
            set => SetProperty(ref _totalRecetteReelsInvestissement, value);
        }

        private decimal _totalGeneralRecettesReels;
        public decimal TotalGeneralRecettesReels
        {
            get => _totalGeneralRecettesReels;
            set => SetProperty(ref _totalGeneralRecettesReels, value);
        }

        private decimal _totalDepenseFonctionnement;
        public decimal TotalDepenseFonctionnement
        {
            get => _totalDepenseFonctionnement;
            set => SetProperty(ref _totalDepenseFonctionnement, value);
        }

        private decimal _totalDepenseReelsFonctionnement;
        public decimal TotalDepenseReelsFonctionnement
        {
            get => _totalDepenseReelsFonctionnement;
            set => SetProperty(ref _totalDepenseReelsFonctionnement, value);
        }

        private decimal _totalDepenseInvestissement;
        public decimal TotalDepenseInvestissement
        {
            get => _totalDepenseInvestissement;
            set => SetProperty(ref _totalDepenseInvestissement, value);
        }

        private decimal _totalGeneralDepensesReels;
        public decimal TotalGeneralDepensesReels
        {
            get => _totalGeneralDepensesReels;
            set => SetProperty(ref _totalGeneralDepensesReels, value);
        }

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS - DIALOG
        // ═══════════════════════════════════════════════════════════

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(IsAddMode));
                    OnPropertyChanged(nameof(DialogTitle));
                }
            }
        }

        public bool IsAddMode => !IsEditMode;

        public string DialogTitle => IsEditMode
            ? "Modifier la ligne budgétaire"
            : "Ajouter une ligne budgétaire";

        public ObservableCollection<Nommenclature> AvailableNomenclatures { get; } = new();

        public Nommenclature? SelectedNomenclature
        {
            get => _selectedNomenclature;
            set => SetProperty(ref _selectedNomenclature, value);
        }

        public string MontantPrevu
        {
            get => _montantPrevu;
            set => SetProperty(ref _montantPrevu, value);
        }

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS - TOTAUX COMPTE ADMINISTRATIF
        // ═══════════════════════════════════════════════════════════

        // Recette Fonctionnement
        private decimal _totalRecetteFonctionnementDefinitif;
        public decimal TotalRecetteFonctionnementDefinitif
        {
            get => _totalRecetteFonctionnementDefinitif;
            set => SetProperty(ref _totalRecetteFonctionnementDefinitif, value);
        }

        private decimal _totalRecetteFonctionnementRealise;
        public decimal TotalRecetteFonctionnementRealise
        {
            get => _totalRecetteFonctionnementRealise;
            set => SetProperty(ref _totalRecetteFonctionnementRealise, value);
        }

        private decimal _totalRecetteFonctionnementTauxRealisation;
        public decimal TotalRecetteFonctionnementTauxRealisation
        {
            get => _totalRecetteFonctionnementTauxRealisation;
            set => SetProperty(ref _totalRecetteFonctionnementTauxRealisation, value);
        }

        private decimal _totalRecetteFonctionnementResteRealise;
        public decimal TotalRecetteFonctionnementResteRealise
        {
            get => _totalRecetteFonctionnementResteRealise;
            set => SetProperty(ref _totalRecetteFonctionnementResteRealise, value);
        }

        // Recette Investissement
        private decimal _totalRecetteInvestissementDefinitif;
        public decimal TotalRecetteInvestissementDefinitif
        {
            get => _totalRecetteInvestissementDefinitif;
            set => SetProperty(ref _totalRecetteInvestissementDefinitif, value);
        }

        private decimal _totalRecetteInvestissementRealise;
        public decimal TotalRecetteInvestissementRealise
        {
            get => _totalRecetteInvestissementRealise;
            set => SetProperty(ref _totalRecetteInvestissementRealise, value);
        }

        private decimal _totalRecetteInvestissementTauxRealisation;
        public decimal TotalRecetteInvestissementTauxRealisation
        {
            get => _totalRecetteInvestissementTauxRealisation;
            set => SetProperty(ref _totalRecetteInvestissementTauxRealisation, value);
        }

        private decimal _totalRecetteInvestissementResteRealise;
        public decimal TotalRecetteInvestissementResteRealise
        {
            get => _totalRecetteInvestissementResteRealise;
            set => SetProperty(ref _totalRecetteInvestissementResteRealise, value);
        }

        // Dépense Fonctionnement
        private decimal _totalDepenseFonctionnementDefinitif;
        public decimal TotalDepenseFonctionnementDefinitif
        {
            get => _totalDepenseFonctionnementDefinitif;
            set => SetProperty(ref _totalDepenseFonctionnementDefinitif, value);
        }

        private decimal _totalDepenseFonctionnementRealise;
        public decimal TotalDepenseFonctionnementRealise
        {
            get => _totalDepenseFonctionnementRealise;
            set => SetProperty(ref _totalDepenseFonctionnementRealise, value);
        }

        private decimal _totalDepenseFonctionnementTauxRealisation;
        public decimal TotalDepenseFonctionnementTauxRealisation
        {
            get => _totalDepenseFonctionnementTauxRealisation;
            set => SetProperty(ref _totalDepenseFonctionnementTauxRealisation, value);
        }

        private decimal _totalDepenseFonctionnementResteRealise;
        public decimal TotalDepenseFonctionnementResteRealise
        {
            get => _totalDepenseFonctionnementResteRealise;
            set => SetProperty(ref _totalDepenseFonctionnementResteRealise, value);
        }

        // Dépense Investissement
        private decimal _totalDepenseInvestissementDefinitif;
        public decimal TotalDepenseInvestissementDefinitif
        {
            get => _totalDepenseInvestissementDefinitif;
            set => SetProperty(ref _totalDepenseInvestissementDefinitif, value);
        }

        private decimal _totalDepenseInvestissementRealise;
        public decimal TotalDepenseInvestissementRealise
        {
            get => _totalDepenseInvestissementRealise;
            set => SetProperty(ref _totalDepenseInvestissementRealise, value);
        }

        private decimal _totalDepenseInvestissementTauxRealisation;
        public decimal TotalDepenseInvestissementTauxRealisation
        {
            get => _totalDepenseInvestissementTauxRealisation;
            set => SetProperty(ref _totalDepenseInvestissementTauxRealisation, value);
        }

        private decimal _totalDepenseInvestissementResteRealise;
        public decimal TotalDepenseInvestissementResteRealise
        {
            get => _totalDepenseInvestissementResteRealise;
            set => SetProperty(ref _totalDepenseInvestissementResteRealise, value);
        }

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS - TOTAUX COMPTE DE GESTION
        // ═══════════════════════════════════════════════════════════

        // Recette Fonctionnement - Gestion
        private decimal _totalRecetteFonctionnementEmis;
        public decimal TotalRecetteFonctionnementEmis
        {
            get => _totalRecetteFonctionnementEmis;
            set => SetProperty(ref _totalRecetteFonctionnementEmis, value);
        }

        private decimal _totalRecetteFonctionnementRecouvre;
        public decimal TotalRecetteFonctionnementRecouvre
        {
            get => _totalRecetteFonctionnementRecouvre;
            set => SetProperty(ref _totalRecetteFonctionnementRecouvre, value);
        }

        private decimal _totalRecetteFonctionnementTauxRecouvrement;
        public decimal TotalRecetteFonctionnementTauxRecouvrement
        {
            get => _totalRecetteFonctionnementTauxRecouvrement;
            set => SetProperty(ref _totalRecetteFonctionnementTauxRecouvrement, value);
        }

        private decimal _totalRecetteFonctionnementResteRecouvre;
        public decimal TotalRecetteFonctionnementResteRecouvre
        {
            get => _totalRecetteFonctionnementResteRecouvre;
            set => SetProperty(ref _totalRecetteFonctionnementResteRecouvre, value);
        }

        // Recette Investissement - Gestion
        private decimal _totalRecetteInvestissementEmis;
        public decimal TotalRecetteInvestissementEmis
        {
            get => _totalRecetteInvestissementEmis;
            set => SetProperty(ref _totalRecetteInvestissementEmis, value);
        }

        private decimal _totalRecetteInvestissementRecouvre;
        public decimal TotalRecetteInvestissementRecouvre
        {
            get => _totalRecetteInvestissementRecouvre;
            set => SetProperty(ref _totalRecetteInvestissementRecouvre, value);
        }

        private decimal _totalRecetteInvestissementTauxRecouvrement;
        public decimal TotalRecetteInvestissementTauxRecouvrement
        {
            get => _totalRecetteInvestissementTauxRecouvrement;
            set => SetProperty(ref _totalRecetteInvestissementTauxRecouvrement, value);
        }

        private decimal _totalRecetteInvestissementResteRecouvre;
        public decimal TotalRecetteInvestissementResteRecouvre
        {
            get => _totalRecetteInvestissementResteRecouvre;
            set => SetProperty(ref _totalRecetteInvestissementResteRecouvre, value);
        }

        // Dépense Fonctionnement - Gestion
        private decimal _totalDepenseFonctionnementEmis;
        public decimal TotalDepenseFonctionnementEmis
        {
            get => _totalDepenseFonctionnementEmis;
            set => SetProperty(ref _totalDepenseFonctionnementEmis, value);
        }

        private decimal _totalDepenseFonctionnementPaye;
        public decimal TotalDepenseFonctionnementPaye
        {
            get => _totalDepenseFonctionnementPaye;
            set => SetProperty(ref _totalDepenseFonctionnementPaye, value);
        }

        private decimal _totalDepenseFonctionnementTauxPaiement;
        public decimal TotalDepenseFonctionnementTauxPaiement
        {
            get => _totalDepenseFonctionnementTauxPaiement;
            set => SetProperty(ref _totalDepenseFonctionnementTauxPaiement, value);
        }

        private decimal _totalDepenseFonctionnementRestePaye;
        public decimal TotalDepenseFonctionnementRestePaye
        {
            get => _totalDepenseFonctionnementRestePaye;
            set => SetProperty(ref _totalDepenseFonctionnementRestePaye, value);
        }

        // Dépense Investissement - Gestion
        private decimal _totalDepenseInvestissementEmis;
        public decimal TotalDepenseInvestissementEmis
        {
            get => _totalDepenseInvestissementEmis;
            set => SetProperty(ref _totalDepenseInvestissementEmis, value);
        }

        private decimal _totalDepenseInvestissementPaye;
        public decimal TotalDepenseInvestissementPaye
        {
            get => _totalDepenseInvestissementPaye;
            set => SetProperty(ref _totalDepenseInvestissementPaye, value);
        }

        private decimal _totalDepenseInvestissementTauxPaiement;
        public decimal TotalDepenseInvestissementTauxPaiement
        {
            get => _totalDepenseInvestissementTauxPaiement;
            set => SetProperty(ref _totalDepenseInvestissementTauxPaiement, value);
        }

        private decimal _totalDepenseInvestissementRestePaye;
        public decimal TotalDepenseInvestissementRestePaye
        {
            get => _totalDepenseInvestissementRestePaye;
            set => SetProperty(ref _totalDepenseInvestissementRestePaye, value);
        }
        public string NomenclatureLibelle => _currentLine?.Nommenclature?.Intitule ?? "N/A";
        #endregion
        // ═══════════════════════════════════════════════════════════
        // COMMANDES
        // ═══════════════════════════════════════════════════════════

        public ICommand AddCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SaveDialogCommand { get; }
        public ICommand CancelDialogCommand { get; }
        public ICommand ToggleExpandCommand { get; } // 🆕
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportPdfCompteAdminCommand { get; }
        public ICommand ExportPdfCompteGestionCommand { get; }
        public ICommand PrintCommand { get; }
        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════════════════════

        public BudgetLinesViewModel(BudgetLineService service, AuthService authService, AuditService auditService)
        {
            _service = service;
            _exerciceService = ExerciceService.Instance;
            _authService = authService;
            _auditService = auditService;


            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            // Commandes principales
            AddCommand = new RelayCommand(async _ => await OpenAddDialogAsync(), _ => CanModifyBudget);
            OpenEditDialogCommand = new RelayCommand<BudgetLine>( line =>  OpenEditDialogAsync(line));
            DeleteCommand = new RelayCommand<BudgetLine>(async line => await DeleteLineAsync(line));
            RefreshCommand = new RelayCommand(async _ => await LoadForSelectedTabAsync());

            // 🆕 Commande pour plier/déplier
            ToggleExpandCommand = new RelayCommand<BudgetLineHierarchyViewModel>(ToggleExpand);

            // Commandes du dialog
            SaveDialogCommand = new RelayCommand(async _ => await SaveDialogAsync(), _ => CanSaveDialog());
            CancelDialogCommand = new RelayCommand(_ => CloseDialog());

            ExportPdfCommand = new RelayCommand(async _ => await ExportToPdfAsync());
            ExportPdfCompteAdminCommand = new RelayCommand(async _ => await ExportToPdfCompteAdminAsync());
            ExportPdfCompteGestionCommand = new RelayCommand(async _ => await ExportToPdfCompteGestionAsync());
            PrintCommand = new RelayCommand(_ => Print());

            // Charger les données initiales
            _ = InitializeAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 🆕 GESTION DE LA HIÉRARCHIE - TOGGLE
        // ═══════════════════════════════════════════════════════════

        private void ToggleExpand(BudgetLineHierarchyViewModel? item)
        {
            if (item == null) return;

            item.ToggleExpanded();
            RefreshDisplayedLines();
            // Rajouter la ligne de totaux
            AddTotalRowToDisplayedLines();

        }

        // ═══════════════════════════════════════════════════════════
        // 🆕 CONSTRUCTION DE LA HIÉRARCHIE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Construit la hiérarchie des lignes budgétaires basée sur la nomenclature
        /// </summary>
        private List<BudgetLineHierarchyViewModel> BuildHierarchy(
            List<BudgetLine> budgetLines,
            NatureType nature,
            SectionType section)
        {
            // Filtrer les lignes selon nature et section
            var filteredLines = budgetLines
                .Where(bl => bl.Nommenclature.Nature == nature &&
                            bl.Nommenclature.Section == section)
                .ToList();

            // Identifier les chapitres (ParentId == null)
            var chapitres = filteredLines
                .Where(bl => bl.Nommenclature.ParentId == null)
                .Select(bl => CreateViewModel(bl, 0, filteredLines))
                .OrderBy(vm => vm.BudgetLine.Nommenclature.Chapitre)
                .ToList();

            return chapitres;
        }

        /// <summary>
        /// Crée un ViewModel avec ses enfants récursivement
        /// </summary>
        private BudgetLineHierarchyViewModel CreateViewModel(
            BudgetLine budgetLine,
            int level,
            List<BudgetLine> allLines)
        {
            var viewModel = new BudgetLineHierarchyViewModel(budgetLine, level);

            // Trouver les enfants directs basés sur la nomenclature
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

        /// <summary>
        /// Génère une clé de tri pour la nomenclature
        /// </summary>
        private string GetOrderKey(Nommenclature n)
        {
            return $"{n.Chapitre ?? ""}|{n.Article ?? ""}|{n.Paragraphe ?? ""}|{n.SousParagraphe ?? ""}";
        }

        /// <summary>
        /// Aplatit la hiérarchie pour l'affichage dans la DataGrid
        /// </summary>
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

        /// <summary>
        /// Rafraîchit l'affichage aplati
        /// </summary>
        private void RefreshDisplayedLines()
        {
            var flatList = FlattenHierarchy(_fullHierarchy)
                .Where(vm => vm.IsVisible)
                .ToList();

            DisplayedLines.Clear();
            foreach (var item in flatList)
            {
                DisplayedLines.Add(item);
            }

            // 🆕 Ajouter la ligne de totaux selon l'onglet sélectionné
            //AddTotalRowToDisplayedLines();
        }

        private void AddTotalRowToDisplayedLines()
        {
            if (DisplayedLines.Count == 0) return;

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
                            MontantPrevu = TotalRecetteFonctionnement,
                            MontantDefinitif = TotalRecetteFonctionnementDefinitif,
                            MontantRealise = TotalRecetteFonctionnementRealise,
                            TauxRealisation = TotalRecetteFonctionnementTauxRealisation,
                            ResteRealise = TotalRecetteFonctionnementResteRealise,
                            MontantEntreSortie = TotalRecetteFonctionnementRecouvre,
                            TauxEntreSortie = TotalRecetteFonctionnementTauxRecouvrement,
                            ResteEntreSortie = TotalRecetteFonctionnementResteRecouvre
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalBudgetLine, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 0 // Total simple
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
                            MontantPrevu = TotalRecetteInvestissement,
                            MontantDefinitif = TotalRecetteInvestissementDefinitif,
                            MontantRealise = TotalRecetteInvestissementRealise,
                            TauxRealisation = TotalRecetteInvestissementTauxRealisation,
                            ResteRealise = TotalRecetteInvestissementResteRealise,
                            MontantEntreSortie = TotalRecetteInvestissementRecouvre,
                            TauxEntreSortie = TotalRecetteInvestissementTauxRecouvrement,
                            ResteEntreSortie = TotalRecetteInvestissementResteRecouvre
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalRecetteInvest, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 0 // Total simple
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
                            MontantDefinitif = TotalRecetteReelsInvestissement,
                            MontantRealise = 0,
                            TauxRealisation = 0,
                            ResteRealise = 0,
                            MontantEntreSortie = 0,
                            TauxEntreSortie = 0,
                            ResteEntreSortie = 0
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalRecetteReelsInvest, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 1 // Sous-total
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
                            MontantDefinitif = TotalGeneralRecettesReels,
                            MontantRealise = 0,
                            TauxRealisation = 0,
                            ResteRealise = 0,
                            MontantEntreSortie = 0,
                            TauxEntreSortie = 0,
                            ResteEntreSortie = 0
                        };

                        totalRows.Add(new BudgetLineHierarchyViewModel(totalGeneralRecettes, 0)
                        {
                            IsTotalRow = true,
                            TotalRowLevel = 2 // Total général
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
                            MontantPrevu = TotalDepenseFonctionnement,
                            MontantDefinitif = TotalDepenseFonctionnementDefinitif,
                            MontantRealise = TotalDepenseFonctionnementRealise,
                            TauxRealisation = TotalDepenseFonctionnementTauxRealisation,
                            ResteRealise = TotalDepenseFonctionnementResteRealise,
                            MontantEntreSortie = TotalDepenseFonctionnementPaye,
                            TauxEntreSortie = TotalDepenseFonctionnementTauxPaiement,
                            ResteEntreSortie = TotalDepenseFonctionnementRestePaye
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
                            MontantDefinitif = TotalDepenseReelsFonctionnement,
                            MontantRealise = 0,
                            TauxRealisation = 0,
                            ResteRealise = 0,
                            MontantEntreSortie = 0,
                            TauxEntreSortie = 0,
                            ResteEntreSortie = 0
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
                            MontantPrevu = TotalDepenseInvestissement,
                            MontantDefinitif = TotalDepenseInvestissementDefinitif,
                            MontantRealise = TotalDepenseInvestissementRealise,
                            TauxRealisation = TotalDepenseInvestissementTauxRealisation,
                            ResteRealise = TotalDepenseInvestissementResteRealise,
                            MontantEntreSortie = TotalDepenseInvestissementPaye,
                            TauxEntreSortie = TotalDepenseInvestissementTauxPaiement,
                            ResteEntreSortie = TotalDepenseInvestissementRestePaye
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
                            MontantDefinitif = TotalGeneralDepensesReels,
                            MontantRealise = 0,
                            TauxRealisation = 0,
                            ResteRealise = 0,
                            MontantEntreSortie = 0,
                            TauxEntreSortie = 0,
                            ResteEntreSortie = 0
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
                DisplayedLines.Add(totalRow);
            }
        }
        // ═══════════════════════════════════════════════════════════
        // GESTION DU CHANGEMENT D'EXERCICE
        // ═══════════════════════════════════════════════════════════

        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                System.Diagnostics.Debug.WriteLine($"Rechargement des lignes budgétaires pour l'exercice : {exercice.Libelle}");

                // Recharger le budget primitif pour le nouvel exercice
                await LoadBudgetPrimitifForCurrentExerciceAsync();

                // Recharger les lignes budgétaires
                await LoadForSelectedTabAsync();

            });
        }

        // ═══════════════════════════════════════════════════════════
        // INITIALISATION
        // ═══════════════════════════════════════════════════════════

        private async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                await LoadBudgetPrimitifForCurrentExerciceAsync();
                await LoadForSelectedTabAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge le budget primitif du nouvel exercice
        /// </summary>
        private async Task LoadBudgetPrimitifForCurrentExerciceAsync()
        {
            try
            {
                if (_exerciceService.CurrentExercice == null)
                {
                    _budgetPrimitif = null;
                    _budgetPrimitifId = 0;
                    DisplayedLines.Clear();
                    return;
                }

                using var context = new AppDbContext();
                _budgetPrimitif = await context.BudgetsPrimitifs
                    .FirstOrDefaultAsync(b => b.ExerciceId == _exerciceService.CurrentExercice.Id);

                if (_budgetPrimitif != null)
                {
                    _budgetPrimitifId = _budgetPrimitif.Id;
                }
                else
                {
                    _budgetPrimitifId = 0;
                    DisplayedLines.Clear();
                }

                OnPropertyChanged(nameof(IsBudgetValidated));
                OnPropertyChanged(nameof(CanModifyBudget));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du budget : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CHARGEMENT DES DONNÉES AVEC HIÉRARCHIE
        // ═══════════════════════════════════════════════════════════

        private (NatureType nature, SectionType section) TabToFilter(int tabIndex)
        {
            return tabIndex switch
            {
                0 => (NatureType.Recette, SectionType.Fonctionnement),
                1 => (NatureType.Recette, SectionType.Investissement),
                2 => (NatureType.Depense, SectionType.Fonctionnement),
                3 => (NatureType.Depense, SectionType.Investissement),
                _ => (NatureType.Recette, SectionType.Fonctionnement)
            };
        }

        public async Task LoadForSelectedTabAsync()
        {
            IsLoading = true;
            try
            {
                if (_budgetPrimitifId == 0)
                {
                    DisplayedLines.Clear();
                    _fullHierarchy.Clear();
                    return;
                }

                var filter = TabToFilter(SelectedTabIndex);
                var all = await _service.GetBudgetLinesForBudgetPrimitifAsync(_budgetPrimitifId);

                // ═══════════════════════════════════════════════════════════
                // CALCUL DES TOTAUX - BUDGET PRIMITIF
                // ═══════════════════════════════════════════════════════════
                TotalRecetteFonctionnement = _service.RecetteFonctionnementPrevu(all);
                TotalRecetteInvestissement = _service.RecetteInvestissementPrevu(all);
                TotalRecetteReelsInvestissement = _service.TotalRecetteReelInvestissementPrevu(all);
                TotalGeneralRecettesReels = _service.TotalGeneralRecetteReelPrevu(all);
                TotalDepenseFonctionnement = _service.DepenseFonctionnementPrevu(all);
                TotalDepenseReelsFonctionnement = _service.TotalDepenseReelFonctionnementPrevu(all);
                TotalDepenseInvestissement = _service.DepenseInvestissementPrevu(all);
                TotalGeneralDepensesReels = _service.TotalGeneralDepenseReelPrevu(all);

                // ═══════════════════════════════════════════════════════════
                // CALCUL DES TOTAUX - COMPTE ADMINISTRATIF
                // ═══════════════════════════════════════════════════════════

                // Recette Fonctionnement
                TotalRecetteFonctionnementDefinitif = _service.RecetteFonctionnementDefinitif(all);
                TotalRecetteFonctionnementRealise = _service.RecetteFonctionnementRealise(all);
                TotalRecetteFonctionnementResteRealise = _service.RecetteFonctionnementResteRealiser(all);
                TotalRecetteFonctionnementTauxRealisation = TotalRecetteFonctionnementDefinitif != 0
                    ? (TotalRecetteFonctionnementRealise / TotalRecetteFonctionnementDefinitif) * 100
                    : 0;

                // Recette Investissement
                TotalRecetteInvestissementDefinitif = _service.RecetteInvestissementDefinitif(all);
                TotalRecetteInvestissementRealise = _service.RecetteInvestissementRealise(all);
                TotalRecetteInvestissementResteRealise = _service.RecetteInvestissementResteRealiser(all);
                TotalRecetteInvestissementTauxRealisation = TotalRecetteInvestissementDefinitif != 0
                    ? (TotalRecetteInvestissementRealise / TotalRecetteInvestissementDefinitif) * 100
                    : 0;

                // Dépense Fonctionnement
                TotalDepenseFonctionnementDefinitif = _service.DepenseFonctionnementDefinitif(all);
                TotalDepenseFonctionnementRealise = _service.DepenseFonctionnementRealise(all);
                TotalDepenseFonctionnementResteRealise = _service.DepenseFonctionnementResteRealiser(all);
                TotalDepenseFonctionnementTauxRealisation = TotalDepenseFonctionnementDefinitif != 0
                    ? (TotalDepenseFonctionnementRealise / TotalDepenseFonctionnementDefinitif) * 100
                    : 0;

                // Dépense Investissement
                TotalDepenseInvestissementDefinitif = _service.DepenseInvestissementDefinitif(all);
                TotalDepenseInvestissementRealise = _service.DepenseInvestissementRealise(all);
                TotalDepenseInvestissementResteRealise = _service.DepenseInvestissementResteRealiser(all);
                TotalDepenseInvestissementTauxRealisation = TotalDepenseInvestissementDefinitif != 0
                    ? (TotalDepenseInvestissementRealise / TotalDepenseInvestissementDefinitif) * 100
                    : 0;

                // ═══════════════════════════════════════════════════════════
                // CALCUL DES TOTAUX - COMPTE DE GESTION
                // ═══════════════════════════════════════════════════════════

                // Recette Fonctionnement - Gestion
                TotalRecetteFonctionnementEmis = _service.RecetteFonctionnementRealise(all);
                TotalRecetteFonctionnementRecouvre = _service.RecetteFonctionnementEntreSortie(all);
                TotalRecetteFonctionnementResteRecouvre = _service.RecetteFonctionnementResteEntreSortie(all);
                TotalRecetteFonctionnementTauxRecouvrement = TotalRecetteFonctionnementEmis != 0
                    ? (TotalRecetteFonctionnementRecouvre / TotalRecetteFonctionnementEmis) * 100
                    : 0;

                // Recette Investissement - Gestion
                TotalRecetteInvestissementEmis = _service.RecetteInvestissementRealise(all);
                TotalRecetteInvestissementRecouvre = _service.RecetteInvestissementEntreSortie(all);
                TotalRecetteInvestissementResteRecouvre = _service.RecetteInvestissementResteEntreSortie(all);
                TotalRecetteInvestissementTauxRecouvrement = TotalRecetteInvestissementEmis != 0
                    ? (TotalRecetteInvestissementRecouvre / TotalRecetteInvestissementEmis) * 100
                    : 0;

                // Dépense Fonctionnement - Gestion
                TotalDepenseFonctionnementEmis = _service.DepenseFonctionnementRealise(all);
                TotalDepenseFonctionnementPaye = _service.DepenseFonctionnementEntreSortie(all);
                TotalDepenseFonctionnementRestePaye = _service.DepenseFonctionnementResteEntreSortie(all);
                TotalDepenseFonctionnementTauxPaiement = TotalDepenseFonctionnementEmis != 0
                    ? (TotalDepenseFonctionnementPaye / TotalDepenseFonctionnementEmis) * 100
                    : 0;

                // Dépense Investissement - Gestion
                TotalDepenseInvestissementEmis = _service.DepenseInvestissementRealise(all);
                TotalDepenseInvestissementPaye = _service.DepenseInvestissementEntreSortie(all);
                TotalDepenseInvestissementRestePaye = _service.DepenseInvestissementResteEntreSortie(all);
                TotalDepenseInvestissementTauxPaiement = TotalDepenseInvestissementEmis != 0
                    ? (TotalDepenseInvestissementPaye / TotalDepenseInvestissementEmis) * 100
                    : 0;

                // 🆕 Construire la hiérarchie
                _fullHierarchy = BuildHierarchy(all, filter.nature, filter.section);

                // 🆕 Afficher la vue aplatie (SANS totaux)
                RefreshDisplayedLines();

                // ✅ MAINTENANT ajouter la ligne de totaux APRÈS que tout soit calculé
                AddTotalRowToDisplayedLines();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        // ═══════════════════════════════════════════════════════════
        // DIALOG - AJOUT
        // ═══════════════════════════════════════════════════════════

        private async Task OpenAddDialogAsync()
        {
            if (!CanModifyBudget)
            {
                MessageBox.Show(
                    "Ce budget est validé et ne peut plus être modifié.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_budgetPrimitifId == 0)
            {
                MessageBox.Show(
                    "Aucun budget primitif disponible pour cet exercice.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var filter = TabToFilter(SelectedTabIndex);
                var available = await _service.GetLeafNomenclaturesNotLinkedAsync(
                    _budgetPrimitifId,
                    filter.nature,
                    filter.section
                );

                if (!available.Any())
                {
                    MessageBox.Show(
                        "Toutes les nomenclatures disponibles sont déjà liées à ce budget.",
                        "Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Préparer le dialog en mode ajout
                IsEditMode = false;
                _currentLine = null;
                SelectedNomenclature = null;
                MontantPrevu = "0";

                AvailableNomenclatures.Clear();
                foreach (var nom in available.OrderBy(n => n.CodeNomenclature))
                {
                    AvailableNomenclatures.Add(nom);
                }

                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DIALOG - MODIFICATION
        // ═══════════════════════════════════════════════════════════

        private void  OpenEditDialogAsync(BudgetLine? line)
        {
            if (line == null) return;

            if (!CanModifyBudget)
            {
                MessageBox.Show(
                    "Ce budget est validé et ne peut plus être modifié.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                // Préparer le dialog en mode édition
                IsEditMode = true;
                _currentLine = line;
                SelectedNomenclature = null;
                MontantPrevu = line.MontantPrevu.ToString();

                OnPropertyChanged(nameof(NomenclatureLibelle));

                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DIALOG - VALIDATION & SAUVEGARDE
        // ═══════════════════════════════════════════════════════════

        private bool CanSaveDialog()
        {
            if (IsEditMode)
            {
                return decimal.TryParse(MontantPrevu, out var montant) && montant >= 0;
            }
            else
            {
                return SelectedNomenclature != null &&
                       decimal.TryParse(MontantPrevu, out var montant) &&
                       montant >= 0;
            }
        }

        private async Task SaveDialogAsync()
        {
            try
            {
                if (!decimal.TryParse(MontantPrevu, out var montant))
                {
                    MessageBox.Show(
                        "Le montant doit être un nombre entier valide.",
                        "Erreur de validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (montant < 0)
                {
                    MessageBox.Show(
                        "Le montant ne peut pas être négatif.",
                        "Erreur de validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                IsDialogOpen = false;

                if (IsEditMode)
                {
                    // Mode édition
                    if (_currentLine == null) return;

                    var (success, message, bl) = await _service.UpdateBudgetLineAsync(
                        _currentLine.Id,
                        montant);
                    

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";
                        await _auditService.LogAsync(
                                   "Prevision modifié ",
                                   $"{bl?.Nommenclature.code()} montant : {bl?.MontantPrevu} {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                                   username);
                        await LoadForSelectedTabAsync();
                    }
                }
                else
                {
                    // Mode ajout
                    if (SelectedNomenclature == null)
                    {
                        MessageBox.Show(
                            "Veuillez sélectionner une nomenclature.",
                            "Erreur de validation",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        IsLoading = false;
                        IsDialogOpen = true;
                        return;
                    }

                    var newLine = await _service.CreateBudgetLineAsync(
                        _budgetPrimitifId,
                        SelectedNomenclature.Id,
                        montant);

                    var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";
                    await _auditService.LogAsync(
                                "Nouvelle Prevision ",
                                $"{newLine.Nommenclature.code()} montant : {newLine.MontantPrevu} {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                                username);

                    MessageBox.Show(
                        $"✅ Ligne budgétaire créée avec succès.\n\n" +
                        $"Nomenclature : {SelectedNomenclature.Intitule}\n" +
                        $"Montant : {montant:N0} GNF\n\n" +
                        $"Les montants des parents ont été recalculés.",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await LoadForSelectedTabAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur  : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CloseDialog()
        {
            IsDialogOpen = false;
            SelectedNomenclature = null;
            MontantPrevu = "0";
            _currentLine = null;
        }

        // ═══════════════════════════════════════════════════════════
        // SUPPRESSION
        // ═══════════════════════════════════════════════════════════

        private async Task DeleteLineAsync(BudgetLine? line)
        {
            if (line == null) return;

            if (!CanModifyBudget)
            {
                MessageBox.Show(
                    "Ce budget est validé et ne peut plus être modifié.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cette ligne ?\n\n" +
                $"Nomenclature : {line.Nommenclature?.Intitule ?? "Non défini"}\n" +
                $"Montant : {line.MontantPrevu:N0} GNF\n\n" +
                $"⚠️ Les montants des parents seront recalculés automatiquement.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                try
                {
                    var (success, message) = await _service.DeleteBudgetLineAsync(line.Id);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        await LoadForSelectedTabAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur : {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// Nettoyer les ressources et se désabonner des événements
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _exerciceService.ExerciceChanged -= OnExerciceChanged;
                _isDisposed = true;
            }
        }

        private async Task ExportToPdfAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF|*.pdf",
                    FileName = $"LignesBudgetaires_{GetTabName(SelectedTabIndex)}_{_exerciceService.CurrentExercice?.Libelle}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => GeneratePdfBudgetPrimitif(saveFileDialog.FileName));

                    MessageBox.Show(
                        "Export PDF réalisé avec succès !",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Ouvrir le fichier
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task ExportToPdfCompteAdminAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF|*.pdf",
                    FileName = $"Compte Administratif_{GetTabName(SelectedTabIndex)}_{_exerciceService.CurrentExercice?.Libelle}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => GeneratePdfCompteAdmin(saveFileDialog.FileName));

                    MessageBox.Show(
                        "Export PDF réalisé avec succès !",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Ouvrir le fichier
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task ExportToPdfCompteGestionAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF|*.pdf",
                    FileName = $"Compte Gestion{GetTabName(SelectedTabIndex)}_{_exerciceService.CurrentExercice?.Libelle}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => GeneratePdfCompteGestion(saveFileDialog.FileName));

                    MessageBox.Show(
                        "Export PDF réalisé avec succès !",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Ouvrir le fichier
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetTabName(int tabIndex)
        {
            return tabIndex switch
            {
                0 => "Recette_Fonctionnement",
                1 => "Recette_Investissement",
                2 => "Depense_Fonctionnement",
                3 => "Depense_Investissement",
                _ => "Budget"
            };
        }

        private string GetTabFullName(int tabIndex)
        {
            return tabIndex switch
            {
                0 => "Recette - Fonctionnement",
                1 => "Recette - Investissement",
                2 => "Dépense - Fonctionnement",
                3 => "Dépense - Investissement",
                _ => "Budget"
            };
        }

        private void GeneratePdfBudgetPrimitif(string filePath)
        {
            // ✅ Format paysage déjà présent : PageSize.A4.Rotate()
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

            document.Open();

            // Polices
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            // Titre
            Paragraph title = new Paragraph($"Lignes Budgétaires - {GetTabFullName(SelectedTabIndex)}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10;
            document.Add(title);

            // Sous-titre avec exercice
            Paragraph subtitle = new Paragraph($"Exercice : {_exerciceService.CurrentExercice?.Libelle ?? "N/A"}", headerFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 20;
            document.Add(subtitle);

            // Date d'export
            Paragraph dateExport = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", normalFont);
            dateExport.Alignment = Element.ALIGN_RIGHT;
            dateExport.SpacingAfter = 20;
            document.Add(dateExport);

            // Tableau principal
            PdfPTable table = new PdfPTable(6) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 12f, 10f, 10f, 12f, 40f, 16f });

            // En-têtes
            AddCellWithColor(table, "Chapitre", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Article", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Sous-Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Intitulé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Montant Prévu", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // Données hiérarchiques
            var flatList = FlattenHierarchy(_fullHierarchy);
            foreach (var item in flatList)
            {
                // Déterminer la couleur de fond selon le niveau
                BaseColor bgColor = item.Level switch
                {
                    0 => new BaseColor(255, 205, 210), // Rouge clair #FFCDD2
                    1 => new BaseColor(255, 249, 196), // Jaune clair #FFF9C4
                    2 => new BaseColor(200, 230, 201), // Vert clair #C8E6C9
                    _ => BaseColor.WHITE
                };

                // Police selon le niveau
                var cellFont = item.Level switch
                {
                    0 => boldFont,
                    1 => boldFont,
                    2 => normalFont,
                    _ => normalFont
                };

                // Indentation pour le chapitre
                string indentation = new string(' ', item.Level * 2);

                AddCellWithColor(table, indentation + (item.BudgetLine.Nommenclature.Chapitre ?? ""), cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Article ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Paragraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.SousParagraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Intitule ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantPrevu:N0} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
            }

            document.Add(table);

            // Espace avant les totaux
            document.Add(new Paragraph(" "));

            // Totaux selon l'onglet
            AddTotalsSection(document, headerFont, boldFont, normalFont);

            document.Close();
            writer.Close();
        }
        private void GeneratePdfCompteAdmin(string filePath)
        {
            // ✅ Format paysage déjà présent : PageSize.A4.Rotate()
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

            document.Open();

            // Polices
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            // Titre
            Paragraph title = new Paragraph($"Compte Administratif - {GetTabFullName(SelectedTabIndex)}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10;
            document.Add(title);

            // Sous-titre avec exercice
            Paragraph subtitle = new Paragraph($"Exercice : {_exerciceService.CurrentExercice?.Libelle ?? "N/A"}", headerFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 20;
            document.Add(subtitle);

            // Date d'export
            Paragraph dateExport = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", normalFont);
            dateExport.Alignment = Element.ALIGN_RIGHT;
            dateExport.SpacingAfter = 20;
            document.Add(dateExport);

            // Tableau principal
            PdfPTable table = new PdfPTable(9) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 10f, 8f, 8f, 10f, 30f, 12f, 12f, 10f, 12f });

            // En-têtes
            AddCellWithColor(table, "Chapitre", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Article", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Sous-Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Intitulé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Montant Prévu", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(table, "Montant Réalisé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(table, "Taux Réalisation", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(table, "Reste Réalisé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // Données hiérarchiques
            var flatList = FlattenHierarchy(_fullHierarchy);
            foreach (var item in flatList)
            {
                // Déterminer la couleur de fond selon le niveau
                BaseColor bgColor = item.Level switch
                {
                    0 => new BaseColor(255, 205, 210), // Rouge clair #FFCDD2
                    1 => new BaseColor(255, 249, 196), // Jaune clair #FFF9C4
                    2 => new BaseColor(200, 230, 201), // Vert clair #C8E6C9
                    _ => BaseColor.WHITE
                };

                // Police selon le niveau
                var cellFont = item.Level switch
                {
                    0 => boldFont,
                    1 => boldFont,
                    2 => normalFont,
                    _ => normalFont
                };

                // Indentation pour le chapitre
                string indentation = new string(' ', item.Level * 2);

                AddCellWithColor(table, indentation + (item.BudgetLine.Nommenclature.Chapitre ?? ""), cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Article ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Paragraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.SousParagraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Intitule ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantDefinitif:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantRealise:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.TauxRealisation:N2} %", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.ResteRealise:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
            }

            document.Add(table);

            // Espace avant les totaux
            document.Add(new Paragraph(" "));

            // Totaux selon l'onglet
            AddTotalsSectionCompteAdmin(document, headerFont, boldFont, normalFont);

            document.Close();
            writer.Close();
        }
        private void GeneratePdfCompteGestion(string filePath)
        {
            // ✅ Format paysage déjà présent : PageSize.A4.Rotate()
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

            document.Open();

            // Polices
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            // Titre
            Paragraph title = new Paragraph($"Compte de Gestion - {GetTabFullName(SelectedTabIndex)}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10;
            document.Add(title);

            // Sous-titre avec exercice
            Paragraph subtitle = new Paragraph($"Exercice : {_exerciceService.CurrentExercice?.Libelle ?? "N/A"}", headerFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 20;
            document.Add(subtitle);

            // Date d'export
            Paragraph dateExport = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", normalFont);
            dateExport.Alignment = Element.ALIGN_RIGHT;
            dateExport.SpacingAfter = 20;
            document.Add(dateExport);

            // 🆕 Déterminer si on est en Recette ou Dépense selon l'onglet
            bool isRecette = SelectedTabIndex == 0 || SelectedTabIndex == 1; // 0=Recette Fonct, 1=Recette Invest

            // Tableau principal
            PdfPTable table = new PdfPTable(10) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 10f, 8f, 8f, 10f, 28f, 11f, 11f, 11f, 10f, 11f });

            // 🆕 En-têtes adaptés selon Recette/Dépense
            AddCellWithColor(table, "Chapitre", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Article", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Sous-Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Intitulé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Montant Prévu", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(table, "Montant Émis", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // 🆕 Colonne adaptée : "Montant Recouvré" pour Recette, "Montant Payé" pour Dépense
            AddCellWithColor(table, isRecette ? "Montant Recouvré" : "Montant Payé",
                headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // 🆕 Colonne adaptée : "Taux Recouvrement" pour Recette, "Taux Paiement" pour Dépense
            AddCellWithColor(table, isRecette ? "Taux Recouvrement" : "Taux Paiement",
                headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // 🆕 Colonne adaptée : "Reste à Recouvrer" pour Recette, "Reste à Payer" pour Dépense
            AddCellWithColor(table, isRecette ? "Reste à Recouvrer" : "Reste à Payer",
                headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // Données hiérarchiques
            var flatList = FlattenHierarchy(_fullHierarchy);
            foreach (var item in flatList)
            {
                // Déterminer la couleur de fond selon le niveau
                BaseColor bgColor = item.Level switch
                {
                    0 => new BaseColor(255, 205, 210), // Rouge clair #FFCDD2
                    1 => new BaseColor(255, 249, 196), // Jaune clair #FFF9C4
                    2 => new BaseColor(200, 230, 201), // Vert clair #C8E6C9
                    _ => BaseColor.WHITE
                };

                // Police selon le niveau
                var cellFont = item.Level switch
                {
                    0 => boldFont,
                    1 => boldFont,
                    2 => normalFont,
                    _ => normalFont
                };

                // Indentation pour le chapitre
                string indentation = new string(' ', item.Level * 2);

                AddCellWithColor(table, indentation + (item.BudgetLine.Nommenclature.Chapitre ?? ""), cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Article ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Paragraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.SousParagraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Intitule ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantDefinitif:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantRealise:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantEntreSortie:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.TauxEntreSortie:N2} %", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.ResteEntreSortie:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
            }

            document.Add(table);

            // Espace avant les totaux
            document.Add(new Paragraph(" "));

            // Totaux selon l'onglet
            AddTotalsSectionCompteGestion(document, headerFont, boldFont, normalFont);

            document.Close();
            writer.Close();
        }
        private void AddCellWithColor(PdfPTable table, string text, iTextSharp.text.Font font, BaseColor backgroundColor, int alignment)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            cell.HorizontalAlignment = alignment;
            cell.BackgroundColor = backgroundColor;
            cell.BorderWidth = 0.5f;
            cell.BorderColor = BaseColor.GRAY;
            table.AddCell(cell);
        }

        private void AddTotalsSection(Document document, iTextSharp.text.Font headerFont, iTextSharp.text.Font boldFont, iTextSharp.text.Font normalFont)
        {
            Paragraph totalsTitle = new Paragraph("Totaux", headerFont);
            totalsTitle.SpacingBefore = 15;
            totalsTitle.SpacingAfter = 10;
            document.Add(totalsTitle);

            PdfPTable totalsTable = new PdfPTable(2) { WidthPercentage = 60 };
            totalsTable.SetWidths(new float[] { 70f, 30f });

            switch (SelectedTabIndex)
            {
                case 0: // Recette - Fonctionnement
                    AddCellWithColor(totalsTable, "Total Recettes de Fonctionnement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnement:N2} GNF", boldFont, new BaseColor(200, 230, 201), Element.ALIGN_RIGHT);
                    break;

                case 1: // Recette - Investissement
                    AddCellWithColor(totalsTable, "Total Recettes d'Investissement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissement:N2} GNF", boldFont, new BaseColor(200, 230, 201), Element.ALIGN_RIGHT);

                    AddCellWithColor(totalsTable, "Total Recettes Réels d'Investissement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteReelsInvestissement:N2} GNF", boldFont, new BaseColor(165, 214, 167), Element.ALIGN_RIGHT);

                    AddCellWithColor(totalsTable, "Total Général des Recettes Réels", headerFont, new BaseColor(66, 165, 245), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalGeneralRecettesReels:N2} GNF", headerFont, new BaseColor(144, 202, 249), Element.ALIGN_RIGHT);
                    break;

                case 2: // Dépense - Fonctionnement
                    AddCellWithColor(totalsTable, "Total Dépenses de Fonctionnement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnement:N2} GNF", boldFont, new BaseColor(239, 154, 154), Element.ALIGN_RIGHT);

                    AddCellWithColor(totalsTable, "Total Dépenses Réels de Fonctionnement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseReelsFonctionnement:N2} GNF", boldFont, new BaseColor(229, 115, 115), Element.ALIGN_RIGHT);
                    break;

                case 3: // Dépense - Investissement
                    AddCellWithColor(totalsTable, "Total Dépenses d'Investissement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissement:N2} GNF", boldFont, new BaseColor(239, 154, 154), Element.ALIGN_RIGHT);

                    AddCellWithColor(totalsTable, "Total Général des Dépenses Réels", headerFont, new BaseColor(198, 40, 40), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalGeneralDepensesReels:N2} GNF", headerFont, new BaseColor(244, 143, 177), Element.ALIGN_RIGHT);
                    break;
            }

            document.Add(totalsTable);
        }

        private void AddTotalsSectionCompteAdmin(Document document, iTextSharp.text.Font headerFont, iTextSharp.text.Font boldFont, iTextSharp.text.Font normalFont)
        {
            Paragraph totalsTitle = new Paragraph("Totaux", headerFont);
            totalsTitle.SpacingBefore = 15;
            totalsTitle.SpacingAfter = 10;
            document.Add(totalsTitle);

            PdfPTable totalsTable = new PdfPTable(4) { WidthPercentage = 100 };
            totalsTable.SetWidths(new float[] { 40f, 20f, 20f, 20f });

            // En-têtes
            AddCellWithColor(totalsTable, "Description", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
            AddCellWithColor(totalsTable, "Montant Définitif", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(totalsTable, "Montant Réalisé", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(totalsTable, "Taux (%)", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(totalsTable, "Reste à Réaliser", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            switch (SelectedTabIndex)
            {
                case 0: // Recette - Fonctionnement
                    AddCellWithColor(totalsTable, "Total Recettes de Fonctionnement", boldFont, new BaseColor(200, 230, 201), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementDefinitif:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementRealise:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementTauxRealisation:N2} %", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementResteRealise:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    break;

                case 1: // Recette - Investissement
                    AddCellWithColor(totalsTable, "Total Recettes d'Investissement", boldFont, new BaseColor(200, 230, 201), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementDefinitif:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementRealise:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementTauxRealisation:N2} %", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementResteRealise:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    break;

                case 2: // Dépense - Fonctionnement
                    AddCellWithColor(totalsTable, "Total Dépenses de Fonctionnement", boldFont, new BaseColor(239, 154, 154), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementDefinitif:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementRealise:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementTauxRealisation:N2} %", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementResteRealise:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    break;

                case 3: // Dépense - Investissement
                    AddCellWithColor(totalsTable, "Total Dépenses d'Investissement", boldFont, new BaseColor(239, 154, 154), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementDefinitif:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementRealise:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementTauxRealisation:N2} %", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementResteRealise:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    break;
            }

            document.Add(totalsTable);
        }

        private void AddTotalsSectionCompteGestion(Document document, iTextSharp.text.Font headerFont, iTextSharp.text.Font boldFont, iTextSharp.text.Font normalFont)
        {
            Paragraph totalsTitle = new Paragraph("Totaux", headerFont);
            totalsTitle.SpacingBefore = 15;
            totalsTitle.SpacingAfter = 10;
            document.Add(totalsTitle);

            PdfPTable totalsTable = new PdfPTable(5) { WidthPercentage = 100 };
            totalsTable.SetWidths(new float[] { 35f, 17f, 17f, 17f, 14f });

            bool isRecette = SelectedTabIndex == 0 || SelectedTabIndex == 1;

            // En-têtes
            AddCellWithColor(totalsTable, "Description", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
            AddCellWithColor(totalsTable, "Montant Définitif", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(totalsTable, "Montant Émis", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(totalsTable, isRecette ? "Montant Recouvré" : "Montant Payé", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(totalsTable, isRecette ? "Taux Recouvrement" : "Taux Paiement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(totalsTable, isRecette ? "Reste à Recouvrer" : "Reste à Payer", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            switch (SelectedTabIndex)
            {
                case 0: // Recette - Fonctionnement
                    AddCellWithColor(totalsTable, "Total Recettes de Fonctionnement", boldFont, new BaseColor(200, 230, 201), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementDefinitif:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementEmis:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementRecouvre:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementTauxRecouvrement:N2} %", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnementResteRecouvre:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    break;

                case 1: // Recette - Investissement
                    AddCellWithColor(totalsTable, "Total Recettes d'Investissement", boldFont, new BaseColor(200, 230, 201), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementDefinitif:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementEmis:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementRecouvre:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementTauxRecouvrement:N2} %", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissementResteRecouvre:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    break;

                case 2: // Dépense - Fonctionnement
                    AddCellWithColor(totalsTable, "Total Dépenses de Fonctionnement", boldFont, new BaseColor(239, 154, 154), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementDefinitif:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementEmis:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementPaye:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementTauxPaiement:N2} %", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnementRestePaye:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    break;

                case 3: // Dépense - Investissement
                    AddCellWithColor(totalsTable, "Total Dépenses d'Investissement", boldFont, new BaseColor(239, 154, 154), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementDefinitif:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementEmis:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementPaye:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementTauxPaiement:N2} %", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissementRestePaye:N2} GNF", normalFont, BaseColor.WHITE, Element.ALIGN_RIGHT);
                    break;
            }

            document.Add(totalsTable);
        }
        private void Print()
        {
            MessageBox.Show(
                "Fonctionnalité d'impression en cours de développement.\n" +
                "Veuillez utiliser l'export PDF puis imprimer le fichier généré.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}