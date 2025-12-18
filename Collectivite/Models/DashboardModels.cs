using System.Windows.Input;

namespace Collectivite.Models
{
    /// <summary>
    /// Modèle pour les indicateurs principaux du dashboard
    /// </summary>
    public class DashboardIndicator
    {
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string FormattedAmount => $"{Amount:N0} GNF";
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public double PercentageChange { get; set; }
        public bool IsPositiveChange => PercentageChange >= 0;
        public string ChangeText => $"{(IsPositiveChange ? "+" : "")}{PercentageChange:F1}%";
    }

    /// <summary>
    /// Modèle pour les données des graphiques
    /// </summary>
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Modèle pour les activités récentes
    /// </summary>
    public class RecentActivity
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string IconColor { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string FormattedDate => Date.ToString("dd/MM/yyyy HH:mm");
       
        public decimal? Amount { get; set; }
        public string FormattedAmount => Amount.HasValue ? $"{Amount.Value:N0} GNF" : "";
    }

    /// <summary>
    /// Modèle pour les actions rapides
    /// </summary>
    public class QuickAction
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ICommand? Command { get; set; }
    }

    /// <summary>
    /// Modèle pour les statistiques budgétaires
    /// </summary>
    public class BudgetStatistics
    {
        public decimal BudgetTotal { get; set; }
        public decimal DepensesEngagees { get; set; }
        public decimal DepensesPayees { get; set; }
        public decimal RecettesPercues { get; set; }
        public decimal SoldeDisponible { get; set; }
        public decimal RecettesOrdonnes {  get; set; }
        public double TauxExecution => BudgetTotal > 0 ? (double)(DepensesPayees / BudgetTotal * 100) : 0;
        public double TauxRecettes => BudgetTotal > 0 ? (double)(RecettesPercues / BudgetTotal * 100) : 0;
    }
}
