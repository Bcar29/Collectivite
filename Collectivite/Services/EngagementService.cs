using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour la gestion des engagements
    /// </summary>
    public class EngagementService
    {
        // ✅ Créer un nouveau DbContext pour chaque opération
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération des données

        /// <summary>
        /// Récupère tous les engagements
        /// </summary>
        public async Task<List<Engagement>> GetAllEngagementsAsync()
        {
            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<Engagement>();
            }

            return await context.Engagements
                .Where(e => e.ExerciceId == exerciceService.CurrentExercice.Id)
                .Include(e => e.Exercice)
                .Include(e => e.Commune)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.Tiers)
                .Include(e => e.Contrat)
                .Include(e => e.Facture)
                .Include(e => e.BonCommandes)
                .AsNoTracking()
                .OrderByDescending(e => e.DateEngagement)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un engagement par son ID
        /// </summary>
        public async Task<Engagement?> GetEngagementByIdAsync(int id)
        {
            using var context = CreateContext();

            return await context.Engagements
                .Include(e => e.Exercice)
                .Include(e => e.Commune)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.Tiers)
                .Include(e => e.Contrat)
                .Include(e => e.Facture)
                .Include(e => e.Mandat)
                .Include(e => e.BonCommandes)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        /// <summary>
        /// Récupère les engagements par exercice
        /// </summary>
        public async Task<List<Engagement>> GetEngagementsByExerciceAsync(int exerciceId)
        {
            using var context = CreateContext();

            return await context.Engagements
                .Include(e => e.Exercice)
                .Include(e => e.Commune)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.Tiers)
                .Where(e => e.ExerciceId == exerciceId)
                .AsNoTracking()
                .OrderByDescending(e => e.DateEngagement)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les engagements par tiers
        /// </summary>
        public async Task<List<Engagement>> GetEngagementsByTiersAsync(int tiersId)
        {
            using var context = CreateContext();

            return await context.Engagements
                .Include(e => e.Exercice)
                .Include(e => e.Commune)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.Tiers)
                .Where(e => e.TiersId == tiersId)
                .AsNoTracking()
                .OrderByDescending(e => e.DateEngagement)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les engagements par ligne budgétaire
        /// </summary>
        public async Task<List<Engagement>> GetEngagementsByBudgetLineAsync(int budgetLineId)
        {
            using var context = CreateContext();

            return await context.Engagements
                .Include(e => e.Exercice)
                .Include(e => e.Commune)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.Tiers)
                .Where(e => e.BudgetLineId == budgetLineId)
                .AsNoTracking()
                .OrderByDescending(e => e.DateEngagement)
                .ToListAsync();
        }

        /// <summary>
        /// Recherche des engagements
        /// </summary>
        public async Task<List<Engagement>> SearchEngagementsAsync(string searchTerm)
        {
            using var context = CreateContext();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllEngagementsAsync();

            searchTerm = searchTerm.ToLower();

            return await context.Engagements
                .Include(e => e.Exercice)
                .Include(e => e.Commune)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.Tiers)
                .Where(e => e.Objet.ToLower().Contains(searchTerm) ||
                           //e.Tiers.Nom.ToLower().Contains(searchTerm) ||
                           e.BudgetLine.Nommenclature.Intitule.ToLower().Contains(searchTerm))
                .AsNoTracking()
                .OrderByDescending(e => e.DateEngagement)
                .ToListAsync();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouveau engagement
        /// </summary>
        public async Task<(bool Success, string Message, Engagement? Engagement)> CreateEngagementAsync(Engagement engagement)
        {
            using var context = CreateContext();

            try
            {
                // Validation
                if (engagement.ExerciceId <= 0)
                    return (false, "L'exercice est obligatoire.", null);

                if (engagement.CommuneId <= 0)
                    return (false, "La commune est obligatoire.", null);

                if (engagement.BudgetLineId <= 0)
                    return (false, "La ligne budgétaire est obligatoire.", null);

                //if (engagement.TiersId <= 0)
                //    return (false, "Le tiers est obligatoire.", null);

                if (string.IsNullOrWhiteSpace(engagement.Objet))
                    return (false, "L'objet de l'engagement est obligatoire.", null);

                if (engagement.MontantEngagement <= 0)
                    return (false, "Le montant de l'engagement doit être supérieur à zéro.", null);

                // Vérifier que l'exercice existe et n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(engagement.ExerciceId);
                if (exercice == null)
                    return (false, "L'exercice spécifié n'existe pas.", null);

                if (exercice.EstCloture)
                    return (false, "Impossible de créer un engagement sur un exercice clôturé.", null);

                // Vérifier que le tiers existe
                var tiersExists = await context.Tiers.AnyAsync(t => t.Id == engagement.TiersId);
                if (!tiersExists)
                    return (false, "Le tiers spécifié n'existe pas.", null);

                // Vérifier que la ligne budgétaire existe
                var budgetLineExists = await context.BudgetLines.AnyAsync(bl => bl.Id == engagement.BudgetLineId);
                if (!budgetLineExists)
                    return (false, "La ligne budgétaire spécifiée n'existe pas.", null);

                // Vérifier les crédits disponibles
                if (engagement.MontantEngagement > engagement.CreditsBudgetaires - engagement.EngagementsAnterieurs)
                {
                    return (false, $"Le montant de l'engagement {engagement.MontantEngagement} dépasse les crédits disponibles  {engagement.CreditsBudgetaires - engagement.EngagementsAnterieurs}.", null);
                }

                // ✅ Créer un nouvel objet sans navigation
                var newEngagement = new Engagement
                {
                    ExerciceId = engagement.ExerciceId,
                    CommuneId = engagement.CommuneId,
                    BudgetLineId = engagement.BudgetLineId,
                    TiersId = engagement.TiersId,
                    Objet = engagement.Objet.Trim(),
                    DateEngagement = engagement.DateEngagement,
                    CreditsBudgetaires = engagement.CreditsBudgetaires,
                    EngagementsAnterieurs = engagement.EngagementsAnterieurs,
                    MontantEngagement = engagement.MontantEngagement,
                    FichierJoin = engagement.FichierJoin,
                    FichierName = engagement.FichierName,
                    ContratId = engagement.ContratId,
                    FactureId = engagement.FactureId

                };

                context.Engagements.Add(newEngagement);
                await context.SaveChangesAsync();

                // Recharger avec les relations
                var savedEngagement = await GetEngagementByIdAsync(newEngagement.Id);

                return (true, "Engagement créé avec succès.", savedEngagement);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return (false, $"Erreur de base de données : {innerMessage}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}", null);
            }
        }

        #endregion

        #region Modification

        /// <summary>
        /// Met à jour un engagement existant
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateEngagementAsync(Engagement engagement)
        {
            using var context = CreateContext();

            try
            {
                var existingEngagement = await context.Engagements.FindAsync(engagement.Id);

                if (existingEngagement == null)
                    return (false, "Engagement introuvable.");

                // Validation
                if (string.IsNullOrWhiteSpace(engagement.Objet))
                    return (false, "L'objet de l'engagement est obligatoire.");

                if (engagement.MontantEngagement <= 0)
                    return (false, "Le montant de l'engagement doit être supérieur à zéro.");

                // Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(engagement.ExerciceId);
                if (exercice?.EstCloture == true)
                    return (false, "Impossible de modifier un engagement sur un exercice clôturé.");

                // Mettre à jour
                existingEngagement.ExerciceId = engagement.ExerciceId;
                existingEngagement.CommuneId = engagement.CommuneId;
                existingEngagement.BudgetLineId = engagement.BudgetLineId;
                existingEngagement.TiersId = engagement.TiersId;
                existingEngagement.Objet = engagement.Objet.Trim();
                existingEngagement.DateEngagement = engagement.DateEngagement;
                existingEngagement.CreditsBudgetaires = engagement.CreditsBudgetaires;
                existingEngagement.EngagementsAnterieurs = engagement.EngagementsAnterieurs;
                existingEngagement.MontantEngagement = engagement.MontantEngagement;
                existingEngagement.FichierJoin = engagement.FichierJoin;
                existingEngagement.ContratId = engagement.ContratId;
                existingEngagement.FactureId = engagement.FactureId;

                await context.SaveChangesAsync();

                return (true, "Engagement modifié avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la modification : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un engagement
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteEngagementAsync(int id)
        {
            using var context = CreateContext();

            try
            {
                var engagement = await context.Engagements
                    .Include(e => e.Mandat)
                    .Include(e => e.BonCommandes)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (engagement == null)
                    return (false, "Engagement introuvable.");

                // Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(engagement.ExerciceId);
                if (exercice?.EstCloture == true)
                    return (false, "Impossible de supprimer un engagement sur un exercice clôturé.");

                // Vérifier s'il y a un mandat lié
                if (engagement.Mandat != null)
                {
                    return (false, "Impossible de supprimer cet engagement car un mandat y est lié.");
                }

                // Vérifier s'il y a des bons de commande liés
                if (engagement.BonCommandes?.Any() == true)
                {
                    return (false, $"Impossible de supprimer cet engagement car {engagement.BonCommandes.Count} bon(s) de commande y sont liés.");
                }

                context.Engagements.Remove(engagement);
                await context.SaveChangesAsync();

                return (true, "Engagement supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        #endregion

        #region Statistiques

        /// <summary>
        /// Obtient les statistiques des engagements
        /// </summary>
        public async Task<EngagementStatistiques> GetStatistiquesAsync(int? exerciceId = null)
        {
            using var context = CreateContext();

            var query = context.Engagements.AsQueryable();

            if (exerciceId.HasValue)
            {
                query = query.Where(e => e.ExerciceId == exerciceId.Value);
            }

            var totalEngagements = await query.CountAsync();
            var montantTotal = await query.SumAsync(e => e.MontantEngagement);
            var montantMoyen = totalEngagements > 0 ? montantTotal / totalEngagements : 0;

            var parTiers = await query
                .GroupBy(e => e.Tiers.Nom)
                .Select(g => new { Tiers = g.Key, Montant = g.Sum(e => e.MontantEngagement) })
                .OrderByDescending(x => x.Montant)
                .Take(10)
                .ToListAsync();

            return new EngagementStatistiques
            {
                TotalEngagements = totalEngagements,
                MontantTotal = montantTotal,
                MontantMoyen = montantMoyen,
                Top10Tiers = parTiers.ToDictionary(x => x.Tiers, x => x.Montant)
            };
        }

        #endregion
    }

    /// <summary>
    /// Classe pour les statistiques des engagements
    /// </summary>
    public class EngagementStatistiques
    {
        public int TotalEngagements { get; set; }
        public double MontantTotal { get; set; }
        public double MontantMoyen { get; set; }
        public Dictionary<string, double> Top10Tiers { get; set; } = new();
    }
}