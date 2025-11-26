using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        public BudgetLine? SelectedBudgetLine
        {
            get => _selectedBudgetLine;
            set
            {
                _selectedBudgetLine = value;
                OnPropertyChanged(nameof(SelectedBudgetLine));
                // Vous pouvez aussi exposer des propriétés spécifiques
                OnPropertyChanged(nameof(MontantDisponible));
                OnPropertyChanged(nameof(NomenclatureCode));
            }
        }

        // Propriétés calculées pour affichage
        public decimal MontantDisponible => SelectedBudgetLine?.MontantDefinitif ?? 0;
        public string NomenclatureCode => SelectedBudgetLine?.Nommenclature ?.code() ?? "";

        

        // Méthode pour charger le BudgetLine quand l'engagement change
        private async Task OnEngagementChanged()
        {
             MandatService mandatService = new MandatService();
            
            if (Mandat.EngagementId > 0)
            {
                var budgetLine = await mandatService.GetBudgetLineByEngagementIdAsync(Mandat.EngagementId);
                SelectedBudgetLine = budgetLine;
                Console.WriteLine(budgetLine);
            }
            else
            {
                SelectedBudgetLine = null;
            }
        }
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
                MontantNet = 0
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            CalculerMontantNetCommand = new RelayCommand(_ => CalculerMontantNet());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());

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
            set => SetProperty(ref _mandat, value);
        }

        public string PageTitle => IsEditMode ? "Modifier le mandat" : "Nouveau mandat";

        // Liste des mois
        public Array MoisList => Enum.GetValues(typeof(TypeMois));

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand CalculerMontantNetCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les engagements
                var engagementService = new EngagementService();
                var engagements = await engagementService.GetAllEngagementsAsync();

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
                            Motif = existingMandat.Motif,
                            DatePaiement = existingMandat.DatePaiement
                        };
                    }
                    else
                    {
                        MessageBox.Show("Mandat introuvable.", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Cancel();
                    }
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

        private async System.Threading.Tasks.Task SaveAsync()
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
                        ReturnToList();
                    }
                }
                else
                {
                    var (success, message, mandat) = await mandatService.CreateMandatAsync(Mandat);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        ReturnToList();
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
            var result = MessageBox.Show(
                "Voulez-vous vraiment annuler ? Les modifications non enregistrées seront perdues.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ReturnToList();
            }
        }

        private void ReturnToList()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    frame.GoBack();
                }
            }
        }

        #endregion


    }
}