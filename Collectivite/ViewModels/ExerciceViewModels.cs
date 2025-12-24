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
        private readonly AuditService _auditService;
        private readonly AuthService _authService;   
        private bool _isLoading;
        private Exercice? _selectedExercice;
        private bool _isDialogOpen;
        private Exercice _dialogExercice;
        private bool _isEditMode;

        public ExerciceViewModel(
            ExerciceService exerciceService,
            AuditService auditService,
            AuthService authService)   // ✅ Injection correcte
        {
            _exerciceService = exerciceService;
            _auditService = auditService;
            _authService = authService; // ✅ On garde l’instance globale

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
                    var (success, message) = await _exerciceService.UpdateAsync(DialogExercice);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        IsDialogOpen = false;
                        await LoadDataAsync();
                        GlobalEvents.NotifyExercicesListChanged();

                        var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";

                        await _auditService.LogAsync(
                            "Modification d'exercice",
                            $"{DialogExercice.Libelle} modifié par {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                            username);
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    var (success, message, exercice) = await _exerciceService.CreateAsync(DialogExercice);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        IsDialogOpen = false;
                        await LoadDataAsync();
                        GlobalEvents.NotifyExercicesListChanged();

                        var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";

                        await _auditService.LogAsync(
                            "Exercice créé",
                            $"{DialogExercice.Libelle} créé par {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                            username);
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
                "⚠️ Tous les budgets liés seront également supprimés !",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            var (success, message) = await _exerciceService.DeleteAsync(exercice.Id);

            MessageBox.Show(message,
                success ? "Succès" : "Erreur",
                MessageBoxButton.OK,
                success ? MessageBoxImage.Information : MessageBoxImage.Warning);

            if (success)
            {
                GlobalEvents.NotifyExercicesListChanged();

                var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";

                await _auditService.LogAsync(
                    "Exercice supprimé",
                    $"{exercice.Libelle} supprimé par {username}",
                    username);

                await LoadDataAsync();
            }

            IsLoading = false;
        }

        private async System.Threading.Tasks.Task CloturerAsync(Exercice? exercice)
        {
            if (exercice == null) return;

            if (exercice.EstCloture)
            {
                MessageBox.Show("Cet exercice est déjà clôturé.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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

            MessageBox.Show(message,
                success ? "Succès" : "Erreur",
                MessageBoxButton.OK,
                success ? MessageBoxImage.Information : MessageBoxImage.Warning);

            if (success)
            {
                var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";

                await _auditService.LogAsync(
                    "Exercice clôturé",
                    $"{exercice.Libelle} clôturé par {username}",
                    username);

                await LoadDataAsync();
            }

            IsLoading = false;
        }

        #endregion
    }
}
