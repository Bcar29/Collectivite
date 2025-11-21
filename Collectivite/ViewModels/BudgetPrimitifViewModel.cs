using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    class BudgetPrimitifViewModel: ViewModelBase
    {
        private readonly BudgetPrimitifService _budgetPrimitifService;
        private bool _isLoading;
        private BudgetPrimitif? _selectedBudgetPrimitif;
        private bool _isDialogOpen;
        private BudgetPrimitif _dialogBudgetPrimitif;
        private bool _isEditMode;
        private bool _isValidationDialogOpen;
        private DateOnly _dateValidation = DateOnly.FromDateTime(DateTime.Now);
        private BudgetPrimitif? _budgetToValidate;
        private readonly AppDbContext _context;
        //private BudgetPrimitifService _exercice;

        public BudgetPrimitifViewModel(BudgetPrimitifService budgetPrimitifService)
        {
            _budgetPrimitifService = budgetPrimitifService;
            _dialogBudgetPrimitif = new BudgetPrimitif
            {
                DateApprobation = DateOnly.FromDateTime(DateTime.Now),
                //DateValidation = DateOnly.FromDateTime(DateTime.Now)
            };

            //Commandes

            LoadBudgetPrimitifCommand = new RelayCommand(async _ => await LoadBudgetPrimitifAsync());
            OppenAddBudgetPrimitifCommand = new RelayCommand(async _ => await OpenAddBudgetPrimitif());
            OppenEditBudgetPrimitifCommand = new RelayCommand<BudgetPrimitif>(budgetPrimitif => OppenEditBudgetPrimitif(budgetPrimitif));
            SaveBudgetPrimitifCommand = new RelayCommand(async _ => await SaveBudgetPrimitifAsync(), _ => CanSaveBudgetPrimitif());
            CancelBudgetPrimitifCommand = new RelayCommand(_ => CancelBudgetPrimitif());
            DeleteBudgetPrimitifCommand = new RelayCommand<BudgetPrimitif>(async budgetPrimitif => await DeleteBudgetPrimitifAsync(budgetPrimitif));
            OpenValidationDialogCommand = new RelayCommand<BudgetPrimitif>(
            budget => OpenValidationDialog(budget));

            ConfirmValidationCommand = new RelayCommand(
                async _ => await ConfirmValidationAsync(),
                _ => CanConfirmValidation());

            CancelValidationCommand = new RelayCommand(
                _ => CancelValidation());

            // Charger les données au démarrage3
            LoadBudgetPrimitifCommand.Execute(null);


        }
        #region Properties
        public ObservableCollection<BudgetPrimitif> BudgetPrimitifs { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public BudgetPrimitif? SelectedBudgetPrimitif
        {
            get => _selectedBudgetPrimitif;
            set => SetProperty(ref _selectedBudgetPrimitif, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public BudgetPrimitif DialogBudgetPrimitif
        {
            get => _dialogBudgetPrimitif;
            set => SetProperty(ref _dialogBudgetPrimitif, value);

        }
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }
        //public BudgetPrimitifService Exercice
        //{
        //    get => _exercice;
        //    set => SetProperty(ref _exercice, value);
        //}
        public DateTime DilogBudgetPrimitifDateApprobation
        {
            get => DialogBudgetPrimitif.DateApprobation.ToDateTime(TimeOnly.MinValue);
            set
            {
                DialogBudgetPrimitif.DateApprobation = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }
        public DateTime DilogBudgetPrimitifDateValidation
        {
            get => DialogBudgetPrimitif.DateValidation.HasValue ? DialogBudgetPrimitif.DateValidation.Value.ToDateTime(TimeOnly.MinValue) : DateTime.Now;
            set
            {
                DialogBudgetPrimitif.DateValidation = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        public bool IsValidationDialogOpen
        {
            get => _isValidationDialogOpen;
            set => SetProperty(ref _isValidationDialogOpen, value);
        }

        public DateOnly DateValidation
        {
            get => _dateValidation;
            set => SetProperty(ref _dateValidation, value);
        }

        public DateTime DateValidationDateTime
        {
            get => DateValidation.ToDateTime(TimeOnly.MinValue);
            set => DateValidation = DateOnly.FromDateTime(value);
        }
        public string DialogTitle => IsEditMode ? "Modifier budget primitif" : "Ajouter budget primitif";

        #endregion

        #region Commands
        public ICommand LoadBudgetPrimitifCommand { get; }
        public ICommand OppenAddBudgetPrimitifCommand { get; }
        public ICommand OppenEditBudgetPrimitifCommand { get; }
        public ICommand SaveBudgetPrimitifCommand { get; }
        public ICommand CancelBudgetPrimitifCommand { get; }
        public ICommand DeleteBudgetPrimitifCommand { get; }
        public ICommand OpenValidationDialogCommand { get; }
        public ICommand ConfirmValidationCommand { get; }
        public ICommand CancelValidationCommand { get; }
        #endregion

        #region Methods
        public async System.Threading.Tasks.Task LoadBudgetPrimitifAsync()
        {
            IsLoading = true;
            try
            {
                var budgetPrimitifs = await _budgetPrimitifService.GetAllBudgetPrimitifAsync();
                BudgetPrimitifs.Clear();
                foreach (var budget in budgetPrimitifs)
                {
                    BudgetPrimitifs.Add(budget);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des budgets : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async System.Threading.Tasks.Task OpenAddBudgetPrimitif()
        {
            try{
                var exercices = await _budgetPrimitifService.GetAllExercie();
                Exercices.Clear();
                foreach (var exercice in exercices)
                {
                   Exercices.Add(exercice);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des exercices : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            DialogBudgetPrimitif = new BudgetPrimitif();
            IsEditMode = false;
            IsDialogOpen = true;
        }

        private void OppenEditBudgetPrimitif(BudgetPrimitif? budgetPrimitif)
        {
            if (budgetPrimitif == null)
                return;
            IsEditMode = true;
            DialogBudgetPrimitif= new BudgetPrimitif
            {
                Id = budgetPrimitif.Id,
                DateApprobation = budgetPrimitif.DateApprobation,
                DateValidation = budgetPrimitif.DateValidation,
                MontantTotal = budgetPrimitif.MontantTotal,
                MontantDepense = budgetPrimitif.MontantDepense,
                MontantRecette = budgetPrimitif.MontantRecette,
                Exercice = budgetPrimitif.Exercice,
                ExerciceId = budgetPrimitif.ExerciceId,
                
            };
            IsDialogOpen = true;

        }

        private bool CanSaveBudgetPrimitif()
        {
            return !string.IsNullOrWhiteSpace(DialogBudgetPrimitif.MontantTotal.ToString());
        }

        private async System.Threading.Tasks.Task SaveBudgetPrimitifAsync()
        {
            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _budgetPrimitifService.UpdateBudgetPrimitifAsync(DialogBudgetPrimitif);
                    if (success)
                    {
                        MessageBox.Show("Budget mis à jour avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadBudgetPrimitifAsync();
                        IsDialogOpen = false;
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    //creation
                    var (success, message, _) = await _budgetPrimitifService.CreateBudgetPrimitifAsync(DialogBudgetPrimitif);
                    if (success)
                    {
                        MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadBudgetPrimitifAsync();
                        IsDialogOpen = false;
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du budget : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelBudgetPrimitif()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteBudgetPrimitifAsync(BudgetPrimitif? budgetPrimitif)
        {
            if (budgetPrimitif == null) return;
            var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer le budget de  '{budgetPrimitif.Exercice.Libelle}' ?", "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                var (success, message) = await _budgetPrimitifService.DeleteBudgetPrimitifAsync(budgetPrimitif.Id);
                if (success)
                {
                    MessageBox.Show("budget supprimée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadBudgetPrimitifAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                IsLoading = false;
            }
        }

        private void OpenValidationDialog(BudgetPrimitif? budget)
        {
            if (budget == null) return;

            // Vérifier si le budget est déjà validé
            if (budget.Status == BudgetPrimitif.Statusbudget.VALIDATED)
            {
                MessageBox.Show(
                    $"Ce budget est déjà validé.\n\n" +
                    $"Date de validation : {budget.DateValidation?.ToString("dd/MM/yyyy") ?? "N/A"}",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _budgetToValidate = budget;

            // Initialiser la date de validation avec la date du jour
            // mais vérifier qu'elle est >= date d'approbation
            var today = DateOnly.FromDateTime(DateTime.Now);
            DateValidation = today >= budget.DateApprobation
                ? today
                : budget.DateApprobation;

            IsValidationDialogOpen = true;
        }

        private bool CanConfirmValidation()
        {
            return _budgetToValidate != null;
        }

        private async Task ConfirmValidationAsync()
        {
            if (_budgetToValidate == null) return;

            IsLoading = true;
            IsValidationDialogOpen = false;

            try
            {
                var service = new BudgetPrimitifService();
                var (success, message) = await service.ValiderBudgetPrimitif(
                    _budgetToValidate.Id,
                    DateValidation);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                //if (success)
                //{
                //    // Recharger les données
                //    await LoadBudgetsPrimitivesAsync();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                _budgetToValidate = null;
            }
        }

        private void CancelValidation()
        {
            IsValidationDialogOpen = false;
            _budgetToValidate = null;
        }
        #endregion
    }
}
