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
        //private BudgetPrimitifService _exercice;

        public BudgetPrimitifViewModel(BudgetPrimitifService budgetPrimitifService)
        {
            _budgetPrimitifService = budgetPrimitifService;
            _dialogBudgetPrimitif = new BudgetPrimitif
            {
                DateVote = DateOnly.FromDateTime(DateTime.Now)
            };

            //Commandes

            LoadBudgetPrimitifCommand = new RelayCommand(async _ => await LoadBudgetPrimitifAsync());
            OppenAddBudgetPrimitifCommand = new RelayCommand(async _ => await OpenAddBudgetPrimitif());
            OppenEditBudgetPrimitifCommand = new RelayCommand<BudgetPrimitif>(budgetPrimitif => OppenEditBudgetPrimitif(budgetPrimitif));
            SaveBudgetPrimitifCommand = new RelayCommand(async _ => await SaveBudgetPrimitifAsync(), _ => CanSaveBudgetPrimitif());
            CancelBudgetPrimitifCommand = new RelayCommand(_ => CancelBudgetPrimitif());
            DeleteBudgetPrimitifCommand = new RelayCommand<BudgetPrimitif>(async budgetPrimitif => await DeleteBudgetPrimitifAsync(budgetPrimitif));

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
        public DateTime DilogBudgetPrimitifDateVote
        {
            get => DialogBudgetPrimitif.DateVote.ToDateTime(TimeOnly.MinValue);
            set
            {
                DialogBudgetPrimitif.DateVote = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
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
                DateVote = budgetPrimitif.DateVote,
                Montant = budgetPrimitif.Montant,
                Exercice = budgetPrimitif.Exercice,
                ExerciceId = budgetPrimitif.ExerciceId,
                
            };
            IsDialogOpen = true;

        }

        private bool CanSaveBudgetPrimitif()
        {
            return !string.IsNullOrWhiteSpace(DialogBudgetPrimitif.Montant.ToString());
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

        #endregion
    }
}
