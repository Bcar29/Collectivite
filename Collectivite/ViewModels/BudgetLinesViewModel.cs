using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class BudgetLinesViewModel : ViewModelBase, IDisposable
    {
        private readonly BudgetLineService _service;
        private int _budgetPrimitifId;
        private BudgetPrimitif? _budgetPrimitif;
        private bool _isLoading;
        private bool _isDialogOpen;
        private bool _isEditMode;
        private BudgetLine? _currentLine;
        private Nommenclature? _selectedNomenclature;
        private string _montantPrevu = "0";
        private bool _isDisposed;
        private readonly ExerciceService _exerciceService;

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS - GÉNÉRAL
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<BudgetLine> DisplayedLines { get; } = new();

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    _ = LoadForSelectedTabAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsBudgetValidated => _budgetPrimitif?.Status == BudgetPrimitif.Statusbudget.VALIDATED;
        public bool CanModifyBudget => !IsBudgetValidated;

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS - DIALOG
        // ═══════════════════════════════════════════════════════════

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(IsAddMode));
                    OnPropertyChanged(nameof(DialogTitle));
                }
            }
        }

        public bool IsAddMode => !IsEditMode;

        public string DialogTitle => IsEditMode
            ? "Modifier la ligne budgétaire"
            : "Ajouter une ligne budgétaire";

        public ObservableCollection<Nommenclature> AvailableNomenclatures { get; } = new();

        public Nommenclature? SelectedNomenclature
        {
            get => _selectedNomenclature;
            set => SetProperty(ref _selectedNomenclature, value);
        }

        public string MontantPrevu
        {
            get => _montantPrevu;
            set => SetProperty(ref _montantPrevu, value);
        }

        public string NomenclatureLibelle => _currentLine?.Nommenclature?.Intitule ?? "N/A";

        // ═══════════════════════════════════════════════════════════
        // COMMANDES
        // ═══════════════════════════════════════════════════════════

        public ICommand AddCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SaveDialogCommand { get; }
        public ICommand CancelDialogCommand { get; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════════════════════

        public BudgetLinesViewModel(BudgetLineService service)
        {
            _service = service;
            //_budgetPrimitifId = budgetPrimitifId;
            _exerciceService = ExerciceService.Instance;

            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            // Commandes principales
            AddCommand = new RelayCommand(async _ => await OpenAddDialogAsync(), _ => CanModifyBudget);
            OpenEditDialogCommand = new RelayCommand<BudgetLine>(async line => await OpenEditDialogAsync(line));
            DeleteCommand = new RelayCommand<BudgetLine>(async line => await DeleteLineAsync(line));
            RefreshCommand = new RelayCommand(async _ => await LoadForSelectedTabAsync());

            // Commandes du dialog
            SaveDialogCommand = new RelayCommand(async _ => await SaveDialogAsync(), _ => CanSaveDialog());
            CancelDialogCommand = new RelayCommand(_ => CloseDialog());

            // Charger les données initiales
            _ = InitializeAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // GESTION DU CHANGEMENT D'EXERCICE
        // ═══════════════════════════════════════════════════════════

        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                System.Diagnostics.Debug.WriteLine($"Rechargement des lignes budgétaires pour l'exercice : {exercice.Libelle}");

                // Recharger le budget primitif pour le nouvel exercice
                await LoadBudgetPrimitifForCurrentExerciceAsync();

                // Recharger les lignes budgétaires
                await LoadForSelectedTabAsync();
            });
        }

        // ═══════════════════════════════════════════════════════════
        // INITIALISATION
        // ═══════════════════════════════════════════════════════════

        private async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                //await LoadBudgetPrimitifAsync();
                await LoadBudgetPrimitifForCurrentExerciceAsync();
                await LoadForSelectedTabAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBudgetPrimitifAsync()
        {
            using var context = new AppDbContext();
            _budgetPrimitif = await context.BudgetsPrimitifs
                .FirstOrDefaultAsync(b => b.Id == _budgetPrimitifId);

            OnPropertyChanged(nameof(IsBudgetValidated));
            OnPropertyChanged(nameof(CanModifyBudget));
        }

        /// <summary>
        /// Charge le budget primitif du nouvel exercice
        /// </summary>
        private async Task LoadBudgetPrimitifForCurrentExerciceAsync()
        {
            try
            {
                if (_exerciceService.CurrentExercice == null)
                {
                    _budgetPrimitif = null;
                    _budgetPrimitifId = 0;
                    DisplayedLines.Clear();
                    return;
                }

                using var context = new AppDbContext();
                _budgetPrimitif = await context.BudgetsPrimitifs
                    .FirstOrDefaultAsync(b => b.ExerciceId == _exerciceService.CurrentExercice.Id);

                if (_budgetPrimitif != null)
                {
                    _budgetPrimitifId = _budgetPrimitif.Id;
                }
                else
                {
                    _budgetPrimitifId = 0;
                    DisplayedLines.Clear();
                }

                OnPropertyChanged(nameof(IsBudgetValidated));
                OnPropertyChanged(nameof(CanModifyBudget));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du budget : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CHARGEMENT DES DONNÉES
        // ═══════════════════════════════════════════════════════════

        private (NatureType nature, SectionType section) TabToFilter(int tabIndex)
        {
            return tabIndex switch
            {
                0 => (NatureType.Recette, SectionType.Fonctionnement),
                1 => (NatureType.Recette, SectionType.Investissement),
                2 => (NatureType.Depense, SectionType.Fonctionnement),
                3 => (NatureType.Depense, SectionType.Investissement),
                _ => (NatureType.Recette, SectionType.Fonctionnement)
            };
        }

        public async Task LoadForSelectedTabAsync()
        {
            IsLoading = true;
            try
            {
                if (_budgetPrimitifId == 0)
                {
                    DisplayedLines.Clear();
                    return;
                }

                var filter = TabToFilter(SelectedTabIndex);
                var all = await _service.GetBudgetLinesForBudgetPrimitifAsync(_budgetPrimitifId);

                var filtered = all
                    .Where(b => b.Nommenclature != null &&
                                b.Nommenclature.Nature == filter.nature &&
                                b.Nommenclature.Section == filter.section)
                    .OrderBy(b => b.Nommenclature.code())
                    //.ThenBy(b => b.Nommenclature.Article)
                    //.ThenBy(b => b.Nommenclature.Paragraphe)
                    //.ThenBy(b => b.Nommenclature.SousParagraphe)
                    .ToList();

                DisplayedLines.Clear();
                foreach (var line in filtered)
                {
                    DisplayedLines.Add(line);
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

        // ═══════════════════════════════════════════════════════════
        // DIALOG - AJOUT
        // ═══════════════════════════════════════════════════════════

        private async Task OpenAddDialogAsync()
        {
            if (!CanModifyBudget)
            {
                MessageBox.Show(
                    "Ce budget est validé et ne peut plus être modifié.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_budgetPrimitifId == 0)
            {
                MessageBox.Show(
                    "Aucun budget primitif disponible pour cet exercice.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var filter = TabToFilter(SelectedTabIndex);
                var available = await _service.GetLeafNomenclaturesNotLinkedAsync(
                    _budgetPrimitifId,
                    filter.nature,
                    filter.section
                );

                if (!available.Any())
                {
                    MessageBox.Show(
                        "Toutes les nomenclatures disponibles sont déjà liées à ce budget.",
                        "Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Préparer le dialog en mode ajout
                IsEditMode = false;
                _currentLine = null;
                SelectedNomenclature = null;
                MontantPrevu = "0";

                AvailableNomenclatures.Clear();
                foreach (var nom in available.OrderBy(n => n.Intitule))
                {
                    AvailableNomenclatures.Add(nom);
                }

                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DIALOG - MODIFICATION
        // ═══════════════════════════════════════════════════════════

        private async Task OpenEditDialogAsync(BudgetLine? line)
        {
            if (line == null) return;

            if (!CanModifyBudget)
            {
                MessageBox.Show(
                    "Ce budget est validé et ne peut plus être modifié.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                // Préparer le dialog en mode édition
                IsEditMode = true;
                _currentLine = line;
                SelectedNomenclature = null;
                MontantPrevu = line.MontantPrevu.ToString();

                OnPropertyChanged(nameof(NomenclatureLibelle));

                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DIALOG - VALIDATION & SAUVEGARDE
        // ═══════════════════════════════════════════════════════════

        private bool CanSaveDialog()
        {
            if (IsEditMode)
            {
                return int.TryParse(MontantPrevu, out var montant) && montant >= 0;
            }
            else
            {
                return SelectedNomenclature != null &&
                       int.TryParse(MontantPrevu, out var montant) &&
                       montant >= 0;
            }
        }

        private async Task SaveDialogAsync()
        {
            try
            {
                if (!int.TryParse(MontantPrevu, out var montant))
                {
                    MessageBox.Show(
                        "Le montant doit être un nombre entier valide.",
                        "Erreur de validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (montant < 0)
                {
                    MessageBox.Show(
                        "Le montant ne peut pas être négatif.",
                        "Erreur de validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                IsDialogOpen = false;

                if (IsEditMode)
                {
                    // Mode édition
                    if (_currentLine == null) return;

                    var (success, message, _) = await _service.UpdateBudgetLineAsync(
                        _currentLine.Id,
                        montant);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        await LoadForSelectedTabAsync();
                    }
                }
                else
                {
                    // Mode ajout
                    if (SelectedNomenclature == null)
                    {
                        MessageBox.Show(
                            "Veuillez sélectionner une nomenclature.",
                            "Erreur de validation",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        IsLoading = false;
                        IsDialogOpen = true;
                        return;
                    }

                    var newLine = await _service.CreateBudgetLineAsync(
                        _budgetPrimitifId,
                        SelectedNomenclature.Id,
                        montant);

                    MessageBox.Show(
                        $"✅ Ligne budgétaire créée avec succès.\n\n" +
                        $"Nomenclature : {SelectedNomenclature.Intitule}\n" +
                        $"Montant : {montant:N0} GNF\n\n" +
                        $"Les montants des parents ont été recalculés.",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await LoadForSelectedTabAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CloseDialog()
        {
            IsDialogOpen = false;
            SelectedNomenclature = null;
            MontantPrevu = "0";
            _currentLine = null;
        }

        // ═══════════════════════════════════════════════════════════
        // SUPPRESSION
        // ═══════════════════════════════════════════════════════════

        private async Task DeleteLineAsync(BudgetLine? line)
        {
            if (line == null) return;

            if (!CanModifyBudget)
            {
                MessageBox.Show(
                    "Ce budget est validé et ne peut plus être modifié.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cette ligne ?\n\n" +
                $"Nomenclature : {line.Nommenclature?.Intitule ?? "N/A"}\n" +
                $"Montant : {line.MontantPrevu:N0} GNF\n\n" +
                $"⚠️ Les montants des parents seront recalculés automatiquement.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                try
                {
                    var (success, message) = await _service.DeleteBudgetLineAsync(line.Id);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        await LoadForSelectedTabAsync();
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
                }
            }
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
    }
}