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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Collectivite.ViewModels
{
    // ═══════════════════════════════════════════════════════════
    // 🆕 CLASSE VIEWMODEL POUR LA HIÉRARCHIE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel pour affichage hiérarchique des lignes budgétaires
    /// </summary>
    public class BudgetLineHierarchyViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isVisible = true;

        public BudgetLine BudgetLine { get; set; }
        public ObservableCollection<BudgetLineHierarchyViewModel> Children { get; set; }
        public BudgetLineHierarchyViewModel? Parent { get; set; }

        /// <summary>
        /// Niveau hiérarchique (0=Chapitre, 1=Article, 2=Paragraphe, 3=SousParagraphe)
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Indique si l'élément est plié ou déplié
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpanderIcon));
                    UpdateChildrenVisibility();
                }
            }
        }

        /// <summary>
        /// Contrôle la visibilité de la ligne
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indique si l'élément a des enfants
        /// </summary>
        public bool HasChildren => Children != null && Children.Count > 0;

        /// <summary>
        /// Indentation basée sur le niveau
        /// </summary>
        public Thickness IndentationMargin => new Thickness(Level * 20, 0, 0, 0);

        /// <summary>
        /// Couleur de fond selon le niveau
        /// </summary>
        public string BackgroundColor
        {
            get
            {
                return Level switch
                {
                    0 => "#FFCDD2", // Rouge clair (Chapitre)
                    1 => "#FFF9C4", // Jaune clair (Article)
                    2 => "#C8E6C9", // Vert clair (Paragraphe)
                    _ => "Transparent" // Sous-paragraphe
                };
            }
        }

        /// <summary>
        /// Icône de pliage/dépliage
        /// </summary>
        public string ExpanderIcon
        {
            get
            {
                if (!HasChildren) return "";
                return IsExpanded ? "ChevronDown" : "ChevronRight";
            }
        }

        /// <summary>
        /// Poids de la police selon le niveau
        /// </summary>
        public FontWeight TextFontWeight
        {
            get
            {
                return Level switch
                {
                    0 => FontWeights.Bold,      // Chapitre
                    1 => FontWeights.SemiBold,  // Article
                    2 => FontWeights.Medium,    // Paragraphe
                    _ => FontWeights.Normal     // Sous-paragraphe
                };
            }
        }

        public BudgetLineHierarchyViewModel(BudgetLine budgetLine, int level)
        {
            BudgetLine = budgetLine;
            Level = level;
            Children = new ObservableCollection<BudgetLineHierarchyViewModel>();
            _isExpanded = false; // Par défaut tout est plié
        }

        /// <summary>
        /// Bascule l'état plié/déplié
        /// </summary>
        public void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        /// <summary>
        /// Met à jour la visibilité des enfants
        /// </summary>
        private void UpdateChildrenVisibility()
        {
            if (Children == null) return;

            foreach (var child in Children)
            {
                child.IsVisible = IsExpanded;
                if (!IsExpanded)
                {
                    // Si on replie, on replie aussi tous les descendants
                    child.IsExpanded = false;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ═══════════════════════════════════════════════════════════
    // VIEWMODEL PRINCIPAL
    // ═══════════════════════════════════════════════════════════

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

        // 🆕 Collections pour la hiérarchie
        private List<BudgetLineHierarchyViewModel> _fullHierarchy = new();

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS - GÉNÉRAL
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<BudgetLineHierarchyViewModel> DisplayedLines { get; } = new();

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
        public ICommand ToggleExpandCommand { get; } // 🆕

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════════════════════

        public BudgetLinesViewModel(BudgetLineService service)
        {
            _service = service;
            _exerciceService = ExerciceService.Instance;

            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            // Commandes principales
            AddCommand = new RelayCommand(async _ => await OpenAddDialogAsync(), _ => CanModifyBudget);
            OpenEditDialogCommand = new RelayCommand<BudgetLine>(async line => await OpenEditDialogAsync(line));
            DeleteCommand = new RelayCommand<BudgetLine>(async line => await DeleteLineAsync(line));
            RefreshCommand = new RelayCommand(async _ => await LoadForSelectedTabAsync());

            // 🆕 Commande pour plier/déplier
            ToggleExpandCommand = new RelayCommand<BudgetLineHierarchyViewModel>(ToggleExpand);

            // Commandes du dialog
            SaveDialogCommand = new RelayCommand(async _ => await SaveDialogAsync(), _ => CanSaveDialog());
            CancelDialogCommand = new RelayCommand(_ => CloseDialog());

            // Charger les données initiales
            _ = InitializeAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 🆕 GESTION DE LA HIÉRARCHIE - TOGGLE
        // ═══════════════════════════════════════════════════════════

        private void ToggleExpand(BudgetLineHierarchyViewModel? item)
        {
            if (item == null) return;

            item.ToggleExpanded();
            RefreshDisplayedLines();
        }

        // ═══════════════════════════════════════════════════════════
        // 🆕 CONSTRUCTION DE LA HIÉRARCHIE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Construit la hiérarchie des lignes budgétaires basée sur la nomenclature
        /// </summary>
        private List<BudgetLineHierarchyViewModel> BuildHierarchy(
            List<BudgetLine> budgetLines,
            NatureType nature,
            SectionType section)
        {
            // Filtrer les lignes selon nature et section
            var filteredLines = budgetLines
                .Where(bl => bl.Nommenclature.Nature == nature &&
                            bl.Nommenclature.Section == section)
                .ToList();

            // Identifier les chapitres (ParentId == null)
            var chapitres = filteredLines
                .Where(bl => bl.Nommenclature.ParentId == null)
                .Select(bl => CreateViewModel(bl, 0, filteredLines))
                .OrderBy(vm => vm.BudgetLine.Nommenclature.Chapitre)
                .ToList();

            return chapitres;
        }

        /// <summary>
        /// Crée un ViewModel avec ses enfants récursivement
        /// </summary>
        private BudgetLineHierarchyViewModel CreateViewModel(
            BudgetLine budgetLine,
            int level,
            List<BudgetLine> allLines)
        {
            var viewModel = new BudgetLineHierarchyViewModel(budgetLine, level);

            // Trouver les enfants directs basés sur la nomenclature
            var children = allLines
                .Where(bl => bl.Nommenclature.ParentId == budgetLine.Nommenclature.Id)
                .Select(bl => CreateViewModel(bl, level + 1, allLines))
                .OrderBy(vm => GetOrderKey(vm.BudgetLine.Nommenclature))
                .ToList();

            foreach (var child in children)
            {
                child.Parent = viewModel;
                viewModel.Children.Add(child);
            }

            return viewModel;
        }

        /// <summary>
        /// Génère une clé de tri pour la nomenclature
        /// </summary>
        private string GetOrderKey(Nommenclature n)
        {
            return $"{n.Chapitre ?? ""}|{n.Article ?? ""}|{n.Paragraphe ?? ""}|{n.SousParagraphe ?? ""}";
        }

        /// <summary>
        /// Aplatit la hiérarchie pour l'affichage dans la DataGrid
        /// </summary>
        private List<BudgetLineHierarchyViewModel> FlattenHierarchy(
            IEnumerable<BudgetLineHierarchyViewModel> hierarchy)
        {
            var result = new List<BudgetLineHierarchyViewModel>();

            foreach (var item in hierarchy)
            {
                result.Add(item);
                if (item.IsExpanded && item.HasChildren)
                {
                    result.AddRange(FlattenHierarchy(item.Children));
                }
            }

            return result;
        }

        /// <summary>
        /// Rafraîchit l'affichage aplati
        /// </summary>
        private void RefreshDisplayedLines()
        {
            var flatList = FlattenHierarchy(_fullHierarchy)
                .Where(vm => vm.IsVisible)
                .ToList();

            DisplayedLines.Clear();
            foreach (var item in flatList)
            {
                DisplayedLines.Add(item);
            }
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
        // CHARGEMENT DES DONNÉES AVEC HIÉRARCHIE
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
                    _fullHierarchy.Clear();
                    return;
                }

                var filter = TabToFilter(SelectedTabIndex);
                var all = await _service.GetBudgetLinesForBudgetPrimitifAsync(_budgetPrimitifId);

                // 🆕 Construire la hiérarchie au lieu de simplement filtrer
                _fullHierarchy = BuildHierarchy(all, filter.nature, filter.section);

                // 🆕 Afficher la vue aplatie
                RefreshDisplayedLines();
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