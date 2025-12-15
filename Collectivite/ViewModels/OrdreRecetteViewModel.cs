using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Collectivite.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class OrdreRecetteViewModel : ViewModelBase
    {
        private bool _isLoading;
        private OrdreRecetteDisplay? _selectedOrdreRecette;
        private bool _isDialogOpen;
        private OrdreRecette _dialogOrdreRecette;
        private readonly CommuneService _communeService;
        private bool _isEditMode;
        private bool _isFilterPanelOpen;
        private readonly string _accessDeniedMessage = "Accès refusé : vous n'avez pas la permission d'effectuer cette opération.";

        // Filtres
        private string _searchNumeroOrdre;
        private int? _filterExerciceId;
        private int? _filterCommuneId;
        private int? _filterTiersId;
        private DateTime? _filterDateDebut;
        private DateTime? _filterDateFin;
        private decimal? _filterMontantMin;
        private decimal? _filterMontantMax;

        public OrdreRecetteViewModel()
        {
            _dialogOrdreRecette = new OrdreRecette
            {
                DateOrdre = DateTime.Now,
                NumeroOrdre = "",
                MontantOrdre = 0,
                MontantOrdreLettre = "",
                Comptable = ""
            };
            _searchNumeroOrdre = string.Empty;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<OrdreRecetteDisplay>(async o => await DeleteAsync(o));
            NavigateToDetailCommand = new RelayCommand<OrdreRecetteDisplay>(o => NavigateToDetail(o));
            NavigateToAddCommand = new RelayCommand(_ => NavigateToAdd());
            NavigateToEditCommand = new RelayCommand<OrdreRecetteDisplay>(o => NavigateToEdit(o));

            // Commandes de recherche et filtrage
            SearchCommand = new RelayCommand(async _ => await SearchAsync());
            ClearFiltersCommand = new RelayCommand(async _ => await ClearFiltersAsync());
            ToggleFilterPanelCommand = new RelayCommand(_ => ToggleFilterPanel());

            // Commande pour générer le montant en lettres
            ConvertMontantToLettresCommand = new RelayCommand(_ => ConvertMontantToLettres());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        // ══════════════════════════════════════════════════════
        // MODIFIÉ : Utiliser OrdreRecetteDisplay au lieu de OrdreRecette
        // ══════════════════════════════════════════════════════
        public ObservableCollection<OrdreRecetteDisplay> OrdresRecette { get; } = new();
        public ObservableCollection<BudgetLine> BudgetLines { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Commune> Communes { get; } = new();
        public ObservableCollection<Tiers> TiersList { get; } = new();

        // Permissions
        public bool CanViewOrdreRecette => SessionManager.HasPermission("OrdreRecette.View");
        public bool CanCreateOrdreRecette => SessionManager.HasPermission("OrdreRecette.Create");
        public bool CanEditOrdreRecette => SessionManager.HasPermission("OrdreRecette.Edit");
        public bool CanDeleteOrdreRecette => SessionManager.HasPermission("OrdreRecette.Delete");

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public OrdreRecetteDisplay? SelectedOrdreRecette
        {
            get => _selectedOrdreRecette;
            set => SetProperty(ref _selectedOrdreRecette, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public OrdreRecette DialogOrdreRecette
        {
            get => _dialogOrdreRecette;
            set => SetProperty(ref _dialogOrdreRecette, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public bool IsFilterPanelOpen
        {
            get => _isFilterPanelOpen;
            set => SetProperty(ref _isFilterPanelOpen, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier l'ordre de recette" : "Nouvel ordre de recette";

        public decimal TotalOrdres => OrdresRecette.Sum(o => o.MontantOrdre);
        public int CountOrdres => OrdresRecette.Count;

        // ══════════════════════════════════════════════════════
        // AJOUT : Statistiques par statut
        // ══════════════════════════════════════════════════════
        public decimal TotalEncaisse => OrdresRecette.Sum(o => o.MontantEncaisse);
        public decimal TotalPayes => OrdresRecette.Where(o => o.Statut == OrdreRecette.StatutOrdre.Enciassé).Sum(o => o.MontantOrdre);
        public decimal TotalPartiels => OrdresRecette.Where(o => o.Statut == OrdreRecette.StatutOrdre.Partiel).Sum(o => o.MontantOrdre);
        public decimal TotalNonPayes => OrdresRecette.Where(o => o.Statut == OrdreRecette.StatutOrdre.Non_Encaissé).Sum(o => o.MontantOrdre);
        public int NombrePayes => OrdresRecette.Count(o => o.Statut == OrdreRecette.StatutOrdre.Enciassé);
        public int NombrePartiels => OrdresRecette.Count(o => o.Statut == OrdreRecette.StatutOrdre.Partiel);
        public int NombreNonPayes => OrdresRecette.Count(o => o.Statut == OrdreRecette.StatutOrdre.Non_Encaissé);

        #endregion

        #region Properties de filtrage

        public string SearchNumeroOrdre
        {
            get => _searchNumeroOrdre;
            set => SetProperty(ref _searchNumeroOrdre, value);
        }

        public int? FilterExerciceId
        {
            get => _filterExerciceId;
            set => SetProperty(ref _filterExerciceId, value);
        }

        public int? FilterCommuneId
        {
            get => _filterCommuneId;
            set => SetProperty(ref _filterCommuneId, value);
        }

        public int? FilterTiersId
        {
            get => _filterTiersId;
            set => SetProperty(ref _filterTiersId, value);
        }

        public DateTime? FilterDateDebut
        {
            get => _filterDateDebut;
            set => SetProperty(ref _filterDateDebut, value);
        }

        public DateTime? FilterDateFin
        {
            get => _filterDateFin;
            set => SetProperty(ref _filterDateFin, value);
        }

        public decimal? FilterMontantMin
        {
            get => _filterMontantMin;
            set => SetProperty(ref _filterMontantMin, value);
        }

        public decimal? FilterMontantMax
        {
            get => _filterMontantMax;
            set => SetProperty(ref _filterMontantMax, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ToggleFilterPanelCommand { get; }
        public ICommand ConvertMontantToLettresCommand { get; }
        public ICommand NavigateToAddCommand { get; }
        public ICommand NavigateToEditCommand { get; }
        public ICommand NavigateToDetailCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            if (!CanViewOrdreRecette)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var ordreRecetteService = new OrdreRecetteService();
                var ordres = await ordreRecetteService.GetAllOrdresRecetteAsync();

                // ══════════════════════════════════════════════════════
                // AJOUT : Calculer le montant encaissé et le statut
                // ══════════════════════════════════════════════════════
                var paiementService = new OrdreRecettePaiementService();

                OrdresRecette.Clear();
                foreach (var o in ordres)
                {
                    var (montantEncaisse, statut) = await paiementService.GetInfoEncaissementAsync(o.Id, o.MontantOrdre);
                    OrdresRecette.Add(new OrdreRecetteDisplay(o, montantEncaisse, statut));
                }

                // Charger les lignes budgétaires
                var budgetLines = await ordreRecetteService.GetBudgetLinesSansEnfantsAsync();

                BudgetLines.Clear();
                foreach (var bl in budgetLines)
                {
                    BudgetLines.Add(bl);
                }

                // Charger les exercices
                using (var context = new AppDbContext())
                {
                    var exerciceService = new ExerciceService();
                    var exercices = await exerciceService.GetAllExerciceAsync();

                    Exercices.Clear();
                    foreach (var ex in exercices)
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

                UpdateStatistics();
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

        // ══════════════════════════════════════════════════════
        // AJOUT : Méthode pour mettre à jour les statistiques
        // ══════════════════════════════════════════════════════
        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalOrdres));
            OnPropertyChanged(nameof(CountOrdres));
            OnPropertyChanged(nameof(TotalEncaisse));
            OnPropertyChanged(nameof(TotalPayes));
            OnPropertyChanged(nameof(TotalPartiels));
            OnPropertyChanged(nameof(TotalNonPayes));
            OnPropertyChanged(nameof(NombrePayes));
            OnPropertyChanged(nameof(NombrePartiels));
            OnPropertyChanged(nameof(NombreNonPayes));
        }

        private void OpenAddDialogAsync()
        {
            IsEditMode = false;

            string numeroOrdre = $"OR-{DateTime.Now:yyyyMMdd}-{OrdresRecette.Count + 1:D4}";

            DialogOrdreRecette = new OrdreRecette
            {
                DateOrdre = DateTime.Now,
                NumeroOrdre = numeroOrdre,
                MontantOrdre = 0,
                MontantOrdreLettre = "",
                Comptable = ""
            };

            IsDialogOpen = true;
        }

        private void OpenEditDialog(OrdreRecette? ordreRecette)
        {
            if (ordreRecette == null) return;

            IsEditMode = true;

            DialogOrdreRecette = new OrdreRecette
            {
                Id = ordreRecette.Id,
                NumeroOrdre = ordreRecette.NumeroOrdre,
                BudgetLineId = ordreRecette.BudgetLineId,
                ExerciceId = ordreRecette.ExerciceId,
                CommuneId = ordreRecette.CommuneId,
                Comptable = ordreRecette.Comptable,
                TiersId = ordreRecette.TiersId,
                Motifs = ordreRecette.Motifs,
                MontantOrdre = ordreRecette.MontantOrdre,
                MontantOrdreLettre = ordreRecette.MontantOrdreLettre,
                DateOrdre = ordreRecette.DateOrdre
            };

            IsDialogOpen = true;
        }

        private bool CanSave()
        {
            return DialogOrdreRecette != null &&
                   !string.IsNullOrWhiteSpace(DialogOrdreRecette.NumeroOrdre) &&
                   DialogOrdreRecette.BudgetLineId > 0 &&
                   DialogOrdreRecette.ExerciceId > 0 &&
                   DialogOrdreRecette.CommuneId > 0 &&
                   !string.IsNullOrWhiteSpace(DialogOrdreRecette.Comptable) &&
                   DialogOrdreRecette.MontantOrdre > 0 &&
                   !string.IsNullOrWhiteSpace(DialogOrdreRecette.MontantOrdreLettre);
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            if (IsEditMode)
            {
                if (!CanEditOrdreRecette)
                {
                    MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                if (!CanCreateOrdreRecette)
                {
                    MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            IsLoading = true;

            try
            {
                var ordreRecetteService = new OrdreRecetteService();

                if (IsEditMode)
                {
                    var (success, message) = await ordreRecetteService.UpdateOrdreRecetteAsync(DialogOrdreRecette);

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
                    var (success, message, ordreRecette) = await ordreRecetteService.CreateOrdreRecetteAsync(DialogOrdreRecette);

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

        // ══════════════════════════════════════════════════════
        // MODIFIÉ : Utiliser OrdreRecetteDisplay
        // ══════════════════════════════════════════════════════
        private async System.Threading.Tasks.Task DeleteAsync(OrdreRecetteDisplay? ordreDisplay)
        {
            if (ordreDisplay == null) return;

            if (!CanDeleteOrdreRecette)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"⚠️ Supprimer l'ordre de recette '{ordreDisplay.NumeroOrdre}' ?\n\n" +
                $"Montant : {ordreDisplay.MontantOrdre:N0} GNF\n" +
                $"Date : {ordreDisplay.DateOrdre:dd/MM/yyyy}\n\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var ordreRecetteService = new OrdreRecetteService();
                var (success, message) = await ordreRecetteService.DeleteOrdreRecetteAsync(ordreDisplay.Id);

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

        private void NavigateToAdd()
        {
            if (!CanCreateOrdreRecette)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.OrdreRecetteFormPage());
        }

        // ══════════════════════════════════════════════════════
        // MODIFIÉ : Utiliser OrdreRecetteDisplay
        // ══════════════════════════════════════════════════════
        private void NavigateToEdit(OrdreRecetteDisplay? ordreDisplay)
        {
            if (ordreDisplay == null) return;
            if (!CanEditOrdreRecette)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.OrdreRecetteFormPage(ordreDisplay.Id));
        }

        // ══════════════════════════════════════════════════════
        // MODIFIÉ : Utiliser OrdreRecetteDisplay
        // ══════════════════════════════════════════════════════
        private void NavigateToDetail(OrdreRecetteDisplay? ordreDisplay)
        {
            if (ordreDisplay == null) return;
            if (!CanViewOrdreRecette)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.OrdreRecetteDetailPage(ordreDisplay.Id));
        }
        #endregion

        #region Recherche et Filtrage

        private async System.Threading.Tasks.Task SearchAsync()
        {
            if (!CanViewOrdreRecette)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var ordreRecetteService = new OrdreRecetteService();

                var resultats = await ordreRecetteService.SearchOrdresRecetteAsync(
                    numeroOrdre: SearchNumeroOrdre,
                    exerciceId: FilterExerciceId,
                    communeId: FilterCommuneId,
                    tiersId: FilterTiersId,
                    dateDebut: FilterDateDebut,
                    dateFin: FilterDateFin,
                    montantMin: FilterMontantMin,
                    montantMax: FilterMontantMax
                );

                // ══════════════════════════════════════════════════════
                // AJOUT : Calculer le montant encaissé et le statut
                // ══════════════════════════════════════════════════════
                var paiementService = new OrdreRecettePaiementService();

                OrdresRecette.Clear();
                foreach (var o in resultats)
                {
                    var (montantEncaisse, statut) = await paiementService.GetInfoEncaissementAsync(o.Id, o.MontantOrdre);
                    OrdresRecette.Add(new OrdreRecetteDisplay(o, montantEncaisse, statut));
                }

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task ClearFiltersAsync()
        {
            SearchNumeroOrdre = string.Empty;
            FilterExerciceId = null;
            FilterCommuneId = null;
            FilterTiersId = null;
            FilterDateDebut = null;
            FilterDateFin = null;
            FilterMontantMin = null;
            FilterMontantMax = null;

            await LoadDataAsync();
        }

        private void ToggleFilterPanel()
        {
            IsFilterPanelOpen = !IsFilterPanelOpen;
        }

        #endregion

        #region Conversion en lettres

        private void ConvertMontantToLettres()
        {
            if (DialogOrdreRecette.MontantOrdre > 0)
            {
                DialogOrdreRecette.MontantOrdreLettre = ConvertirNombreEnLettres((long)DialogOrdreRecette.MontantOrdre);
                OnPropertyChanged(nameof(DialogOrdreRecette));
            }
        }

        private string ConvertirNombreEnLettres(long nombre)
        {
            if (nombre == 0) return "zéro";

            string[] unites = { "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf" };
            string[] dizaines = { "", "dix", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante-dix", "quatre-vingt", "quatre-vingt-dix" };
            string[] speciales = { "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf" };

            string resultat = "";

            // Milliards
            if (nombre >= 1000000000)
            {
                long milliards = nombre / 1000000000;
                resultat += ConvertirNombreEnLettres(milliards) + " milliard" + (milliards > 1 ? "s" : "") + " ";
                nombre %= 1000000000;
            }

            // Millions
            if (nombre >= 1000000)
            {
                long millions = nombre / 1000000;
                resultat += ConvertirNombreEnLettres(millions) + " million" + (millions > 1 ? "s" : "") + " ";
                nombre %= 1000000;
            }

            // Milliers
            if (nombre >= 1000)
            {
                long milliers = nombre / 1000;
                if (milliers == 1)
                    resultat += "mille ";
                else
                    resultat += ConvertirNombreEnLettres(milliers) + " mille ";
                nombre %= 1000;
            }

            // Centaines
            if (nombre >= 100)
            {
                long centaines = nombre / 100;
                if (centaines == 1)
                    resultat += "cent ";
                else
                    resultat += unites[centaines] + " cent" + (nombre % 100 == 0 ? "s" : "") + " ";
                nombre %= 100;
            }

            // Dizaines et unités
            if (nombre >= 20)
            {
                long diz = nombre / 10;
                long unit = nombre % 10;

                if (diz == 7 || diz == 9)
                {
                    resultat += dizaines[diz - 1] + "-";
                    if (unit == 1 && diz == 7)
                        resultat += "et-onze";
                    else if (unit == 0)
                        resultat += "dix";
                    else
                        resultat += speciales[unit];
                }
                else
                {
                    resultat += dizaines[diz];
                    if (unit == 1 && diz != 8)
                        resultat += "-et-un";
                    else if (unit > 0)
                        resultat += (diz == 8 && unit == 0 ? "s" : "-") + unites[unit];
                    else if (diz == 8)
                        resultat += "s";
                }
            }
            else if (nombre >= 10)
            {
                resultat += speciales[nombre - 10];
            }
            else if (nombre > 0)
            {
                resultat += unites[nombre];
            }

            return resultat.Trim() + " francs guinéens";
        }

        #endregion
    }
}