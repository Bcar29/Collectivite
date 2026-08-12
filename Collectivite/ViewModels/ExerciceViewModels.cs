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

        public ExerciceViewModel(
            ExerciceService exerciceService) 
        {
            _exerciceService = exerciceService;

            _dialogExercice = new Exercice
            {
                Libelle = "",
                DateDebut = DateOnly.FromDateTime(DateTime.Now),
                DateFin = DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
                EstCloture = false
            };

            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<Exercice>(ex => OpenEditDialog(ex));
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Exercice>(async ex => await DeleteAsync(ex));
            CloturerCommand = new RelayCommand<Exercice>(async ex => await CloturerAsync(ex));

            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Exercice> Exercices { get; } = new();

        public bool CanViewExercice => SessionManager.HasPermission("Exercice.View");
        public bool CanCreateExercice => SessionManager.HasPermission("Exercice.Create");
        public bool CanEditExercice => SessionManager.HasPermission("Exercice.Edit");
        public bool CanDeleteExercice => SessionManager.HasPermission("Exercice.Delete");

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
                    Exercices.Add(exercice);
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

        private void OpenAddDialog()
        {
            if (!CanCreateExercice)
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

            IsEditMode = false;

            DialogExercice = new Exercice
            {
                Libelle = "",
                DateDebut = DateOnly.FromDateTime(DateTime.Now),
                DateFin = DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
                EstCloture = false
            };

            OnPropertyChanged(nameof(DialogExerciceDateDebut));
            OnPropertyChanged(nameof(DialogExerciceDateFin));

            IsDialogOpen = true;
        }

        private void OpenEditDialog(Exercice? exercice)
        {
            if (exercice == null) return;

            if (!CanEditExercice)
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

            IsEditMode = true;

            DialogExercice = new Exercice
            {
                Id = exercice.Id,
                Libelle = exercice.Libelle,
                DateDebut = exercice.DateDebut,
                DateFin = exercice.DateFin,
                EstCloture = exercice.EstCloture
            };

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
            if (!(IsEditMode ? CanEditExercice : CanCreateExercice))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

            if (DialogExercice.DateDebut >= DialogExercice.DateFin)
            {
                NotificationService.ShowWarning("La date de début doit être antérieure à la date de fin.");
                return;
            }

            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _exerciceService.UpdateAsync(DialogExercice);

                    if (success)
                    {
                        NotificationService.ShowSuccess(message);

                        IsDialogOpen = false;
                        await LoadDataAsync();
                        GlobalEvents.NotifyExercicesListChanged();

                    }
                    else
                    {
                        NotificationService.ShowWarning(message);
                    }
                }
                else
                {
                    var (success, message, exercice) = await _exerciceService.CreateAsync(DialogExercice);

                    if (success)
                    {
                        NotificationService.ShowSuccess(message);

                        IsDialogOpen = false;
                        await LoadDataAsync();
                        GlobalEvents.NotifyExercicesListChanged();

                    }
                    else
                    {
                        NotificationService.ShowWarning(message);
                    }
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
        }

        private async System.Threading.Tasks.Task DeleteAsync(Exercice? exercice)
        {
            if (exercice == null) return;

            if (!CanDeleteExercice)
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'exercice '{exercice.Libelle}' ?\n\n" +
                "⚠️ Tous les budgets liés seront également supprimés !",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            var (success, message) = await _exerciceService.DeleteAsync(exercice.Id);

            if (success)
                NotificationService.ShowSuccess(message);
            else
                NotificationService.ShowWarning(message);

            if (success)
            {
                GlobalEvents.NotifyExercicesListChanged();

                await LoadDataAsync();
            }

            IsLoading = false;
        }

        private async System.Threading.Tasks.Task CloturerAsync(Exercice? exercice)
        {
            if (exercice == null) return;

            if (!CanEditExercice)
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

            if (exercice.EstCloture)
            {
                NotificationService.ShowInfo("Cet exercice est déjà clôturé.");
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous clôturer l'exercice '{exercice.Libelle}' ?\n\n" +
                "⚠️ Action irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            var (success, message) = await _exerciceService.CloturerAsync(exercice.Id);

            if (success)
                NotificationService.ShowSuccess(message);
            else
                NotificationService.ShowWarning(message);

            if (success)
            {

                await LoadDataAsync();
            }

            IsLoading = false;
        }

        #endregion
    }
}
