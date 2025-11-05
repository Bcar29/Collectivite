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
    public class RemaniementViewModel : ViewModelBase
    {
        private readonly RemaniementService _remaniementService;
        private bool _isLoading;
        private Remaniement? _selectedRemaniement;
        private bool _isDialogOpen;
        private Remaniement _dialogRemaniement;
        private TypeRemaniement _typeRemaniement;
        private BudgetLine? _selectedBudgetLine;

        public RemaniementViewModel(RemaniementService remaniementService)
        {
            _remaniementService = remaniementService;
            _dialogRemaniement = new Remaniement
            {
                Date = DateTime.Now
            };
            _typeRemaniement = TypeRemaniement.en_plus;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand(async _ => await OpenAddDialogAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Remaniement>(async remaniement => await DeleteAsync(remaniement));

            // Charger les données au démarrage
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Remaniement> Remaniements { get; } = new();
        public ObservableCollection<BudgetLine> BudgetLinesSansEnfants { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Remaniement? SelectedRemaniement
        {
            get => _selectedRemaniement;
            set => SetProperty(ref _selectedRemaniement, value);
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

        public TypeRemaniement TypeRemaniement
        {
            get => _typeRemaniement;
            set => SetProperty(ref _typeRemaniement, value);
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
                }
            }
        }

        public string DialogTitle => "Nouveau Remaniement";

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var remaniements = await _remaniementService.GetAllRemaniementAsync();

                Remaniements.Clear();
                foreach (var remaniement in remaniements)
                {
                    Remaniements.Add(remaniement);
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

        private async System.Threading.Tasks.Task OpenAddDialogAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les BudgetLines sans enfants
                var budgetLines = await _remaniementService.GetBudgetLinesSansEnfantsAsync();

                BudgetLinesSansEnfants.Clear();
                foreach (var budgetLine in budgetLines)
                {
                    BudgetLinesSansEnfants.Add(budgetLine);
                }

                // Initialiser le dialog
                DialogRemaniement = new Remaniement
                {
                    Date = DateTime.Now,
                    Montant = 0,
                    Motif = ""
                };

                SelectedBudgetLine = null;
                TypeRemaniement = TypeRemaniement.en_plus;

                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dialog : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanSave()
        {
            return DialogRemaniement != null &&
                   DialogRemaniement.Montant > 0 &&
                   !string.IsNullOrWhiteSpace(DialogRemaniement.Motif) &&
                   DialogRemaniement.IdBudgetLine > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                var confirmation = MessageBox.Show(
                    $"Confirmer le remaniement ?\n\n" +
                    $"Type : {(TypeRemaniement == TypeRemaniement.en_plus ? "Augmentation (+)" : "Diminution (-)")}\n" +
                    $"Montant : {DialogRemaniement.Montant:N0} GNF\n" +
                    $"Ligne budgétaire : {SelectedBudgetLine?.Nommenclature?.Intitule}\n\n" +
                    $"⚠️ Les budgets parents seront également mis à jour.",
                    "Confirmation du remaniement",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                {
                    IsLoading = false;
                    return;
                }

                var (success, message, remaniement) = await _remaniementService.CreateRemaniementAsync(
                    DialogRemaniement,
                    TypeRemaniement);

                if (success)
                {
                    MessageBox.Show(message, "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    IsDialogOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void CancelDialog()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteAsync(Remaniement? remaniement)
        {
            if (remaniement == null) return;

            var result = MessageBox.Show(
                $"⚠️ Êtes-vous sûr de vouloir supprimer ce remaniement ?\n\n" +
                $"Montant : {remaniement.Montant:N0} GNF\n" +
                $"Date : {remaniement.Date:dd/MM/yyyy HH:mm}\n" +
                $"Motif : {remaniement.Motif}\n\n" +
                $"⚠️ ATTENTION : Les montants ne seront PAS inversés !",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _remaniementService.DeleteRemaniementAsync(remaniement.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDataAsync();
                }

                IsLoading = false;
            }
        }

        #endregion
    }
}