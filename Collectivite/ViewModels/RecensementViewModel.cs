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
    public class RecensementViewModel : ViewModelBase
    {
        private readonly RecensementService _recensementService;

        private bool _isLoading;
        private Recensement? _selectedRecensement;
        private bool _isDialogOpen;
        private Recensement _dialogRecensement;
        private bool _isEditMode;
        private string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";

        // ═══════════════════════════════════════════════════════════
        // 🆕 PROPRIÉTÉS DE FILTRAGE
        // ═══════════════════════════════════════════════════════════
        private Exercice? _selectedFilterExercice;
        private Commune? _selectedFilterCommune;
        private Tiers? _selectedFilterTiers;
        private BudgetLine? _selectedFilterBudgetLine;
        private string _searchText = string.Empty;
        private decimal? _montantMin;
        private decimal? _montantMax;
        private bool _isFilterPanelExpanded = true;

        // Collection complète (non filtrée)
        private readonly ObservableCollection<Recensement> _allRecensements = new();

        public RecensementViewModel(RecensementService recensementService)
        {
            _recensementService = recensementService;

            _dialogRecensement = new Recensement
            {
                MontantRecense = 0
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddDialogCommand = new RelayCommand(async _ => await OpenAddDialogAsync());
            OpenEditDialogCommand = new RelayCommand<Recensement>(r => OpenEditDialog(r));
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<Recensement>(async r => await DeleteAsync(r));

            // 🆕 Commandes de filtrage
            ApplyFilterCommand = new RelayCommand(_ => ApplyFilters());
            ClearFilterCommand = new RelayCommand(_ => ClearFilters());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        // Collection affichée (filtrée)
        public ObservableCollection<Recensement> Recensements { get; } = new();
        public ObservableCollection<BudgetLine> LignesBudgetaires { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Commune> Communes { get; } = new();
        public ObservableCollection<Tiers> TiersList { get; } = new();

        // Permissions dynamiques
        public bool CanViewRecensement => SessionManager.HasPermission("Recensement.View");
        public bool CanCreateRecensement => SessionManager.HasPermission("Recensement.Create");
        public bool CanEditRecensement => SessionManager.HasPermission("Recensement.Edit");
        public bool CanDeleteRecensement => SessionManager.HasPermission("Recensement.Delete");

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Recensement? SelectedRecensement
        {
            get => _selectedRecensement;
            set => SetProperty(ref _selectedRecensement, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Recensement DialogRecensement
        {
            get => _dialogRecensement;
            set => SetProperty(ref _dialogRecensement, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier le recensement" : "Nouveau recensement";

        // Total calculé sur les données FILTRÉES
        public double TotalRecense => Recensements.Sum(r => r.MontantRecense);

        // Nombre d'éléments
        public int NombreRecensements => Recensements.Count;
        public int NombreTotalRecensements => _allRecensements.Count;

        #endregion

        #region Propriétés de filtrage

        public bool IsFilterPanelExpanded
        {
            get => _isFilterPanelExpanded;
            set => SetProperty(ref _isFilterPanelExpanded, value);
        }

        public Exercice? SelectedFilterExercice
        {
            get => _selectedFilterExercice;
            set
            {
                if (SetProperty(ref _selectedFilterExercice, value))
                {
                    ApplyFilters();
                }
            }
        }

        public Commune? SelectedFilterCommune
        {
            get => _selectedFilterCommune;
            set
            {
                if (SetProperty(ref _selectedFilterCommune, value))
                {
                    ApplyFilters();
                }
            }
        }

        public Tiers? SelectedFilterTiers
        {
            get => _selectedFilterTiers;
            set
            {
                if (SetProperty(ref _selectedFilterTiers, value))
                {
                    ApplyFilters();
                }
            }
        }

        public BudgetLine? SelectedFilterBudgetLine
        {
            get => _selectedFilterBudgetLine;
            set
            {
                if (SetProperty(ref _selectedFilterBudgetLine, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        public decimal? MontantMin
        {
            get => _montantMin;
            set
            {
                if (SetProperty(ref _montantMin, value))
                {
                    ApplyFilters();
                }
            }
        }

        public decimal? MontantMax
        {
            get => _montantMax;
            set
            {
                if (SetProperty(ref _montantMax, value))
                {
                    ApplyFilters();
                }
            }
        }

        // Indicateur si des filtres sont actifs
        public bool HasActiveFilters =>
            SelectedFilterExercice != null ||
            SelectedFilterCommune != null ||
            SelectedFilterTiers != null ||
            SelectedFilterBudgetLine != null ||
            !string.IsNullOrWhiteSpace(SearchText) ||
            MontantMin.HasValue ||
            MontantMax.HasValue;

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }

        // 🆕 Commandes de filtrage
        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Vérifier la permission de consultation
                if (!CanViewRecensement)
                {
                    MessageBox.Show(
                        "Accès refusé : vous n'avez pas la permission de consulter les recensements.",
                        "Accès refusé",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    Recensements.Clear();
                    _allRecensements.Clear();
                    return;
                }

                // Charger tous les recensements
                var recensements = await _recensementService.GetAllRecensementsAsync();

                _allRecensements.Clear();
                foreach (var r in recensements)
                {
                    _allRecensements.Add(r);
                }

                // Appliquer les filtres (ou afficher tout si aucun filtre)
                ApplyFilters();

                // ✅ Charger TOUTES les lignes budgétaires
                var lignesBudgetaires = await _recensementService.GetAllBudgetLinesAsync();

                LignesBudgetaires.Clear();
                foreach (var lb in lignesBudgetaires)
                {
                    LignesBudgetaires.Add(lb);
                }

                System.Diagnostics.Debug.WriteLine($"✅ {LignesBudgetaires.Count} lignes budgétaires chargées");

                // Charger les exercices
                using (var context = new AppDbContext())
                {
                    var exerciceService = new ExerciceService();
                    var exercices = await exerciceService.GetAllExerciceAsync();

                    Exercices.Clear();
                    foreach (var ex in exercices.Where(e => !e.EstCloture))
                    {
                        Exercices.Add(ex);
                    }
                }

                // Charger les communes
                using (var context = new AppDbContext())
                {
                    var communeService = new CommuneService();
                    var communes = await communeService.GetAllCommuneAsync();

                    Communes.Clear();
                    foreach (var c in communes)
                    {
                        Communes.Add(c);
                    }
                }

                // Charger les tiers
                var tiersService = new TiersService();
                var tiers = await tiersService.GetTiersActifsAsync();

                TiersList.Clear();
                foreach (var t in tiers)
                {
                    TiersList.Add(t);
                }

                RefreshStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERREUR : {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Applique les filtres sur la collection
        /// </summary>
        private void ApplyFilters()
        {
            var filtered = _allRecensements.AsEnumerable();

            // Filtre par exercice
            if (SelectedFilterExercice != null)
            {
                filtered = filtered.Where(r => r.ExerciceId == SelectedFilterExercice.Id);
            }

            // Filtre par commune
            if (SelectedFilterCommune != null)
            {
                filtered = filtered.Where(r => r.CommuneId == SelectedFilterCommune.Id);
            }

            // Filtre par tiers
            if (SelectedFilterTiers != null)
            {
                filtered = filtered.Where(r => r.TiersId == SelectedFilterTiers.Id);
            }

            // Filtre par ligne budgétaire
            if (SelectedFilterBudgetLine != null)
            {
                filtered = filtered.Where(r => r.BudgetLineId == SelectedFilterBudgetLine.Id);
            }

            // Filtre par recherche textuelle (intitulé, tiers, commune)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower().Trim();
                filtered = filtered.Where(r =>
                    (r.BudgetLine?.Nommenclature?.Intitule?.ToLower().Contains(searchLower) ?? false) ||
                    (r.BudgetLine?.Nommenclature?.Chapitre?.ToLower().Contains(searchLower) ?? false) ||
                    (r.BudgetLine?.Nommenclature?.Article?.ToLower().Contains(searchLower) ?? false) ||
                    (r.BudgetLine?.Nommenclature?.CodeNomenclature?.ToLower().Contains(searchLower) ?? false) ||
                    (r.Tiers?.Nom?.ToLower().Contains(searchLower) ?? false) ||
                    (r.Commune?.Nom?.ToLower().Contains(searchLower) ?? false)
                );
            }

            // Filtre par montant minimum
            if (MontantMin.HasValue)
            {
                filtered = filtered.Where(r => (decimal)r.MontantRecense >= MontantMin.Value);
            }

            // Filtre par montant maximum
            if (MontantMax.HasValue)
            {
                filtered = filtered.Where(r => (decimal)r.MontantRecense <= MontantMax.Value);
            }

            // Mettre à jour la collection affichée
            Recensements.Clear();
            foreach (var r in filtered.OrderByDescending(r => r.Id))
            {
                Recensements.Add(r);
            }

            RefreshStatistics();
        }

        /// <summary>
        /// Réinitialise tous les filtres
        /// </summary>
        private void ClearFilters()
        {
            _selectedFilterExercice = null;
            _selectedFilterCommune = null;
            _selectedFilterTiers = null;
            _selectedFilterBudgetLine = null;
            _searchText = string.Empty;
            _montantMin = null;
            _montantMax = null;

            OnPropertyChanged(nameof(SelectedFilterExercice));
            OnPropertyChanged(nameof(SelectedFilterCommune));
            OnPropertyChanged(nameof(SelectedFilterTiers));
            OnPropertyChanged(nameof(SelectedFilterBudgetLine));
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(MontantMin));
            OnPropertyChanged(nameof(MontantMax));
            OnPropertyChanged(nameof(HasActiveFilters));

            // Réafficher toutes les données
            Recensements.Clear();
            foreach (var r in _allRecensements.OrderByDescending(r => r.Id))
            {
                Recensements.Add(r);
            }

            RefreshStatistics();
        }

        /// <summary>
        /// Rafraîchit les statistiques affichées
        /// </summary>
        private void RefreshStatistics()
        {
            OnPropertyChanged(nameof(TotalRecense));
            OnPropertyChanged(nameof(NombreRecensements));
            OnPropertyChanged(nameof(NombreTotalRecensements));
            OnPropertyChanged(nameof(HasActiveFilters));
        }

        private async System.Threading.Tasks.Task OpenAddDialogAsync()
        {
            if (!CanCreateRecensement)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Recensement.Create",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsEditMode = false;

            DialogRecensement = new Recensement
            {
                MontantRecense = 0
            };

            // ✅ Recharger toutes les lignes budgétaires
            IsLoading = true;
            try
            {
                var lignesBudgetaires = await _recensementService.GetAllBudgetLinesAsync();

                LignesBudgetaires.Clear();
                foreach (var lb in lignesBudgetaires)
                {
                    LignesBudgetaires.Add(lb);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Modal : {LignesBudgetaires.Count} lignes budgétaires chargées");

                if (LignesBudgetaires.Count == 0)
                {
                    MessageBox.Show(
                        "⚠️ Aucune ligne budgétaire n'a été trouvée.\n\n" +
                        "Veuillez d'abord créer des lignes budgétaires dans le module approprié.",
                        "Attention",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                OnPropertyChanged(nameof(LignesBudgetaires));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERREUR : {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des lignes budgétaires : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                IsLoading = false;
            }

            IsDialogOpen = true;
        }

        private void OpenEditDialog(Recensement? recensement)
        {
            if (recensement == null) return;
            if (!CanEditRecensement)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Recensement.Edit",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsEditMode = true;

            DialogRecensement = new Recensement
            {
                Id = recensement.Id,
                BudgetLineId = recensement.BudgetLineId,
                ExerciceId = recensement.ExerciceId,
                CommuneId = recensement.CommuneId,
                TiersId = recensement.TiersId,
                MontantRecense = recensement.MontantRecense
            };

            IsDialogOpen = true;
        }

        private bool CanSave()
        {
            return DialogRecensement != null &&
                   DialogRecensement.BudgetLineId > 0 &&
                   DialogRecensement.ExerciceId > 0 &&
                   DialogRecensement.CommuneId > 0 &&
                   DialogRecensement.TiersId > 0 &&
                   DialogRecensement.MontantRecense > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    if (!CanEditRecensement)
                    {
                        MessageBox.Show(
                            _accessDeniedMessage + "\nPermission requise : Recensement.Edit",
                            "Accès refusé",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    var (success, message) = await _recensementService.UpdateRecensementAsync(DialogRecensement);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        IsDialogOpen = false;
                        await LoadDataAsync();
                    }
                }
                else
                {
                    if (!CanCreateRecensement)
                    {
                        MessageBox.Show(
                            _accessDeniedMessage + "\nPermission requise : Recensement.Create",
                            "Accès refusé",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    var (success, message, recensement) = await _recensementService.CreateRecensementAsync(DialogRecensement);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        IsDialogOpen = false;
                        await LoadDataAsync();
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

        private async System.Threading.Tasks.Task DeleteAsync(Recensement? recensement)
        {
            if (recensement == null) return;

            var result = MessageBox.Show(
                $"⚠️ Supprimer ce recensement ?\n\n" +
                $"Commune : {recensement.Commune?.Nom}\n" +
                $"Tiers : {recensement.Tiers?.Nom}\n" +
                $"Montant : {recensement.MontantRecense:N0} GNF\n\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            if (!CanDeleteRecensement)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Recensement.Delete",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var (success, message) = await _recensementService.DeleteRecensementAsync(recensement.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDataAsync();
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

        #endregion
    }
}