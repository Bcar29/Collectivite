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
// Ajoutez ces using en haut du fichier
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using Microsoft.Win32;

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
            _isExpanded = true; // Par défaut tout est plié
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
        private readonly AuditService _auditService;
        private readonly AuthService _authService;

        // 🆕 Collections pour la hiérarchie
        private List<BudgetLineHierarchyViewModel> _fullHierarchy = new();

        #region proprietes
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

        //propriete des totaux 
        private decimal _totalRecetteFonctionnement;
        public decimal TotalRecetteFonctionnement
        {
            get => _totalRecetteFonctionnement;
            set => SetProperty(ref _totalRecetteFonctionnement, value);
        }

        private decimal _totalRecetteInvestissement;
        public decimal TotalRecetteInvestissement
        {
            get => _totalRecetteInvestissement;
            set => SetProperty(ref _totalRecetteInvestissement, value);
        }

        private decimal _totalRecetteReelsInvestissement;
        public decimal TotalRecetteReelsInvestissement
        {
            get => _totalRecetteReelsInvestissement;
            set => SetProperty(ref _totalRecetteReelsInvestissement, value);
        }

        private decimal _totalGeneralRecettesReels;
        public decimal TotalGeneralRecettesReels
        {
            get => _totalGeneralRecettesReels;
            set => SetProperty(ref _totalGeneralRecettesReels, value);
        }

        private decimal _totalDepenseFonctionnement;
        public decimal TotalDepenseFonctionnement
        {
            get => _totalDepenseFonctionnement;
            set => SetProperty(ref _totalDepenseFonctionnement, value);
        }

        private decimal _totalDepenseReelsFonctionnement;
        public decimal TotalDepenseReelsFonctionnement
        {
            get => _totalDepenseReelsFonctionnement;
            set => SetProperty(ref _totalDepenseReelsFonctionnement, value);
        }

        private decimal _totalDepenseInvestissement;
        public decimal TotalDepenseInvestissement
        {
            get => _totalDepenseInvestissement;
            set => SetProperty(ref _totalDepenseInvestissement, value);
        }

        private decimal _totalGeneralDepensesReels;
        public decimal TotalGeneralDepensesReels
        {
            get => _totalGeneralDepensesReels;
            set => SetProperty(ref _totalGeneralDepensesReels, value);
        }

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
        #endregion
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
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportPdfCompteAdminCommand { get; }
        public ICommand ExportPdfCompteGestionCommand { get; }
        public ICommand PrintCommand { get; }
        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════════════════════

        public BudgetLinesViewModel(BudgetLineService service, AuthService authService, AuditService auditService)
        {
            _service = service;
            _exerciceService = ExerciceService.Instance;
            _authService = authService;
            _auditService = auditService;


            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            // Commandes principales
            AddCommand = new RelayCommand(async _ => await OpenAddDialogAsync(), _ => CanModifyBudget);
            OpenEditDialogCommand = new RelayCommand<BudgetLine>( line =>  OpenEditDialogAsync(line));
            DeleteCommand = new RelayCommand<BudgetLine>(async line => await DeleteLineAsync(line));
            RefreshCommand = new RelayCommand(async _ => await LoadForSelectedTabAsync());

            // 🆕 Commande pour plier/déplier
            ToggleExpandCommand = new RelayCommand<BudgetLineHierarchyViewModel>(ToggleExpand);

            // Commandes du dialog
            SaveDialogCommand = new RelayCommand(async _ => await SaveDialogAsync(), _ => CanSaveDialog());
            CancelDialogCommand = new RelayCommand(_ => CloseDialog());

            ExportPdfCommand = new RelayCommand(async _ => await ExportToPdfAsync());
            ExportPdfCompteAdminCommand = new RelayCommand(async _ => await ExportToPdfCompteAdminAsync());
            ExportPdfCompteGestionCommand = new RelayCommand(async _ => await ExportToPdfCompteGestionAsync());
            PrintCommand = new RelayCommand(_ => Print());

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

                TotalRecetteFonctionnement = _service.RecetteFonctionnementPrevu(all);
                TotalRecetteInvestissement = _service.RecetteInvestissementPrevu(all);
                TotalRecetteReelsInvestissement = _service.TotalRecetteReelInvestissementPrevu(all);
                TotalGeneralRecettesReels = _service.TotalGeneralRecetteReelPrevu(all);
                TotalDepenseFonctionnement = _service.DepenseFonctionnementPrevu(all);
                TotalDepenseReelsFonctionnement = _service.TotalDepenseReelFonctionnementPrevu(all);
                TotalDepenseInvestissement = _service.DepenseInvestissementPrevu(all);
                TotalGeneralDepensesReels = _service.TotalGeneralDepenseReelPrevu(all);
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

        private void  OpenEditDialogAsync(BudgetLine? line)
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
                return decimal.TryParse(MontantPrevu, out var montant) && montant >= 0;
            }
            else
            {
                return SelectedNomenclature != null &&
                       decimal.TryParse(MontantPrevu, out var montant) &&
                       montant >= 0;
            }
        }

        private async Task SaveDialogAsync()
        {
            try
            {
                if (!decimal.TryParse(MontantPrevu, out var montant))
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

                    var (success, message, bl) = await _service.UpdateBudgetLineAsync(
                        _currentLine.Id,
                        montant);
                    

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";
                        await _auditService.LogAsync(
                                   "Prevision modifié ",
                                   $"{bl?.Nommenclature.code()} montant : {bl?.MontantPrevu} {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                                   username);
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

                    var username = _authService.CurrentUser?.Username ?? "Utilisateur inconnu";
                    await _auditService.LogAsync(
                                "Nouvelle Prevision ",
                                $"{newLine.Nommenclature.code()} montant : {newLine.MontantPrevu} {username} le {DateTime.Now:dd/MM/yyyy HH:mm}",
                                username);

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
                MessageBox.Show($"❌ Erreur  : {ex.Message}",
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
                $"Nomenclature : {line.Nommenclature?.Intitule ?? "Non défini"}\n" +
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

        private async Task ExportToPdfAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF|*.pdf",
                    FileName = $"LignesBudgetaires_{GetTabName(SelectedTabIndex)}_{_exerciceService.CurrentExercice?.Libelle}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => GeneratePdfBudgetPrimitif(saveFileDialog.FileName));

                    MessageBox.Show(
                        "Export PDF réalisé avec succès !",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Ouvrir le fichier
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task ExportToPdfCompteAdminAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF|*.pdf",
                    FileName = $"Compte Administratif_{GetTabName(SelectedTabIndex)}_{_exerciceService.CurrentExercice?.Libelle}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => GeneratePdfCompteAdmin(saveFileDialog.FileName));

                    MessageBox.Show(
                        "Export PDF réalisé avec succès !",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Ouvrir le fichier
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task ExportToPdfCompteGestionAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF|*.pdf",
                    FileName = $"Compte Gestion{GetTabName(SelectedTabIndex)}_{_exerciceService.CurrentExercice?.Libelle}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => GeneratePdfCompteGestion(saveFileDialog.FileName));

                    MessageBox.Show(
                        "Export PDF réalisé avec succès !",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Ouvrir le fichier
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetTabName(int tabIndex)
        {
            return tabIndex switch
            {
                0 => "Recette_Fonctionnement",
                1 => "Recette_Investissement",
                2 => "Depense_Fonctionnement",
                3 => "Depense_Investissement",
                _ => "Budget"
            };
        }

        private string GetTabFullName(int tabIndex)
        {
            return tabIndex switch
            {
                0 => "Recette - Fonctionnement",
                1 => "Recette - Investissement",
                2 => "Dépense - Fonctionnement",
                3 => "Dépense - Investissement",
                _ => "Budget"
            };
        }

        private void GeneratePdfBudgetPrimitif(string filePath)
        {
            // ✅ Format paysage déjà présent : PageSize.A4.Rotate()
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

            document.Open();

            // Polices
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            // Titre
            Paragraph title = new Paragraph($"Lignes Budgétaires - {GetTabFullName(SelectedTabIndex)}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10;
            document.Add(title);

            // Sous-titre avec exercice
            Paragraph subtitle = new Paragraph($"Exercice : {_exerciceService.CurrentExercice?.Libelle ?? "N/A"}", headerFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 20;
            document.Add(subtitle);

            // Date d'export
            Paragraph dateExport = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", normalFont);
            dateExport.Alignment = Element.ALIGN_RIGHT;
            dateExport.SpacingAfter = 20;
            document.Add(dateExport);

            // Tableau principal
            PdfPTable table = new PdfPTable(6) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 12f, 10f, 10f, 12f, 40f, 16f });

            // En-têtes
            AddCellWithColor(table, "Chapitre", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Article", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Sous-Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Intitulé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Montant Prévu", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // Données hiérarchiques
            var flatList = FlattenHierarchy(_fullHierarchy);
            foreach (var item in flatList)
            {
                // Déterminer la couleur de fond selon le niveau
                BaseColor bgColor = item.Level switch
                {
                    0 => new BaseColor(255, 205, 210), // Rouge clair #FFCDD2
                    1 => new BaseColor(255, 249, 196), // Jaune clair #FFF9C4
                    2 => new BaseColor(200, 230, 201), // Vert clair #C8E6C9
                    _ => BaseColor.WHITE
                };

                // Police selon le niveau
                var cellFont = item.Level switch
                {
                    0 => boldFont,
                    1 => boldFont,
                    2 => normalFont,
                    _ => normalFont
                };

                // Indentation pour le chapitre
                string indentation = new string(' ', item.Level * 2);

                AddCellWithColor(table, indentation + (item.BudgetLine.Nommenclature.Chapitre ?? ""), cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Article ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Paragraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.SousParagraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Intitule ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantPrevu:N0} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
            }

            document.Add(table);

            // Espace avant les totaux
            document.Add(new Paragraph(" "));

            // Totaux selon l'onglet
            AddTotalsSection(document, headerFont, boldFont, normalFont);

            document.Close();
            writer.Close();
        }
        private void GeneratePdfCompteAdmin(string filePath)
        {
            // ✅ Format paysage déjà présent : PageSize.A4.Rotate()
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

            document.Open();

            // Polices
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            // Titre
            Paragraph title = new Paragraph($"Compte Administratif - {GetTabFullName(SelectedTabIndex)}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10;
            document.Add(title);

            // Sous-titre avec exercice
            Paragraph subtitle = new Paragraph($"Exercice : {_exerciceService.CurrentExercice?.Libelle ?? "N/A"}", headerFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 20;
            document.Add(subtitle);

            // Date d'export
            Paragraph dateExport = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", normalFont);
            dateExport.Alignment = Element.ALIGN_RIGHT;
            dateExport.SpacingAfter = 20;
            document.Add(dateExport);

            // Tableau principal
            PdfPTable table = new PdfPTable(9) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 10f, 8f, 8f, 10f, 30f, 12f, 12f, 10f, 12f });

            // En-têtes
            AddCellWithColor(table, "Chapitre", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Article", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Sous-Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Intitulé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Montant Prévu", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(table, "Montant Réalisé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(table, "Taux Réalisation", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(table, "Reste Réalisé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // Données hiérarchiques
            var flatList = FlattenHierarchy(_fullHierarchy);
            foreach (var item in flatList)
            {
                // Déterminer la couleur de fond selon le niveau
                BaseColor bgColor = item.Level switch
                {
                    0 => new BaseColor(255, 205, 210), // Rouge clair #FFCDD2
                    1 => new BaseColor(255, 249, 196), // Jaune clair #FFF9C4
                    2 => new BaseColor(200, 230, 201), // Vert clair #C8E6C9
                    _ => BaseColor.WHITE
                };

                // Police selon le niveau
                var cellFont = item.Level switch
                {
                    0 => boldFont,
                    1 => boldFont,
                    2 => normalFont,
                    _ => normalFont
                };

                // Indentation pour le chapitre
                string indentation = new string(' ', item.Level * 2);

                AddCellWithColor(table, indentation + (item.BudgetLine.Nommenclature.Chapitre ?? ""), cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Article ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Paragraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.SousParagraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Intitule ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantDefinitif:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantRealise:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.TauxRealisation:N2} %", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.ResteRealise:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
            }

            document.Add(table);

            // Espace avant les totaux
            document.Add(new Paragraph(" "));

            // Totaux selon l'onglet
            AddTotalsSection(document, headerFont, boldFont, normalFont);

            document.Close();
            writer.Close();
        }
        private void GeneratePdfCompteGestion(string filePath)
        {
            // ✅ Format paysage déjà présent : PageSize.A4.Rotate()
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

            document.Open();

            // Polices
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            // Titre
            Paragraph title = new Paragraph($"Compte de Gestion - {GetTabFullName(SelectedTabIndex)}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10;
            document.Add(title);

            // Sous-titre avec exercice
            Paragraph subtitle = new Paragraph($"Exercice : {_exerciceService.CurrentExercice?.Libelle ?? "N/A"}", headerFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 20;
            document.Add(subtitle);

            // Date d'export
            Paragraph dateExport = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", normalFont);
            dateExport.Alignment = Element.ALIGN_RIGHT;
            dateExport.SpacingAfter = 20;
            document.Add(dateExport);

            // 🆕 Déterminer si on est en Recette ou Dépense selon l'onglet
            bool isRecette = SelectedTabIndex == 0 || SelectedTabIndex == 1; // 0=Recette Fonct, 1=Recette Invest

            // Tableau principal
            PdfPTable table = new PdfPTable(10) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 10f, 8f, 8f, 10f, 28f, 11f, 11f, 11f, 10f, 11f });

            // 🆕 En-têtes adaptés selon Recette/Dépense
            AddCellWithColor(table, "Chapitre", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Article", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Sous-Paragraphe", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Intitulé", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
            AddCellWithColor(table, "Montant Prévu", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);
            AddCellWithColor(table, "Montant Émis", headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // 🆕 Colonne adaptée : "Montant Recouvré" pour Recette, "Montant Payé" pour Dépense
            AddCellWithColor(table, isRecette ? "Montant Recouvré" : "Montant Payé",
                headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // 🆕 Colonne adaptée : "Taux Recouvrement" pour Recette, "Taux Paiement" pour Dépense
            AddCellWithColor(table, isRecette ? "Taux Recouvrement" : "Taux Paiement",
                headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // 🆕 Colonne adaptée : "Reste à Recouvrer" pour Recette, "Reste à Payer" pour Dépense
            AddCellWithColor(table, isRecette ? "Reste à Recouvrer" : "Reste à Payer",
                headerFont, BaseColor.LIGHT_GRAY, Element.ALIGN_RIGHT);

            // Données hiérarchiques
            var flatList = FlattenHierarchy(_fullHierarchy);
            foreach (var item in flatList)
            {
                // Déterminer la couleur de fond selon le niveau
                BaseColor bgColor = item.Level switch
                {
                    0 => new BaseColor(255, 205, 210), // Rouge clair #FFCDD2
                    1 => new BaseColor(255, 249, 196), // Jaune clair #FFF9C4
                    2 => new BaseColor(200, 230, 201), // Vert clair #C8E6C9
                    _ => BaseColor.WHITE
                };

                // Police selon le niveau
                var cellFont = item.Level switch
                {
                    0 => boldFont,
                    1 => boldFont,
                    2 => normalFont,
                    _ => normalFont
                };

                // Indentation pour le chapitre
                string indentation = new string(' ', item.Level * 2);

                AddCellWithColor(table, indentation + (item.BudgetLine.Nommenclature.Chapitre ?? ""), cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Article ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Paragraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.SousParagraphe ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, item.BudgetLine.Nommenclature.Intitule ?? "", cellFont, bgColor, Element.ALIGN_LEFT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantDefinitif:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantRealise:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.MontantEntreSortie:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.TauxEntreSortie:N2} %", cellFont, bgColor, Element.ALIGN_RIGHT);
                AddCellWithColor(table, $"{item.BudgetLine.ResteEntreSortie:N2} GNF", cellFont, bgColor, Element.ALIGN_RIGHT);
            }

            document.Add(table);

            // Espace avant les totaux
            document.Add(new Paragraph(" "));

            // Totaux selon l'onglet
            AddTotalsSection(document, headerFont, boldFont, normalFont);

            document.Close();
            writer.Close();
        }
        private void AddCellWithColor(PdfPTable table, string text, iTextSharp.text.Font font, BaseColor backgroundColor, int alignment)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            cell.HorizontalAlignment = alignment;
            cell.BackgroundColor = backgroundColor;
            cell.BorderWidth = 0.5f;
            cell.BorderColor = BaseColor.GRAY;
            table.AddCell(cell);
        }

        private void AddTotalsSection(Document document, iTextSharp.text.Font headerFont, iTextSharp.text.Font boldFont, iTextSharp.text.Font normalFont)
        {
            Paragraph totalsTitle = new Paragraph("Totaux", headerFont);
            totalsTitle.SpacingBefore = 15;
            totalsTitle.SpacingAfter = 10;
            document.Add(totalsTitle);

            PdfPTable totalsTable = new PdfPTable(2) { WidthPercentage = 60 };
            totalsTable.SetWidths(new float[] { 70f, 30f });

            switch (SelectedTabIndex)
            {
                case 0: // Recette - Fonctionnement
                    AddCellWithColor(totalsTable, "Total Recettes de Fonctionnement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteFonctionnement:N2} GNF", boldFont, new BaseColor(200, 230, 201), Element.ALIGN_RIGHT);
                    break;

                case 1: // Recette - Investissement
                    AddCellWithColor(totalsTable, "Total Recettes d'Investissement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteInvestissement:N2} GNF", boldFont, new BaseColor(200, 230, 201), Element.ALIGN_RIGHT);

                    AddCellWithColor(totalsTable, "Total Recettes Réels d'Investissement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalRecetteReelsInvestissement:N2} GNF", boldFont, new BaseColor(165, 214, 167), Element.ALIGN_RIGHT);

                    AddCellWithColor(totalsTable, "Total Général des Recettes Réels", headerFont, new BaseColor(66, 165, 245), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalGeneralRecettesReels:N2} GNF", headerFont, new BaseColor(144, 202, 249), Element.ALIGN_RIGHT);
                    break;

                case 2: // Dépense - Fonctionnement
                    AddCellWithColor(totalsTable, "Total Dépenses de Fonctionnement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseFonctionnement:N2} GNF", boldFont, new BaseColor(239, 154, 154), Element.ALIGN_RIGHT);

                    AddCellWithColor(totalsTable, "Total Dépenses Réels de Fonctionnement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseReelsFonctionnement:N2} GNF", boldFont, new BaseColor(229, 115, 115), Element.ALIGN_RIGHT);
                    break;

                case 3: // Dépense - Investissement
                    AddCellWithColor(totalsTable, "Total Dépenses d'Investissement", boldFont, BaseColor.LIGHT_GRAY, Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalDepenseInvestissement:N2} GNF", boldFont, new BaseColor(239, 154, 154), Element.ALIGN_RIGHT);

                    AddCellWithColor(totalsTable, "Total Général des Dépenses Réels", headerFont, new BaseColor(198, 40, 40), Element.ALIGN_LEFT);
                    AddCellWithColor(totalsTable, $"{TotalGeneralDepensesReels:N2} GNF", headerFont, new BaseColor(244, 143, 177), Element.ALIGN_RIGHT);
                    break;
            }

            document.Add(totalsTable);
        }

        private void Print()
        {
            MessageBox.Show(
                "Fonctionnalité d'impression en cours de développement.\n" +
                "Veuillez utiliser l'export PDF puis imprimer le fichier généré.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}