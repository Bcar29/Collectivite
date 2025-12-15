using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class MandatFormViewModel : ViewModelBase
    {
        private bool _isLoading;
        private bool _isEditMode;
        private int? _mandatId;
        private Mandat _mandat;
        private BudgetLine? _selectedBudgetLine;

        public MandatFormViewModel(int? mandatId = null)
        {
            _mandatId = mandatId;
            _isEditMode = mandatId.HasValue;

            _mandat = new Mandat
            {
                DateEmission = DateTime.Now,
                Mois = (TypeMois)DateTime.Now.Month - 1,
                MontantBrut = 0,
                Rts = 0,
                AutresPrecomptes = 0,
                MontantNet = 0,
                NumeroMandat = "" // Sera généré automatiquement
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            CalculerMontantNetCommand = new RelayCommand(_ => CalculerMontantNet());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            ConvertMontantToLettresCommand = new RelayCommand(_ => ConvertMontantToLettres());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Engagement> Engagements { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public Mandat Mandat
        {
            get => _mandat;
            set
            {
                if (SetProperty(ref _mandat, value))
                {
                    // Déclencher le chargement du BudgetLine quand l'EngagementId change
                    if (_mandat != null && _mandat.EngagementId > 0)
                    {
                        _ = OnEngagementChanged();
                    }
                }
            }
        }

        public BudgetLine? SelectedBudgetLine
        {
            get => _selectedBudgetLine;
            set
            {
                if (SetProperty(ref _selectedBudgetLine, value))
                {
                    OnPropertyChanged(nameof(MontantDisponible));
                    OnPropertyChanged(nameof(NomenclatureCode));
                }
            }
        }

        public string PageTitle => IsEditMode ? "Modifier le mandat" : "Nouveau mandat";

        // Liste des mois
        public Array MoisList => Enum.GetValues(typeof(TypeMois));

        // Propriétés calculées pour affichage
        public decimal MontantDisponible => SelectedBudgetLine?.MontantDefinitif ?? 0;
        public string NomenclatureCode => SelectedBudgetLine?.Nommenclature?.CodeNomenclature ?? "";

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand CalculerMontantNetCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ConvertMontantToLettresCommand { get; }

        #endregion

        #region Methods

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les engagements
                var engagementService = new EngagementService();
                var engagements = await engagementService.GetEngagementsForMandatAsync();

                Engagements.Clear();
                foreach (var e in engagements)
                {
                    Engagements.Add(e);
                }

                // Si mode édition, charger le mandat
                if (IsEditMode && _mandatId.HasValue)
                {
                    var mandatService = new MandatService();
                    var existingMandat = await mandatService.GetMandatByIdAsync(_mandatId.Value);

                    if (existingMandat != null)
                    {
                        Mandat = new Mandat
                        {
                            Id = existingMandat.Id,
                            NumeroMandat = existingMandat.NumeroMandat,
                            Bordereau = existingMandat.Bordereau,
                            Mois = existingMandat.Mois,
                            EngagementId = existingMandat.EngagementId,
                            MontantBrut = existingMandat.MontantBrut,
                            Rts = existingMandat.Rts,
                            AutresPrecomptes = existingMandat.AutresPrecomptes,
                            MontantNet = existingMandat.MontantNet,
                            MontantLettre = existingMandat.MontantLettre,
                            DateEmission = existingMandat.DateEmission,
                            Objet = existingMandat.Objet,
                            DatePaiement = existingMandat.DatePaiement
                        };

                        // Charger le BudgetLine pour l'engagement
                        await OnEngagementChanged();
                    }
                    else
                    {
                        MessageBox.Show("Mandat introuvable.", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Cancel();
                    }
                }
                else
                {
                    // Mode création : générer le numéro automatiquement
                    var mandatService = new MandatService();
                    var nextNumero = await mandatService.GenerateNextNumeroAsync();
                    Mandat.NumeroMandat = nextNumero;
                    OnPropertyChanged(nameof(Mandat));
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
        /// Méthode pour charger le BudgetLine quand l'engagement change
        /// </summary>
        private async Task OnEngagementChanged()
        {
            var mandatService = new MandatService();

            if (Mandat.EngagementId > 0)
            {
                var budgetLine = await mandatService.GetBudgetLineByEngagementIdAsync(Mandat.EngagementId);
                SelectedBudgetLine = budgetLine;
            }
            else
            {
                SelectedBudgetLine = null;
            }
        }

        private void CalculerMontantNet()
        {
            Mandat.MontantNet = Mandat.MontantBrut - Mandat.Rts - Mandat.AutresPrecomptes;
            OnPropertyChanged(nameof(Mandat));
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Mandat.NumeroMandat) &&
                   Mandat.EngagementId > 0 &&
                   Mandat.MontantBrut > 0 &&
                   Mandat.MontantNet > 0 &&
                   !string.IsNullOrWhiteSpace(Mandat.MontantLettre) &&
                   !string.IsNullOrWhiteSpace(Mandat.Objet);
        }

        private async Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                var mandatService = new MandatService();

                if (IsEditMode)
                {
                    var (success, message) = await mandatService.UpdateMandatAsync(Mandat);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        Cancel();
                    }
                }
                else
                {
                    var (success, message, mandat) = await mandatService.CreateMandatAsync(Mandat);

                    // Si Mandat est créé avec succès, générer l'écriture comptable
                    if (success && mandat != null)
                    {
                        // Récupérer la ligne budgétaire de l'engagement correspondant avec sa nomenclature
                        var currentEngagement = Engagements
                            .FirstOrDefault(e => e.Id == Mandat.EngagementId);

                        if (currentEngagement != null)
                        {
                            var budgetLine = currentEngagement.BudgetLine;

                            if (budgetLine?.Nommenclature != null)
                            {
                                // APPELER LA FONCTION UTILITAIRE
                                var (ecritureSuccess, ecritureMessage, ecriture) =
                                    await EcritureComptableHelper.GenererEcritureComptableAsync(
                                        budgetLine,  // La ligne budgétaire complète
                                        null,        // Pas d'ordre de recette (null car on traite un mandat)
                                        mandat       // Le mandat créé
                                    );

                                // Ajouter le résultat au message principal
                                if (ecritureSuccess)
                                {
                                    message += "\n\n" + ecritureMessage;
                                }
                                else
                                {
                                    // Le mandat est créé mais pas l'écriture
                                    message += "\n\n⚠️ " + ecritureMessage;
                                }
                            }
                        }
                    }

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        Cancel();
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
            NavigationService.Instance.NavigateTo(new Views.Pages.MandatListPage());

        }

        private void ConvertMontantToLettres()
        {
            if (Mandat.MontantNet > 0)
            {
                Mandat.MontantLettre = Convertir.ConvertirNombreEnLettres((long)Mandat.MontantNet);
                OnPropertyChanged(nameof(Mandat));
            }
        }

        #endregion
    }
}