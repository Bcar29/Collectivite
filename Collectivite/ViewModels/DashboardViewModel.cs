using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class DashboardViewModel : ViewModelBase, IDisposable
    {
        private BudgetStatistics _statistics;
        private AuditService _auditService;
        private DashboardService _dashboardService;
        private readonly ExerciceService _exerciceService;
        private bool _isDisposed;
        private readonly AuthService _authService;
        //private bool _isLoading = true;

        public DashboardViewModel(AuditService auditService, AuthService authService)
        {
            _auditService = auditService;
            _dashboardService = new DashboardService();
            _statistics = new BudgetStatistics();
            _exerciceService = ExerciceService.Instance;
            _authService = authService;

            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;


            // Commandes pour les actions rapides
            NewMandatCommand = new RelayCommand(_ => NavigationService.Instance.NavigateTo(new Views.Pages.MandatListPage()));
            NewBonCommandeCommand = new RelayCommand(_ => NavigationService.Instance.NavigateTo(new Views.Pages.BonCommandeListPage(_authService)));
            NewOrdreRecetteCommand = new RelayCommand(_ => NavigationService.Instance.NavigateTo(new Views.Pages.OrdreRecettePage()));
            NewEngagementCommand = new RelayCommand(_ => NavigationService.Instance.NavigateTo(new Views.Pages.EngagementPage()));

            // Initialiser les données
            LoadDashboardData();
        }

        #region Properties

        public ObservableCollection<DashboardIndicator> Indicators { get; } = new();
        public ObservableCollection<ChartDataPoint> BarChartData { get; } = new();
        public ObservableCollection<ChartDataPoint> LineChartData { get; } = new();
        public ObservableCollection<AuditLog> RecentActivities { get; } = new();
        public ObservableCollection<QuickAction> QuickActions { get; } = new();

        public BudgetStatistics Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }
        //public bool IsLoading
        //{
        //    get => _isLoading;
        //    set => SetProperty(ref _isLoading, value);
        //}


        #endregion

        #region Commands

        public ICommand NewMandatCommand { get; }
        public ICommand NewBonCommandeCommand { get; }
        public ICommand NewOrdreRecetteCommand { get; }
        public ICommand NewEngagementCommand { get; }

        #endregion

        #region Methods

        private async void LoadDashboardData()
        {

            await LoadIndicators();
            await LoadBarChartData();
            await LoadLineChartData();
            LoadRecentActivities();
            LoadQuickActions();
            //IsLoading = false;
        }

        private async Task LoadIndicators()
        {
            try
            {
                // Charger les indicateurs depuis le service
                var indicators = await _dashboardService.GetIndicatorsAsync();

                Indicators.Clear();
                foreach (var indicator in indicators)
                {
                    Indicators.Add(indicator);
                }

                // Mettre à jour les statistiques
                Statistics = await _dashboardService.GetBudgetStatisticsAsync();
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement des indicateurs : {ex.Message}");
            }
        }

        private async Task LoadBarChartData()
        {
            try
            {
                BarChartData.Clear();

                var data = await _dashboardService.GetBarChartDataAsync();

                foreach (var point in data)
                {
                    BarChartData.Add(point);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement du graphique en barres : {ex.Message}");
            }
        }

        private async Task LoadLineChartData()
        {
            try
            {
                LineChartData.Clear();

                var data = await _dashboardService.GetLineChartDataAsync();

                foreach (var point in data)
                {
                    LineChartData.Add(point);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement du graphique en lignes : {ex.Message}");
            }
        }

        private async void LoadRecentActivities()
        {
            try
            {
                RecentActivities.Clear();

                var logs = await _auditService.GetAllLogsAsync();

                foreach (var log in logs.Take(10)) // charger les 10 dernières
                {
                    RecentActivities.Add(log);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors du chargement des activités : {ex.Message}");
            }
        }

        private void LoadQuickActions()
        {
            QuickActions.Clear();

            QuickActions.Add(new QuickAction
            {
                Title = "Nouveau Mandat",
                Icon = "CashCheck",
                Color = "#4CAF50",
                Description = "Créer un mandat de paiement",
                Command = NewMandatCommand
            });

            QuickActions.Add(new QuickAction
            {
                Title = "Bon de Commande",
                Icon = "FileDocumentEdit",
                Color = "#2196F3",
                Description = "Créer un bon de commande",
                Command = NewBonCommandeCommand
            });

            QuickActions.Add(new QuickAction
            {
                Title = "Ordre de Recette",
                Icon = "Receipt",
                Color = "#FF9800",
                Description = "Créer un ordre de recette",
                Command = NewOrdreRecetteCommand
            });

            QuickActions.Add(new QuickAction
            {
                Title = "Engagement",
                Icon = "ClipboardText",
                Color = "#9C27B0",
                Description = "Créer une fiche d'engagement",
                Command = NewEngagementCommand
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GESTION DU CHANGEMENT D'EXERCICE
        // ═══════════════════════════════════════════════════════════

        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Rechargement des lignes budgétaires pour l'exercice : {exercice.Libelle}");

                LoadDashboardData();

            });
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