using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    class BudgetPrimitifViewModel : ViewModelBase, IDisposable
    {
        private readonly BudgetPrimitifService _budgetPrimitifService;
        private readonly ExerciceService _exerciceService;
        private bool _isLoading;
        private BudgetPrimitif? _selectedBudgetPrimitif;
        private bool _isDialogOpen;
        private BudgetPrimitif _dialogBudgetPrimitif;
        private bool _isEditMode;
        private bool _isValidationDialogOpen;
        private bool _isApprovalDialogOpen;
        private DateOnly _dateValidation = DateOnly.FromDateTime(DateTime.Now);
        private DateOnly _dateApprobation = DateOnly.FromDateTime(DateTime.Now);
        private BudgetPrimitif? _budgetToValidate;
        private BudgetPrimitif? _budgetToApprove;
        private byte[]? _fichierValidation;
        private string? _fileNameValidation;
        private bool _isDisposed;
        public BudgetPrimitifViewModel(BudgetPrimitifService budgetPrimitifService)
        {
            _budgetPrimitifService = budgetPrimitifService;
            _exerciceService = ExerciceService.Instance;

            _dialogBudgetPrimitif = new BudgetPrimitif
            {
                DateApprobation = DateOnly.FromDateTime(DateTime.Now),
            };

            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            //Commandes
            LoadBudgetPrimitifCommand = new RelayCommand(async _ => await LoadBudgetPrimitifAsync());
            OppenAddBudgetPrimitifCommand = new RelayCommand(async _ => await OpenAddBudgetPrimitif());
            OppenEditBudgetPrimitifCommand = new RelayCommand<BudgetPrimitif>(budgetPrimitif => OppenEditBudgetPrimitif(budgetPrimitif));
            SaveBudgetPrimitifCommand = new RelayCommand(async _ => await SaveBudgetPrimitifAsync(), _ => CanSaveBudgetPrimitif());
            CancelBudgetPrimitifCommand = new RelayCommand(_ => CancelBudgetPrimitif());
            DeleteBudgetPrimitifCommand = new RelayCommand<BudgetPrimitif>(async budgetPrimitif => await DeleteBudgetPrimitifAsync(budgetPrimitif));
            OpenValidationDialogCommand = new RelayCommand<BudgetPrimitif>(
                budget => OpenValidationDialog(budget));

            OpenApprovalDialogCommand = new RelayCommand<BudgetPrimitif>(
                budget => OpenApprovalDialog(budget));

            ConfirmValidationCommand = new RelayCommand(
                async _ => await ConfirmValidationAsync(),
                _ => CanConfirmValidation());

            CancelValidationCommand = new RelayCommand(
                _ => CancelValidation());

            ConfirmApprovalCommand = new RelayCommand(
                async _ => await ConfirmApprovalAsync(),
                _ => CanConfirmApproval());

            CancelApprovalCommand = new RelayCommand(
                _ => CancelApproval());

            SelectFileCommand = new RelayCommand(_ => SelectFile());

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

        public DateTime DilogBudgetPrimitifDateApprobation
        {
            get => DialogBudgetPrimitif.DateApprobation.HasValue 
                ? DialogBudgetPrimitif.DateApprobation.Value.ToDateTime(TimeOnly.MinValue) 
                : DateTime.Now;
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

        public bool IsApprovalDialogOpen
        {
            get => _isApprovalDialogOpen;
            set => SetProperty(ref _isApprovalDialogOpen, value);
        }

        public DateOnly DateApprobation
        {
            get => _dateApprobation;
            set => SetProperty(ref _dateApprobation, value);
        }

        public DateTime DateApprobationDateTime
        {
            get => DateApprobation.ToDateTime(TimeOnly.MinValue);
            set => DateApprobation = DateOnly.FromDateTime(value);
        }

        public byte[]? FichierValidation
        {
            get => _fichierValidation;
            set => SetProperty(ref _fichierValidation, value);
        }

        public string? FileNameValidation
        {
            get => _fileNameValidation;
            set => SetProperty(ref _fileNameValidation, value);
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
        public ICommand OpenApprovalDialogCommand { get; }
        public ICommand ConfirmApprovalCommand { get; }
        public ICommand CancelApprovalCommand { get; }
        public ICommand SelectFileCommand { get; }
        #endregion

        #region Methods

        /// <summary>
        /// Gestionnaire pour recharger les données quand l'exercice change
        /// </summary>
        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                //System.Diagnostics.Debug.WriteLine($"Rechargement des budgets pour l'exercice : {exercice.Libelle}");
                await LoadBudgetPrimitifAsync();
            });
        }

        public async Task LoadBudgetPrimitifAsync()
        {
            IsLoading = true;
            try
            {
                // Vérifier qu'un exercice est sélectionné
                if (_exerciceService.CurrentExercice == null)
                {
                    BudgetPrimitifs.Clear();
                    MessageBox.Show(
                        "Aucun exercice n'est sélectionné.",
                        "Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var budgetPrimitifs = await _budgetPrimitifService.GetAllBudgetPrimitifAsync();

                BudgetPrimitifs.Clear();
                foreach (var budget in budgetPrimitifs)
                {
                    BudgetPrimitifs.Add(budget);
                }

                OnPropertyChanged(nameof(BudgetPrimitifs));

                System.Diagnostics.Debug.WriteLine($"Chargé {budgetPrimitifs.Count} budgets pour l'exercice {_exerciceService.CurrentExercice.Libelle}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du chargement des budgets : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task OpenAddBudgetPrimitif()
        {
            // ═══════════════════════════════════════════════════════════
            // Les budgets primitifs sont créés automatiquement lors de la création d'un exercice
            // ═══════════════════════════════════════════════════════════
            MessageBox.Show(
                "Les budgets primitifs sont créés automatiquement lors de la création d'un exercice.\n\n" +
                "Pour créer un nouveau budget primitif, veuillez créer un nouvel exercice.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // ═══════════════════════════════════════════════════════════
        // MÉTHODE SUPPRIMÉE : Le bouton Modifier est remplacé par Approuver
        // ═══════════════════════════════════════════════════════════
        // Cette méthode n'est plus utilisée car on ne modifie plus les budgets
        // après leur création. On les approuve puis on les valide.
        private void OppenEditBudgetPrimitif(BudgetPrimitif? budgetPrimitif)
        {
            // Cette méthode est conservée pour compatibilité mais ne devrait plus être appelée
            // Le bouton Modifier a été remplacé par le bouton Approuver
            if (budgetPrimitif == null)
                return;
            
            MessageBox.Show(
                "La modification du budget primitif n'est plus disponible.\n" +
                "Veuillez utiliser le bouton d'approbation pour approuver le budget.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private bool CanSaveBudgetPrimitif()
        {
            return !string.IsNullOrWhiteSpace(DialogBudgetPrimitif.MontantTotal.ToString());
        }

        private async Task SaveBudgetPrimitifAsync()
        {
            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _budgetPrimitifService.UpdateBudgetPrimitifAsync(DialogBudgetPrimitif);
                    if (success)
                    {
                        MessageBox.Show(
                            "Budget mis à jour avec succès.",
                            "Succès",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
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
                MessageBox.Show(
                    $"Erreur lors de l'enregistrement du budget : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

        private async Task DeleteBudgetPrimitifAsync(BudgetPrimitif? budgetPrimitif)
        {
            if (budgetPrimitif == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le budget de '{budgetPrimitif.Exercice.Libelle}' ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                var (success, message) = await _budgetPrimitifService.DeleteBudgetPrimitifAsync(budgetPrimitif.Id);
                if (success)
                {
                    MessageBox.Show(
                        "Budget supprimé avec succès.",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    await LoadBudgetPrimitifAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                IsLoading = false;
            }
        }

        private void OpenApprovalDialog(BudgetPrimitif? budget)
        {
            if (budget == null) return;

            // Vérifier si le budget est déjà approuvé
            if (budget.Status == BudgetPrimitif.Statusbudget.APPROVED || budget.Status == BudgetPrimitif.Statusbudget.VALIDATED)
            {
                MessageBox.Show(
                    $"Ce budget est déjà approuvé.\n\n" +
                    $"Date d'approbation : {budget.DateApprobation?.ToString("dd/MM/yyyy") ?? "N/A"}",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Vérifier que le budget est en DRAFT
            if (budget.Status != BudgetPrimitif.Statusbudget.DRAFT)
            {
                MessageBox.Show(
                    "Ce budget ne peut pas être approuvé. Il doit être en mode DRAFT.",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _budgetToApprove = budget;

            // Initialiser la date d'approbation avec la date du jour
            DateApprobation = DateOnly.FromDateTime(DateTime.Now);

            IsApprovalDialogOpen = true;
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

            // Vérifier que le budget est approuvé
            if (budget.Status != BudgetPrimitif.Statusbudget.APPROVED)
            {
                MessageBox.Show(
                    "Ce budget doit être approuvé avant d'être validé.",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _budgetToValidate = budget;

            // Initialiser la date de validation avec la date du jour
            // mais vérifier qu'elle est >= date d'approbation
            var today = DateOnly.FromDateTime(DateTime.Now);
            DateValidation = budget.DateApprobation.HasValue && today >= budget.DateApprobation.Value
                ? today
                : (budget.DateApprobation ?? today);

            // Réinitialiser le fichier
            FichierValidation = null;
            FileNameValidation = null;

            IsValidationDialogOpen = true;
        }

        private bool CanConfirmValidation()
        {
            return _budgetToValidate != null;
        }

        private bool CanConfirmApproval()
        {
            return _budgetToApprove != null;
        }

        private async Task ConfirmApprovalAsync()
        {
            if (_budgetToApprove == null) return;

            IsLoading = true;
            IsApprovalDialogOpen = false;

            try
            {
                var (success, message) = await _budgetPrimitifService.ApprouverBudgetPrimitif(
                    _budgetToApprove.Id,
                    DateApprobation);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadBudgetPrimitifAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                _budgetToApprove = null;
            }
        }

        private void CancelApproval()
        {
            IsApprovalDialogOpen = false;
            _budgetToApprove = null;
        }

        private async Task ConfirmValidationAsync()
        {
            if (_budgetToValidate == null) return;

            IsLoading = true;
            IsValidationDialogOpen = false;

            try
            {
                var (success, message) = await _budgetPrimitifService.ValiderBudgetPrimitif(
                    _budgetToValidate.Id,
                    DateValidation,
                    FichierValidation,
                    FileNameValidation);

                MessageBox.Show(
                    message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadBudgetPrimitifAsync();
                    // Réinitialiser le fichier
                    FichierValidation = null;
                    FileNameValidation = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                _budgetToValidate = null;
            }
        }

        private void SelectFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers PDF|*.pdf|Tous les fichiers|*.*",
                Title = "Sélectionner le fichier de validation"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    FileNameValidation = System.IO.Path.GetFileName(openFileDialog.FileName);
                    FichierValidation = File.ReadAllBytes(openFileDialog.FileName);
                    OnPropertyChanged(nameof(FileNameValidation));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la lecture du fichier : {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelValidation()
        {
            IsValidationDialogOpen = false;
            _budgetToValidate = null;
        }

        /// <summary>
        /// Nettoyer les ressources et se désabonner des événements
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _exerciceService.ExerciceChanged -= OnExerciceChanged;
                _isDisposed = true;
            }
        }

        #endregion
    }
}