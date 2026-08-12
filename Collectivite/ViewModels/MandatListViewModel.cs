using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Collectivite.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class MandatListViewModel : ViewModelBase, IDisposable
    {
        private bool _isLoading;
        private Mandat? _selectedMandat;
        private readonly ExerciceService _exerciceService;
        private readonly string _accessDeniedMessage = "Accès refusé : vous n'avez pas la permission d'effectuer cette opération.";

        // Filtres
        private string? _filtreNumeroMandat;
        private string? _filtreBordereau;
        private TypeMois? _filtreMois;
        private int? _filtreEngagementId;
        private decimal? _filtreMontantMin;
        private decimal? _filtreMontantMax;
        private DateTime? _filtreDateEmissionDebut;
        private DateTime? _filtreDateEmissionFin;
        private bool? _filtreEstPaye;
        private bool _isDisposed;
        private bool _isFiltered;

        // Pagination
        private const int PageSize = 20;
        private int _pageNumber = 1;
        public int PageNumber
        {
            get => _pageNumber;
            set => SetProperty(ref _pageNumber, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    OnPropertyChanged(nameof(TotalPages));
                }
            }
        }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        // ══════════════════════════════════════════════════════
        // AJOUT : Dictionnaire pour stocker les montants payés
        // ══════════════════════════════════════════════════════
        private Dictionary<int, decimal> _montantsPayes = new();

        /// <summary>
        /// Obtient le montant payé pour un mandat spécifique
        /// </summary>
        public decimal GetMontantPaye(int mandatId)
        {
            return _montantsPayes.TryGetValue(mandatId, out var montant) ? montant : 0m;
        }

        /// <summary>
        /// Dictionnaire accessible pour le binding (utilisé par le converter)
        /// </summary>
        public Dictionary<int, decimal> MontantsPayes => _montantsPayes;

        public MandatListViewModel()
        {
            _exerciceService = ExerciceService.Instance;
            _exerciceService.ExerciceChanged += OnExerciceChanged;
            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataResetAsync());
            ApplyFiltersCommand = new RelayCommand(async _ => await ApplyFiltersResetAsync());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            OpenAddPageCommand = new RelayCommand(_ => OpenAddPage());
            OpenEditPageCommand = new RelayCommand<Mandat>(m => OpenEditPage(m));
            OpenDetailPageCommand = new RelayCommand<Mandat>(m => OpenDetailPage(m));
            DeleteCommand = new RelayCommand<Mandat>(async m => await DeleteAsync(m));
            MarquerPayeCommand = new RelayCommand<Mandat>(async m => await MarquerCommePaye(m));
            AnnulerPaiementCommand = new RelayCommand<Mandat>(async m => await AnnulerPaiement(m));
            NextPageCommand = new RelayCommand(async _ => await NextPageAsync(), _ => PageNumber < TotalPages);
            PreviousPageCommand = new RelayCommand(async _ => await PreviousPageAsync(), _ => PageNumber > 1);

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Mandat> Mandats { get; } = new();
        public ObservableCollection<Engagement> Engagements { get; } = new();

        // Permissions
        public bool CanViewMandat => SessionManager.HasPermission("Mandat.View");
        public bool CanCreateMandat => SessionManager.HasPermission("Mandat.Create");
        public bool CanEditMandat => SessionManager.HasPermission("Mandat.Edit");
        public bool CanDeleteMandat => SessionManager.HasPermission("Mandat.Delete");

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Mandat? SelectedMandat
        {
            get => _selectedMandat;
            set => SetProperty(ref _selectedMandat, value);
        }

        public decimal TotalMandats => Mandats.Sum(m => m.MontantNet);

        // ══════════════════════════════════════════════════════
        // MODIFIÉ : Statistiques basées sur le statut calculé
        // ══════════════════════════════════════════════════════
        public decimal TotalPayes => Mandats.Where(m => m.Status == Mandat.StatutMandat.Payé).Sum(m => m.MontantNet);
        public decimal TotalPartiels => Mandats.Where(m => m.Status == Mandat.StatutMandat.Partiel).Sum(m => m.MontantNet);
        public decimal TotalNonPayes => Mandats.Where(m => m.Status == Mandat.StatutMandat.Non_Payé).Sum(m => m.MontantNet);

        public int NombreMandats => Mandats.Count;
        public int NombrePayes => Mandats.Count(m => m.Status == Mandat.StatutMandat.Payé);
        public int NombrePartiels => Mandats.Count(m => m.Status == Mandat.StatutMandat.Partiel);
        public int NombreNonPayes => Mandats.Count(m => m.Status == Mandat.StatutMandat.Non_Payé);

        // ══════════════════════════════════════════════════════
        // AJOUT : Total des montants payés
        // ══════════════════════════════════════════════════════
        public decimal TotalMontantsPayes => _montantsPayes.Values.Sum();

        #endregion

        #region Filtres

        public string? FiltreNumeroMandat
        {
            get => _filtreNumeroMandat;
            set => SetProperty(ref _filtreNumeroMandat, value);
        }

        public string? FiltreBordereau
        {
            get => _filtreBordereau;
            set => SetProperty(ref _filtreBordereau, value);
        }

        public TypeMois? FiltreMois
        {
            get => _filtreMois;
            set => SetProperty(ref _filtreMois, value);
        }

        public int? FiltreEngagementId
        {
            get => _filtreEngagementId;
            set => SetProperty(ref _filtreEngagementId, value);
        }

        public decimal? FiltreMontantMin
        {
            get => _filtreMontantMin;
            set => SetProperty(ref _filtreMontantMin, value);
        }

        public decimal? FiltreMontantMax
        {
            get => _filtreMontantMax;
            set => SetProperty(ref _filtreMontantMax, value);
        }

        public DateTime? FiltreDateEmissionDebut
        {
            get => _filtreDateEmissionDebut;
            set => SetProperty(ref _filtreDateEmissionDebut, value);
        }

        public DateTime? FiltreDateEmissionFin
        {
            get => _filtreDateEmissionFin;
            set => SetProperty(ref _filtreDateEmissionFin, value);
        }

        public bool? FiltreEstPaye
        {
            get => _filtreEstPaye;
            set => SetProperty(ref _filtreEstPaye, value);
        }

        // Liste des mois pour le ComboBox
        public Array MoisList => Enum.GetValues(typeof(TypeMois));

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand OpenAddPageCommand { get; }
        public ICommand OpenEditPageCommand { get; }
        public ICommand OpenDetailPageCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand MarquerPayeCommand { get; }
        public ICommand AnnulerPaiementCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        #endregion

        #region Methods
        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadDataResetAsync();
            });
        }

        private async System.Threading.Tasks.Task LoadDataResetAsync()
        {
            _isFiltered = false;
            PageNumber = 1;
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            if (!CanViewMandat)
            {
                NotificationService.ShowWarning(_accessDeniedMessage);
                return;
            }

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var (mandats, totalCount) = await mandatService.GetAllMandatsAsync(PageNumber, PageSize);
                TotalCount = totalCount;

                // ══════════════════════════════════════════════════════
                // MODIFIÉ : Calculer le statut et le montant payé
                // ══════════════════════════════════════════════════════
                var paiementService = new MandatPaiementService();
                _montantsPayes.Clear(); // Réinitialiser le dictionnaire

                foreach (var m in mandats)
                {
                    var (montantPaye, statut) = await paiementService.GetInfoPaiementAsync(m.Id, m.MontantNet);
                    m.Status = statut;
                    _montantsPayes[m.Id] = montantPaye; // Stocker le montant payé
                }

                Mandats.Clear();
                foreach (var m in mandats)
                {
                    Mandats.Add(m);
                }

                // Charger les engagements pour le filtre
                var engagementService = new EngagementService();
                var engagements = await engagementService.GetAllEngagementsAsync();

                Engagements.Clear();
                foreach (var e in engagements)
                {
                    Engagements.Add(e);
                }

                UpdateStatistics();
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

        private async System.Threading.Tasks.Task ApplyFiltersResetAsync()
        {
            _isFiltered = true;
            PageNumber = 1;
            await ApplyFiltersAsync();
        }

        private async System.Threading.Tasks.Task ApplyFiltersAsync()
        {
            if (!CanViewMandat)
            {
                NotificationService.ShowWarning(_accessDeniedMessage);
                return;
            }

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var (mandats, totalCount) = await mandatService.GetMandatsFilteredAsync(
                    numeroMandat: FiltreNumeroMandat,
                    bordereau: FiltreBordereau,
                    mois: FiltreMois,
                    engagementId: FiltreEngagementId,
                    montantMin: FiltreMontantMin,
                    montantMax: FiltreMontantMax,
                    dateEmissionDebut: FiltreDateEmissionDebut,
                    dateEmissionFin: FiltreDateEmissionFin,
                    estPaye: FiltreEstPaye,
                    pageNumber: PageNumber,
                    pageSize: PageSize
                );
                TotalCount = totalCount;

                // ══════════════════════════════════════════════════════
                // MODIFIÉ : Calculer le statut et le montant payé
                // ══════════════════════════════════════════════════════
                var paiementService = new MandatPaiementService();
                _montantsPayes.Clear(); // Réinitialiser le dictionnaire

                foreach (var m in mandats)
                {
                    var (montantPaye, statut) = await paiementService.GetInfoPaiementAsync(m.Id, m.MontantNet);
                    m.Status = statut;
                    _montantsPayes[m.Id] = montantPaye; // Stocker le montant payé
                }

                Mandats.Clear();
                foreach (var m in mandats)
                {
                    Mandats.Add(m);
                }

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors de l'application des filtres : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task NextPageAsync()
        {
            if (PageNumber >= TotalPages) return;
            PageNumber++;
            await (_isFiltered ? ApplyFiltersAsync() : LoadDataAsync());
        }

        private async System.Threading.Tasks.Task PreviousPageAsync()
        {
            if (PageNumber <= 1) return;
            PageNumber--;
            await (_isFiltered ? ApplyFiltersAsync() : LoadDataAsync());
        }

        private void ClearFilters()
        {
            FiltreNumeroMandat = null;
            FiltreBordereau = null;
            FiltreMois = null;
            FiltreEngagementId = null;
            FiltreMontantMin = null;
            FiltreMontantMax = null;
            FiltreDateEmissionDebut = null;
            FiltreDateEmissionFin = null;
            FiltreEstPaye = null;

            LoadDataCommand.Execute(null);
        }

        private void OpenAddPage()
        {
            if (!CanCreateMandat)
            {
                NotificationService.ShowWarning(_accessDeniedMessage);
                return;
            }

            var formPage = new MandatFormPage();
            NavigationService.Instance.NavigateTo(formPage);
        }

        private void OpenEditPage(Mandat? mandat)
        {
            if (mandat == null) return;
            if (!CanEditMandat)
            {
                NotificationService.ShowWarning(_accessDeniedMessage);
                return;
            }

            var editPage = new Views.Pages.MandatFormPage(mandat.Id);
            NavigationService.Instance.NavigateTo(editPage);
        }

        private void OpenDetailPage(Mandat? mandat)
        {
            if (mandat == null) return;
            if (!CanViewMandat)
            {
                NotificationService.ShowWarning(_accessDeniedMessage);
                return;
            }

            var detailPage = new Views.Pages.MandatDetailPage(mandat.Id);
            NavigationService.Instance.NavigateTo(detailPage);
        }

        private async System.Threading.Tasks.Task DeleteAsync(Mandat? mandat)
        {
            if (mandat == null) return;

            if (!CanDeleteMandat)
            {
                NotificationService.ShowWarning(_accessDeniedMessage);
                return;
            }

            var result = MessageBox.Show(
                $"⚠️ Supprimer le mandat '{mandat.NumeroMandat}' ?\n\n" +
                $"Montant : {mandat.MontantNet:N0} GNF\n" +
                $"Objet : {mandat.Objet}\n\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var (success, message) = await mandatService.DeleteMandatAsync(mandat.Id);

                if (success)
                    NotificationService.ShowSuccess(message);
                else
                    NotificationService.ShowWarning(message);

                if (success)
                {
                    _isFiltered = false;
                    await LoadDataAsync();
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

        private async System.Threading.Tasks.Task MarquerCommePaye(Mandat? mandat)
        {
            if (mandat == null) return;

            if (!CanEditMandat)
            {
                NotificationService.ShowWarning(_accessDeniedMessage);
                return;
            }

            if (mandat.DatePaiement != null)
            {
                NotificationService.ShowInfo("Ce mandat a déjà été payé.");
                return;
            }

            var result = MessageBox.Show(
                $"Marquer le mandat '{mandat.NumeroMandat}' comme payé ?\n\n" +
                $"Montant : {mandat.MontantNet:N0} GNF",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var (success, message) = await mandatService.MarquerCommePaye(mandat.Id, DateTime.Now);

                if (success)
                    NotificationService.ShowSuccess(message);
                else
                    NotificationService.ShowWarning(message);

                if (success)
                {
                    _isFiltered = false;
                    await LoadDataAsync();
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

        private async System.Threading.Tasks.Task AnnulerPaiement(Mandat? mandat)
        {
            if (mandat == null) return;

            if (!CanEditMandat)
            {
                NotificationService.ShowWarning(_accessDeniedMessage);
                return;
            }

            if (mandat.DatePaiement == null)
            {
                NotificationService.ShowInfo("Ce mandat n'a pas encore été payé.");
                return;
            }

            var result = MessageBox.Show(
                $"Annuler le paiement du mandat '{mandat.NumeroMandat}' ?\n\n" +
                $"Montant : {mandat.MontantNet:N0} GNF\n" +
                $"Date de paiement : {mandat.DatePaiement:dd/MM/yyyy}",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();
                var (success, message) = await mandatService.AnnulerPaiement(mandat.Id);

                if (success)
                    NotificationService.ShowSuccess(message);
                else
                    NotificationService.ShowWarning(message);

                if (success)
                {
                    _isFiltered = false;
                    await LoadDataAsync();
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

        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalMandats));
            OnPropertyChanged(nameof(TotalPayes));
            OnPropertyChanged(nameof(TotalPartiels));
            OnPropertyChanged(nameof(TotalNonPayes));
            OnPropertyChanged(nameof(NombreMandats));
            OnPropertyChanged(nameof(NombrePayes));
            OnPropertyChanged(nameof(NombrePartiels));
            OnPropertyChanged(nameof(NombreNonPayes));
            OnPropertyChanged(nameof(TotalMontantsPayes));
            OnPropertyChanged(nameof(MontantsPayes));
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