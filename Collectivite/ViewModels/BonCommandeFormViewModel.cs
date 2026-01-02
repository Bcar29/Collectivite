using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class BonCommandeFormViewModel : ViewModelBase
    {
        private bool _isLoading;
        private BonCommande _bonCommande;
        private bool _isEditMode;
        private int? _bonCommandeId;
        private ExpressionBesoin? _selectedExpressionBesoin;
        private readonly ExerciceService _exerciceService;
        private readonly AuthService _authService;

        public BonCommandeFormViewModel(AuthService authService,  int? bonCommandeId = null)
        {
            _bonCommandeId = bonCommandeId;
            _isEditMode = bonCommandeId.HasValue;
            _exerciceService = ExerciceService.Instance;
            _authService = authService;

            _bonCommande = new BonCommande
            {
                DateCreation = DateTime.Now,
                Numero = "",
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            AddDetailCommand = new RelayCommand(_ => AddDetail());
            RemoveDetailCommand = new RelayCommand<DetailBonCommande>(d => RemoveDetail(d));
            RecalculerDetailCommand = new RelayCommand<DetailBonCommande>(d => RecalculerDetail(d));

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<ExpressionBesoin> ExpressionBesoins { get; } = new();
        public ObservableCollection<Engagement> EngagementsDisponibles { get; } = new();
        public ObservableCollection<Engagement> EngagementsSelectionnes { get; } = new();
        public ObservableCollection<DetailBonCommande> Details { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public BonCommande BonCommande
        {
            get => _bonCommande;
            set => SetProperty(ref _bonCommande, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        /// <summary>
        /// Expression de Besoin sélectionnée - Charge automatiquement les détails
        /// </summary>
        public ExpressionBesoin? SelectedExpressionBesoin
        {
            get => _selectedExpressionBesoin;
            set
            {
                if (SetProperty(ref _selectedExpressionBesoin, value))
                {
                    // Mettre à jour l'ID dans le BonCommande
                    if (value != null)
                    {
                        BonCommande.ExpressionBesoinId = value.Id;

                        // Charger automatiquement les détails de l'expression de besoin
                        // seulement si on n'est pas en mode édition ou si c'est une nouvelle sélection
                        if (!IsEditMode || Details.Count == 0)
                        {
                            LoadDetailsFromExpressionBesoin(value);
                        }
                        else
                        {
                            // En mode édition, demander confirmation avant de remplacer
                            var result = MessageBox.Show(
                                "Voulez-vous remplacer les détails actuels par ceux de l'expression de besoin sélectionnée ?",
                                "Confirmation",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                LoadDetailsFromExpressionBesoin(value);
                            }
                        }
                    }
                    else
                    {
                        BonCommande.ExpressionBesoinId = 0;
                    }

                    OnPropertyChanged(nameof(BonCommande));
                }
            }
        }

        public string PageTitle => IsEditMode ? "Modifier le bon de commande" : "Nouveau bon de commande";

        public double MontantTotal => Details.Sum(d => d.Total);

        /// <summary>
        /// Nombre de lignes de détails
        /// </summary>
        public int NombreLignes => Details.Count;

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddDetailCommand { get; }
        public ICommand RemoveDetailCommand { get; }
        public ICommand RecalculerDetailCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les expressions de besoin avec leurs détails
                var expressionBesoinService = new ExpressionBesoinService();
                var expressionBesoins = await expressionBesoinService.GetAllExpressionBesoinsAsync();

                ExpressionBesoins.Clear();
                foreach (var eb in expressionBesoins)
                {
                    ExpressionBesoins.Add(eb);
                }

                // Charger les engagements disponibles (sans bon de commande)
                var engagementService = new EngagementService();
                var engagements = await engagementService.GetEngagementsWithoutBonCommandeAsync();

                EngagementsDisponibles.Clear();
                foreach (var e in engagements)
                {
                    EngagementsDisponibles.Add(e);
                }

                // Si mode édition, charger le bon de commande
                if (_bonCommandeId.HasValue)
                {
                    var bonCommandeService = new BonCommandeService();
                    var bonCommande = await bonCommandeService.GetBonCommandeByIdAsync(_bonCommandeId.Value);

                    if (bonCommande != null)
                    {
                        BonCommande = new BonCommande
                        {
                            Id = bonCommande.Id,
                            Numero = bonCommande.Numero,
                            DateCreation = bonCommande.DateCreation,
                            ExpressionBesoinId = bonCommande.ExpressionBesoinId
                        };

                        // Sélectionner l'expression de besoin sans déclencher le rechargement des détails
                        _selectedExpressionBesoin = ExpressionBesoins.FirstOrDefault(eb => eb.Id == bonCommande.ExpressionBesoinId);
                        OnPropertyChanged(nameof(SelectedExpressionBesoin));

                        // Charger les engagements sélectionnés
                        EngagementsSelectionnes.Clear();
                        if (bonCommande.Engagements != null)
                        {
                            foreach (var engagement in bonCommande.Engagements)
                            {
                                EngagementsSelectionnes.Add(engagement);
                                if (!EngagementsDisponibles.Any(e => e.Id == engagement.Id))
                                {
                                    EngagementsDisponibles.Add(engagement);
                                }
                            }
                        }

                        // Charger les détails existants du bon de commande
                        Details.Clear();
                        if (bonCommande.Details != null)
                        {
                            foreach (var detail in bonCommande.Details)
                            {
                                Details.Add(new DetailBonCommande
                                {
                                    Id = detail.Id,
                                    Designation = detail.Designation,
                                    Quantite = detail.Quantite,
                                    PrixUnitaire = detail.PrixUnitaire
                                });
                            }
                        }

                        OnPropertyChanged(nameof(MontantTotal));
                        OnPropertyChanged(nameof(NombreLignes));
                    }
                }
                else
                {
                    // Mode création : générer le numéro automatiquement
                    var bonCommandeService = new BonCommandeService();
                    var nextNumero = await bonCommandeService.GenerateNextNumeroAsync();
                    BonCommande.Numero = nextNumero;
                    OnPropertyChanged(nameof(BonCommande));
                }
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

        /// <summary>
        /// Charge les détails depuis une Expression de Besoin sélectionnée
        /// </summary>
        private void LoadDetailsFromExpressionBesoin(ExpressionBesoin expressionBesoin)
        {
            if (expressionBesoin?.Details == null || !expressionBesoin.Details.Any())
            {
                // Si l'expression de besoin n'a pas de détails chargés, les récupérer
                LoadDetailsFromExpressionBesoinAsync(expressionBesoin.Id);
                return;
            }

            // Vider les détails actuels
            Details.Clear();

            // Créer les détails du bon de commande à partir de l'expression de besoin
            foreach (var detailEB in expressionBesoin.Details)
            {
                var detailBC = new DetailBonCommande
                {
                    Designation = detailEB.Designation,
                    Quantite = detailEB.Quantite,
                    PrixUnitaire = 0 // Le prix unitaire sera saisi par l'utilisateur
                };

                Details.Add(detailBC);
            }

            // Notifier les changements
            OnPropertyChanged(nameof(MontantTotal));
            OnPropertyChanged(nameof(NombreLignes));

            // Message d'information
            if (Details.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"✅ {Details.Count} ligne(s) chargée(s) depuis l'expression de besoin {expressionBesoin.Numero}");
            }
        }

        /// <summary>
        /// Charge les détails de manière asynchrone si nécessaire
        /// </summary>
        private async void LoadDetailsFromExpressionBesoinAsync(int expressionBesoinId)
        {
            try
            {
                IsLoading = true;

                var expressionBesoinService = new ExpressionBesoinService();
                var expressionBesoin = await expressionBesoinService.GetExpressionBesoinByIdAsync(expressionBesoinId);

                if (expressionBesoin?.Details != null && expressionBesoin.Details.Any())
                {
                    // Vider les détails actuels
                    Details.Clear();

                    // Créer les détails du bon de commande
                    foreach (var detailEB in expressionBesoin.Details)
                    {
                        var detailBC = new DetailBonCommande
                        {
                            Designation = detailEB.Designation,
                            Quantite = detailEB.Quantite,
                            PrixUnitaire = 0
                        };

                        Details.Add(detailBC);
                    }

                    OnPropertyChanged(nameof(MontantTotal));
                    OnPropertyChanged(nameof(NombreLignes));

                    System.Diagnostics.Debug.WriteLine($"✅ {Details.Count} ligne(s) chargée(s) depuis l'expression de besoin");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des détails : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanSave()
        {
            return BonCommande != null &&
                   !string.IsNullOrWhiteSpace(BonCommande.Numero) &&
                   BonCommande.ExpressionBesoinId > 0 &&
                   Details.Count > 0 &&
                   Details.All(d => !string.IsNullOrWhiteSpace(d.Designation) && d.Quantite > 0);
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                // Validation supplémentaire
                if (Details.Any(d => d.PrixUnitaire <= 0))
                {
                    MessageBox.Show("Veuillez saisir un prix unitaire pour chaque ligne.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    return;
                }

                var bonCommandeService = new BonCommandeService();
                var detailsList = Details.ToList();
                var engagementIds = EngagementsSelectionnes.Select(e => e.Id).ToList();

                if (IsEditMode)
                {
                    var (success, message) = await bonCommandeService.UpdateBonCommandeAsync(
                        BonCommande, detailsList, engagementIds);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        NavigateBack();
                    }
                }
                else
                {
                    var (success, message, bonCommande) = await bonCommandeService.CreateBonCommandeAsync(
                        BonCommande, detailsList, engagementIds);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        NavigateBack();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            NavigateBack();
        }

        private void NavigateBack()
        {
            NavigationService.Instance.NavigateTo(new Views.Pages.BonCommandeListPage(_authService));
        }

        private void AddDetail()
        {
            var newDetail = new DetailBonCommande
            {
                Designation = "",
                Quantite = 1,
                PrixUnitaire = 0
            };

            Details.Add(newDetail);
            OnPropertyChanged(nameof(MontantTotal));
            OnPropertyChanged(nameof(NombreLignes));
        }

        private void RemoveDetail(DetailBonCommande? detail)
        {
            if (detail != null)
            {
                Details.Remove(detail);
                OnPropertyChanged(nameof(MontantTotal));
                OnPropertyChanged(nameof(NombreLignes));
            }
        }

        private void RecalculerDetail(DetailBonCommande? detail)
        {
            if (detail != null)
            {
                OnPropertyChanged(nameof(MontantTotal));
            }
        }

        #endregion
    }
}