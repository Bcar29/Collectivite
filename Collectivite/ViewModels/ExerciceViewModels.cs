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
    public class ExerciceViewModel : ViewModelBase
    {
        private readonly ExerciceService _exerciceService;
        private bool _isLoading;
        private Exercice? _selectedExercice;
        private bool _isDialogOpen;
        private Exercice _dialogExercice;
        private bool _isEditMode;

        public ExerciceViewModel(ExerciceService exerciceService)
        {
            _exerciceService = exerciceService;
            _dialogExercice = new Exercice
            {
                Libelle = "",
                DateDebut = DateOnly.FromDateTime(DateTime.Now),
                DateFin = DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
                EstCloture = false
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<Exercice>(exercice => OpenEditDialog(exercice));
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Exercice>(async exercice => await DeleteAsync(exercice));
            CloturerCommand = new RelayCommand<Exercice>(async exercice => await CloturerAsync(exercice));

            // Charger les données au démarrage
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Exercice> Exercices { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Exercice? SelectedExercice
        {
            get => _selectedExercice;
            set => SetProperty(ref _selectedExercice, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Exercice DialogExercice
        {
            get => _dialogExercice;
            set => SetProperty(ref _dialogExercice, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // ✅ CORRECTION : Propriétés pour les dates avec conversion DateTime <-> DateOnly
        public DateTime DialogExerciceDateDebut
        {
            get => DialogExercice.DateDebut.ToDateTime(TimeOnly.MinValue);
            set
            {
                DialogExercice.DateDebut = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        public DateTime DialogExerciceDateFin
        {
            get => DialogExercice.DateFin.ToDateTime(TimeOnly.MinValue);
            set
            {
                DialogExercice.DateFin = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        public string DialogTitle => IsEditMode ? "Modifier l'exercice" : "Nouvel exercice";

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CloturerCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var exercices = await _exerciceService.GetAllExerciceAsync();

                Exercices.Clear();
                foreach (var exercice in exercices)
                {
                    Exercices.Add(exercice);
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

        private void OpenAddDialog()
        {
            IsEditMode = false;

            // ✅ CORRECTION : Initialiser TOUTES les propriétés y compris les dates
            DialogExercice = new Exercice
            {
                Libelle = "",
                DateDebut = DateOnly.FromDateTime(DateTime.Now),
                DateFin = DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
                EstCloture = false
            };

            // ✅ IMPORTANT : Notifier les changements de dates
            OnPropertyChanged(nameof(DialogExerciceDateDebut));
            OnPropertyChanged(nameof(DialogExerciceDateFin));

            IsDialogOpen = true;
        }

        private void OpenEditDialog(Exercice? exercice)
        {
            if (exercice == null) return;

            IsEditMode = true;
            DialogExercice = new Exercice
            {
                Id = exercice.Id,
                Libelle = exercice.Libelle,
                DateDebut = exercice.DateDebut,
                DateFin = exercice.DateFin,
                EstCloture = exercice.EstCloture
                // ✅ Ne pas copier les relations (BudgetPrimitifs)
            };

            // ✅ IMPORTANT : Notifier les changements de dates
            OnPropertyChanged(nameof(DialogExerciceDateDebut));
            OnPropertyChanged(nameof(DialogExerciceDateFin));

            IsDialogOpen = true;
        }

        private bool CanSave()
        {
            return DialogExercice != null &&
                   !string.IsNullOrWhiteSpace(DialogExercice.Libelle) &&
                   DialogExercice.DateDebut < DialogExercice.DateFin;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            // ✅ Validation des dates
            if (DialogExercice.DateDebut >= DialogExercice.DateFin)
            {
                MessageBox.Show("La date de début doit être antérieure à la date de fin.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    // Modification
                    var (success, message) = await _exerciceService.UpdateAsync(DialogExercice);

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
                else
                {
                    // Création
                    var (success, message, exercice) = await _exerciceService.CreateAsync(DialogExercice);

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

        private async System.Threading.Tasks.Task DeleteAsync(Exercice? exercice)
        {
            if (exercice == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'exercice '{exercice.Libelle}' ?\n\n" +
                "⚠️ Attention : Tous les budgets liés seront également supprimés !",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _exerciceService.DeleteAsync(exercice.Id);

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

        // ✅ CORRECTION : Méthode Clôturer décommentée et corrigée
        private async System.Threading.Tasks.Task CloturerAsync(Exercice? exercice)
        {
            if (exercice == null) return;

            // ✅ Vérifier si déjà clôturé
            if (exercice.EstCloture)
            {
                MessageBox.Show("Cet exercice est déjà clôturé.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir clôturer l'exercice '{exercice.Libelle}' ?\n\n" +
                "⚠️ Cette action est irréversible !\n" +
                "Une fois clôturé, l'exercice ne pourra plus être modifié.",
                "Confirmation de clôture",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _exerciceService.CloturerAsync(exercice.Id);

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