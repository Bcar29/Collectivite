using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private BudgetStatistics _statistics;
        AuditService _auditService;

        public DashboardViewModel(AuditService auditService)
        {
            _auditService = auditService;
            _statistics = new BudgetStatistics();

            // Initialiser les données
            LoadDashboardData();

            // Commandes pour les actions rapides
            NewMandatCommand = new RelayCommand(_ => ExecuteQuickAction("Nouveau Mandat"));
            NewBonCommandeCommand = new RelayCommand(_ => ExecuteQuickAction("Nouveau Bon de Commande"));
            NewOrdreRecetteCommand = new RelayCommand(_ => ExecuteQuickAction("Nouvel Ordre de Recette"));
            NewEngagementCommand = new RelayCommand(_ => ExecuteQuickAction("Nouvel Engagement"));
            _auditService = auditService;
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

        #endregion

        #region Commands

        public ICommand NewMandatCommand { get; }
        public ICommand NewBonCommandeCommand { get; }
        public ICommand NewOrdreRecetteCommand { get; }
        public ICommand NewEngagementCommand { get; }

        #endregion

        #region Methods

        private void LoadDashboardData()
        {
            LoadIndicators();
            LoadBarChartData();
            LoadLineChartData();
            LoadRecentActivities();
            LoadQuickActions();
        }

        private void LoadIndicators()
        {
            // Données de démonstration
            Statistics = new BudgetStatistics
            {
                BudgetTotal = 15_000_000_000,
                DepensesEngagees = 8_450_000_000,
                RecettesPercues = 12_800_000_000,
                SoldeDisponible = 6_550_000_000
            };

            Indicators.Clear();

            Indicators.Add(new DashboardIndicator
            {
                Title = "Budget Total",
                Amount = Statistics.BudgetTotal,
                Icon = "CashMultiple",
                Color = "#1976D2",
                PercentageChange = 5.2
            });

            Indicators.Add(new DashboardIndicator
            {
                Title = "Dépenses Engagées",
                Amount = Statistics.DepensesEngagees,
                Icon = "TrendingDown",
                Color = "#F44336",
                PercentageChange = -2.8
            });

            Indicators.Add(new DashboardIndicator
            {
                Title = "Recettes Perçues",
                Amount = Statistics.RecettesPercues,
                Icon = "TrendingUp",
                Color = "#4CAF50",
                PercentageChange = 8.5
            });

            Indicators.Add(new DashboardIndicator
            {
                Title = "Solde Disponible",
                Amount = Statistics.SoldeDisponible,
                Icon = "WalletOutline",
                Color = "#FF9800",
                PercentageChange = 3.4
            });
        }

        private void LoadBarChartData()
        {
            BarChartData.Clear();

            // Recettes
            BarChartData.Add(new ChartDataPoint { Label = "Fonctionnement", Value = 7500, Category = "Recettes" });
            BarChartData.Add(new ChartDataPoint { Label = "Investissement", Value = 5300, Category = "Recettes" });

            // Dépenses
            BarChartData.Add(new ChartDataPoint { Label = "Fonctionnement", Value = 4800, Category = "Dépenses" });
            BarChartData.Add(new ChartDataPoint { Label = "Investissement", Value = 3650, Category = "Dépenses" });
        }

        private void LoadLineChartData()
        {
            LineChartData.Clear();

            var months = new[] { "Jan", "Fév", "Mar", "Avr", "Mai", "Jun", "Jul", "Aoû", "Sep", "Oct", "Nov", "Déc" };
            var recettes = new[] { 800, 950, 1100, 1050, 1200, 1150, 1300, 1250, 1400, 1350, 1450, 1500 };
            var depenses = new[] { 600, 700, 750, 800, 850, 800, 900, 850, 950, 900, 980, 1000 };

            for (int i = 0; i < months.Length; i++)
            {
                LineChartData.Add(new ChartDataPoint
                {
                    Label = months[i],
                    Value = recettes[i],
                    Category = "Recettes"
                });

                LineChartData.Add(new ChartDataPoint
                {
                    Label = months[i],
                    Value = depenses[i],
                    Category = "Dépenses"
                });
            }
        }

        private async void LoadRecentActivities()
        {
            try
            {
                RecentActivities.Clear();

                var logs = await _auditService.GetAllLogsAsync();

                foreach (var log in logs.Take(20)) // charger les 20 dernières
                {
                    RecentActivities.Add(log);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des activités : {ex.Message}");
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
                Description = "Créer un mandat de paiement"
            });

            QuickActions.Add(new QuickAction
            {
                Title = "Bon de Commande",
                Icon = "FileDocumentEdit",
                Color = "#2196F3",
                Description = "Créer un bon de commande"
            });

            QuickActions.Add(new QuickAction
            {
                Title = "Ordre de Recette",
                Icon = "Receipt",
                Color = "#FF9800",
                Description = "Créer un ordre de recette"
            });

            QuickActions.Add(new QuickAction
            {
                Title = "Engagement",
                Icon = "ClipboardText",
                Color = "#9C27B0",
                Description = "Créer une fiche d'engagement"
            });
        }

        private void ExecuteQuickAction(string actionName)
        {
            // TODO: Implémenter la navigation vers les pages correspondantes
            System.Windows.MessageBox.Show(
                $"Action '{actionName}' sera disponible dans les prochaines étapes.",
                "Action Rapide",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        #endregion
    }
}
