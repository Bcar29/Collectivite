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

        public BonCommandeFormViewModel(int? bonCommandeId = null)
        {
            _bonCommandeId = bonCommandeId;
            _isEditMode = bonCommandeId.HasValue;

            _bonCommande = new BonCommande
            {
                DateCreation = DateTime.Now,
                Numero = "", // Sera généré automatiquement
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

        public string PageTitle => IsEditMode ? "Modifier le bon de commande" : "Nouveau bon de commande";

        public double MontantTotal => Details.Sum(d => d.Total);

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
                // Charger les expressions de besoin
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

                        // Charger les engagements sélectionnés
                        EngagementsSelectionnes.Clear();
                        if (bonCommande.Engagements != null)
                        {
                            foreach (var engagement in bonCommande.Engagements)
                            {
                                EngagementsSelectionnes.Add(engagement);
                                // Ajouter aussi aux disponibles pour pouvoir les désélectionner
                                if (!EngagementsDisponibles.Any(e => e.Id == engagement.Id))
                                {
                                    EngagementsDisponibles.Add(engagement);
                                }
                            }
                        }

                        // Charger les détails
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

        private bool CanSave()
        {
            return BonCommande != null &&
                   !string.IsNullOrWhiteSpace(BonCommande.Numero) &&
                   BonCommande.ExpressionBesoinId > 0 &&
                   Details.Count > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
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
            NavigationService.Instance.GoBack();
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
        }

        private void RemoveDetail(DetailBonCommande? detail)
        {
            if (detail != null)
            {
                Details.Remove(detail);
                OnPropertyChanged(nameof(MontantTotal));
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