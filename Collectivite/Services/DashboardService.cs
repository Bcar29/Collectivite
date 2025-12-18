using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.Services
{
    public class DashboardService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        /// <summary>
        /// Récupère les statistiques budgétaires pour un exercice donné
        /// </summary>
        public async Task<BudgetStatistics> GetBudgetStatisticsAsync(int exerciceId)
        {
            var statistics = new BudgetStatistics();

            using var context = CreateContext();

            // 1. Budget Total (depuis BudgetPrimitif)
            var budgetPrimitif = await context.BudgetsPrimitifs
                .FirstOrDefaultAsync(b => b.ExerciceId == exerciceId);

            if (budgetPrimitif != null)
            {
                statistics.BudgetTotal = budgetPrimitif.MontantRecette;
            }

            // 2. Dépenses Engagées (somme des engagements)
            statistics.DepensesEngagees = await context.Engagements
                .Where(e => e.ExerciceId == exerciceId)
                .SumAsync(e => (decimal?)e.MontantEngagement) ?? 0;

            // 3. Depense payées
            statistics.DepensesPayees = await context.Mouvements
                .Where(m => m.Mandat != null && m.Mandat.Engagement.ExerciceId == exerciceId)
                .SumAsync(m => m.Montant);

            // 3. Recettes Perçues 
            statistics.RecettesPercues = await context.Mouvements
                .Where(m => m.OrdreRecette != null && m.OrdreRecette.ExerciceId == exerciceId)
                .SumAsync(m => m.Montant);

            // 3. Recettes Ordonnes (somme des ordres de recettes)
            statistics.RecettesOrdonnes = await context.OrdreRecettes
                .Where(o => o.ExerciceId == exerciceId)
                .SumAsync(o => (decimal?)o.MontantOrdre) ?? 0;

            // 4. Solde Disponible (Budget Total - Dépenses Payées)
            statistics.SoldeDisponible = statistics.RecettesPercues - statistics.DepensesPayees;

            return statistics;
        }

        /// <summary>
        /// Récupère les statistiques budgétaires de l'exercice courant
        /// </summary>
        public async Task<BudgetStatistics> GetBudgetStatisticsAsync()
        {
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new BudgetStatistics();
            }

            return await GetBudgetStatisticsAsync(exerciceService.CurrentExercice.Id);
        }

        /// <summary>
        /// Récupère les données pour le graphique en barres (Fonctionnement vs Investissement)
        /// </summary>
        public async Task<List<ChartDataPoint>> GetBarChartDataAsync(int exerciceId)
        {
            var data = new List<ChartDataPoint>();
            using var context = CreateContext();

            // Recettes par type
            var recettesFonctionnement = await context.OrdreRecettes
                .Include(o => o.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Where(o => o.ExerciceId == exerciceId &&
                           o.BudgetLine.Nommenclature.Nature == NatureType.Recette &&
                           o.BudgetLine.Nommenclature.Section == SectionType.Fonctionnement)
                .SumAsync(o => (decimal?)o.MontantOrdre) ?? 0;

            var recettesInvestissement = await context.OrdreRecettes
                .Include(o => o.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Where(o => o.ExerciceId == exerciceId &&
                           o.BudgetLine.Nommenclature.Nature == NatureType.Recette &&
                           o.BudgetLine.Nommenclature.Section == SectionType.Investissement)
                .SumAsync(o => (decimal?)o.MontantOrdre) ?? 0;

            // Dépenses par type
            var depensesFonctionnement = await context.Engagements
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Where(e => e.ExerciceId == exerciceId &&
                           e.BudgetLine.Nommenclature.Nature == NatureType.Depense &&
                           e.BudgetLine.Nommenclature.Section == SectionType.Fonctionnement)
                .SumAsync(e => (decimal?)e.MontantEngagement) ?? 0;

            var depensesInvestissement = await context.Engagements
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Where(e => e.ExerciceId == exerciceId &&
                           e.BudgetLine.Nommenclature.Nature == NatureType.Depense &&
                           e.BudgetLine.Nommenclature.Section == SectionType.Investissement)
                .SumAsync(e => (decimal?)e.MontantEngagement) ?? 0;

            // Ajouter les données au graphique (conversion en millions pour lisibilité)
            data.Add(new ChartDataPoint { Label = "Fonctionnement", Value = (double)(recettesFonctionnement / 1_000_000), Category = "Recettes" });
            data.Add(new ChartDataPoint { Label = "Investissement", Value = (double)(recettesInvestissement / 1_000_000), Category = "Recettes" });
            data.Add(new ChartDataPoint { Label = "Fonctionnement", Value = (double)(depensesFonctionnement / 1_000_000), Category = "Dépenses" });
            data.Add(new ChartDataPoint { Label = "Investissement", Value = (double)(depensesInvestissement / 1_000_000), Category = "Dépenses" });

            return data;
        }

        /// <summary>
        /// Récupère les données pour le graphique en barres de l'exercice courant
        /// </summary>
        public async Task<List<ChartDataPoint>> GetBarChartDataAsync()
        {
            var exerciceService = ExerciceService.Instance;
            
            if (exerciceService.CurrentExercice == null)
            {
                return new List<ChartDataPoint>();
            }
            

            return await GetBarChartDataAsync(exerciceService.CurrentExercice.Id);
        }

        /// <summary>
        /// Récupère les données pour le graphique en lignes (évolution mensuelle)
        /// </summary>
        public async Task<List<ChartDataPoint>> GetLineChartDataAsync(int exerciceId)
        {
            using var context = CreateContext();
            var data = new List<ChartDataPoint>();

            var months = new[]
            {
                "Jan", "Fév", "Mar", "Avr", "Mai", "Jun",
                "Jul", "Aoû", "Sep", "Oct", "Nov", "Déc"
            };

            for (int month = 1; month <= 12; month++)
            {
                // ===================== RECETTES =====================
                var recettesMois = await context.Mouvements
                    .Where(m =>
                        m.OrdreRecette != null &&
                        m.OrdreRecette.ExerciceId == exerciceId &&
                        m.Date.Month == month
                    )
                    .SumAsync(m => (decimal?)m.Montant) ?? 0;
                

                // ===================== DÉPENSES =====================
                var depensesMois = await context.Mouvements
                    .Where(m =>
                        m.Mandat != null &&
                        m.Mandat.Engagement.ExerciceId == exerciceId &&
                        m.Date.Month == month
                    )
                    .SumAsync(m => (decimal?)m.Montant) ?? 0;

                // ===================== AJOUT AU GRAPHIQUE =====================
                data.Add(new ChartDataPoint
                {
                    Label = months[month - 1],
                    Value = (double)(recettesMois / 1_000_000),
                    Category = "Recettes"
                });

                data.Add(new ChartDataPoint
                {
                    Label = months[month - 1],
                    Value = (double)(depensesMois / 1_000_000),
                    Category = "Dépenses"
                });
            }

            Debug.WriteLine($"debut de ma boucle");

            foreach (var d in data)
            {
                Debug.WriteLine($"{d.Label} | {d.Category} | {d.Value}");
            }

                Debug.WriteLine($"fin de ma boucle");

            return data;
        }


        /// <summary>
        /// Récupère les données pour le graphique en lignes de l'exercice courant
        /// </summary>
        public async Task<List<ChartDataPoint>> GetLineChartDataAsync()
        {
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<ChartDataPoint>();
            }

            return await GetLineChartDataAsync(exerciceService.CurrentExercice.Id);
        }

        /// <summary>
        /// Calcule le pourcentage de changement par rapport à l'exercice précédent
        /// </summary>
        public async Task<double> GetPercentageChangeAsync(int exerciceId, string indicatorType)
        {
            using var context = CreateContext();

            // Récupérer l'exercice actuel
            var exerciceActuel = await context.Exercices.FindAsync(exerciceId);
            if (exerciceActuel == null) return 0;

            // Extraire l'année actuelle depuis le libellé
            var anneeActuelle = exerciceActuel.GetAnnee();
            if (anneeActuelle == null) return 0;

            // Charger UNIQUEMENT les exercices contenant un nombre
            var candidats = await context.Exercices
                //.Where(e => e.Libelle != null && EF.Functions.Like(e.Libelle, "%[0-9]%"))
                .ToListAsync();

            // Trouver l'exercice précédent via GetAnnee()
            var exercicePrecedent = candidats
                .Where(e => e.GetAnnee() == anneeActuelle.Value - 1)
                .FirstOrDefault();

            if (exercicePrecedent == null) return 0;

            decimal montantActuel = 0;
            decimal montantPrecedent = 0;

            switch (indicatorType.ToLower())
            {
                case "budget":
                    montantActuel = await context.BudgetsPrimitifs
                        .Where(b => b.ExerciceId == exerciceId)
                        .Select(b => b.MontantTotal)
                        .FirstOrDefaultAsync();

                    montantPrecedent = await context.BudgetsPrimitifs
                        .Where(b => b.ExerciceId == exercicePrecedent.Id)
                        .Select(b => b.MontantTotal)
                        .FirstOrDefaultAsync();
                    break;

                case "depenses":
                    montantActuel = await context.Engagements
                        .Where(e => e.ExerciceId == exerciceId)
                        .SumAsync(e => (decimal?)e.MontantEngagement) ?? 0;

                    montantPrecedent = await context.Engagements
                        .Where(e => e.ExerciceId == exercicePrecedent.Id)
                        .SumAsync(e => (decimal?)e.MontantEngagement) ?? 0;
                    break;

                case "recettes":
                    montantActuel = await context.OrdreRecettes
                        .Where(o => o.ExerciceId == exerciceId)
                        .SumAsync(o => (decimal?)o.MontantOrdre) ?? 0;

                    montantPrecedent = await context.OrdreRecettes
                        .Where(o => o.ExerciceId == exercicePrecedent.Id)
                        .SumAsync(o => (decimal?)o.MontantOrdre) ?? 0;
                    break;

                case "solde":
                    var statsActuel = await GetBudgetStatisticsAsync(exerciceId);
                    var statsPrecedent = await GetBudgetStatisticsAsync(exercicePrecedent.Id);
                    montantActuel = statsActuel.SoldeDisponible;
                    montantPrecedent = statsPrecedent.SoldeDisponible;
                    break;
            }

            if (montantPrecedent == 0) return 0;

            return (double)(((montantActuel - montantPrecedent) / montantPrecedent) * 100);
        }

        /// <summary>
        /// Calcule le pourcentage de changement pour l'exercice courant
        /// </summary>
        public async Task<double> GetPercentageChangeAsync(string indicatorType)
        {
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return 0;
            }

            return await GetPercentageChangeAsync(exerciceService.CurrentExercice.Id, indicatorType);
        }

        /// <summary>
        /// Récupère tous les indicateurs avec leurs pourcentages de changement
        /// </summary>
        public async Task<List<DashboardIndicator>> GetIndicatorsAsync(int exerciceId)
        {
            var statistics = await GetBudgetStatisticsAsync(exerciceId);
            var indicators = new List<DashboardIndicator>();

            indicators.Add(new DashboardIndicator
            {
                Title = "Budget Total",
                Amount = statistics.BudgetTotal,
                Icon = "CashMultiple",
                Color = "#1976D2",
                PercentageChange = await GetPercentageChangeAsync(exerciceId, "budget")
            });

            indicators.Add(new DashboardIndicator
            {
                Title = "Dépenses Engagées",
                Amount = statistics.DepensesEngagees,
                Icon = "TrendingDown",
                Color = "#F44336",
                PercentageChange = await GetPercentageChangeAsync(exerciceId, "depenses")
            });
            indicators.Add(new DashboardIndicator
            {
                Title = "Recettes Ordonnées",
                Amount = statistics.RecettesOrdonnes,
                Icon = "TrendingUp",
                Color = "#4CAF50",
                PercentageChange = await GetPercentageChangeAsync(exerciceId, "recettes")
            });

            indicators.Add(new DashboardIndicator
            {
                Title = "Solde Disponible",
                Amount = statistics.SoldeDisponible,
                Icon = "WalletOutline",
                Color = "#FF9800",
                PercentageChange = await GetPercentageChangeAsync(exerciceId, "solde")
            });

            indicators.Add(new DashboardIndicator
            {
                Title = "Dépenses Payées",
                Amount = statistics.DepensesPayees,
                Icon = "TrendingDown",
                Color = "#F44336",
                PercentageChange = await GetPercentageChangeAsync(exerciceId, "depenses")
            });

            indicators.Add(new DashboardIndicator
            {
                Title = "Recettes Perçues",
                Amount = statistics.RecettesPercues,
                Icon = "TrendingUp",
                Color = "#4CAF50",
                PercentageChange = await GetPercentageChangeAsync(exerciceId, "recettes")
            });

            return indicators;
        }

        /// <summary>
        /// Récupère tous les indicateurs de l'exercice courant
        /// </summary>
        public async Task<List<DashboardIndicator>> GetIndicatorsAsync()
        {
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<DashboardIndicator>();
            }

            return await GetIndicatorsAsync(exerciceService.CurrentExercice.Id);
        }
    }
}