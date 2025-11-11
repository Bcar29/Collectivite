using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class RecensementService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération

        /// <summary>
        /// Récupère tous les recensements avec leurs relations
        /// </summary>
        public async Task<List<Recensement>> GetAllRecensementsAsync()
        {
            using var context = CreateContext();

            return await context.Recensements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(r => r.Exercice)
                .Include(r => r.Commune)
                .Include(r => r.Tiers)
                .AsNoTracking()
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un recensement par son ID
        /// </summary>
        public async Task<Recensement?> GetRecensementByIdAsync(int id)
        {
            using var context = CreateContext();

            return await context.Recensements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(r => r.Exercice)
                .Include(r => r.Commune)
                .Include(r => r.Tiers)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Récupère les recensements par exercice
        /// </summary>
        public async Task<List<Recensement>> GetRecensementsByExerciceAsync(int exerciceId)
        {
            using var context = CreateContext();

            return await context.Recensements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(r => r.Commune)
                .Include(r => r.Tiers)
                .Where(r => r.ExerciceId == exerciceId)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les recensements par commune
        /// </summary>
        public async Task<List<Recensement>> GetRecensementsByCommuneAsync(int communeId)
        {
            using var context = CreateContext();

            return await context.Recensements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(r => r.Exercice)
                .Include(r => r.Tiers)
                .Where(r => r.CommuneId == communeId)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les lignes budgétaires des recettes fiscales uniquement (71xx)
        /// </summary>
        public async Task<List<BudgetLine>> GetRecettesFiscalesBudgetLinesAsync()
        {
            using var context = CreateContext();

            return await context.BudgetLines
                .Include(bl => bl.Nommenclature)
                .Include(bl => bl.Remaniements)
                .Where(bl => bl.Nommenclature.Chapitre != null &&
                            bl.Nommenclature.Chapitre.StartsWith("71"))
                .AsNoTracking()
                .OrderBy(bl => bl.Nommenclature.Chapitre)
                .ThenBy(bl => bl.Nommenclature.Article)
                .ThenBy(bl => bl.Nommenclature.Paragraphe)
                .ToListAsync();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouveau recensement
        /// </summary>
        public async Task<(bool Success, string Message, Recensement? Recensement)> CreateRecensementAsync(
            Recensement recensement)
        {
            using var context = CreateContext();

            try
            {
                // Validation
                if (recensement.BudgetLineId <= 0)
                    return (false, "La ligne budgétaire est obligatoire.", null);

                if (recensement.ExerciceId <= 0)
                    return (false, "L'exercice est obligatoire.", null);

                if (recensement.CommuneId <= 0)
                    return (false, "La commune est obligatoire.", null);

                if (recensement.TiersId <= 0)
                    return (false, "Le tiers est obligatoire.", null);

                if (recensement.MontantRecense <= 0)
                    return (false, "Le montant recensé doit être supérieur à zéro.", null);

                // Vérifier que la ligne budgétaire est bien une recette fiscale (71xx)
                var budgetLine = await context.BudgetLines
                    .Include(bl => bl.Nommenclature)
                    .FirstOrDefaultAsync(bl => bl.Id == recensement.BudgetLineId);

                if (budgetLine == null)
                    return (false, "Ligne budgétaire introuvable.", null);

                if (!budgetLine.Nommenclature.Chapitre?.StartsWith("71") ?? true)
                {
                    return (false,
                        "⚠️ Cette ligne budgétaire n'est pas une recette fiscale (71xx).\n" +
                        "Seules les recettes fiscales peuvent être recensées.",
                        null);
                }

                // Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(recensement.ExerciceId);
                if (exercice != null && exercice.EstCloture)
                {
                    return (false, "Impossible d'ajouter un recensement sur un exercice clôturé.", null);
                }

                // Créer le recensement
                var newRecensement = new Recensement
                {
                    BudgetLineId = recensement.BudgetLineId,
                    ExerciceId = recensement.ExerciceId,
                    CommuneId = recensement.CommuneId,
                    TiersId = recensement.TiersId,
                    MontantRecense = recensement.MontantRecense
                };

                context.Recensements.Add(newRecensement);
                await context.SaveChangesAsync();

                // Recharger avec les relations
                var savedRecensement = await GetRecensementByIdAsync(newRecensement.Id);

                return (true, "✅ Recensement créé avec succès.", savedRecensement);
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
        /// Met à jour un recensement
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateRecensementAsync(Recensement recensement)
        {
            using var context = CreateContext();

            try
            {
                var existingRecensement = await context.Recensements.FindAsync(recensement.Id);

                if (existingRecensement == null)
                    return (false, "Recensement introuvable.");

                // Vérifier que la ligne budgétaire est une recette fiscale
                var budgetLine = await context.BudgetLines
                    .Include(bl => bl.Nommenclature)
                    .FirstOrDefaultAsync(bl => bl.Id == recensement.BudgetLineId);

                if (budgetLine == null)
                    return (false, "Ligne budgétaire introuvable.");

                if (!budgetLine.Nommenclature.Chapitre?.StartsWith("71") ?? true)
                {
                    return (false, "Cette ligne budgétaire n'est pas une recette fiscale (71xx).");
                }

                // Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(recensement.ExerciceId);
                if (exercice != null && exercice.EstCloture)
                {
                    return (false, "Impossible de modifier un recensement sur un exercice clôturé.");
                }

                // Mettre à jour
                existingRecensement.BudgetLineId = recensement.BudgetLineId;
                existingRecensement.ExerciceId = recensement.ExerciceId;
                existingRecensement.CommuneId = recensement.CommuneId;
                existingRecensement.TiersId = recensement.TiersId;
                existingRecensement.MontantRecense = recensement.MontantRecense;

                await context.SaveChangesAsync();

                return (true, "✅ Recensement modifié avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un recensement
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteRecensementAsync(int id)
        {
            using var context = CreateContext();

            try
            {
                var recensement = await context.Recensements.FindAsync(id);

                if (recensement == null)
                    return (false, "Recensement introuvable.");

                // Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(recensement.ExerciceId);
                if (exercice != null && exercice.EstCloture)
                {
                    return (false, "Impossible de supprimer un recensement sur un exercice clôturé.");
                }

                context.Recensements.Remove(recensement);
                await context.SaveChangesAsync();

                return (true, "✅ Recensement supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Statistiques

        /// <summary>
        /// Obtient le total des montants recensés par commune
        /// </summary>
        public async Task<Dictionary<int, double>> GetTotalParCommuneAsync(int exerciceId)
        {
            using var context = CreateContext();

            return await context.Recensements
                .Where(r => r.ExerciceId == exerciceId)
                .GroupBy(r => r.CommuneId)
                .Select(g => new { CommuneId = g.Key, Total = g.Sum(r => r.MontantRecense) })
                .ToDictionaryAsync(x => x.CommuneId, x => x.Total);
        }

        /// <summary>
        /// Obtient le total des montants recensés par ligne budgétaire
        /// </summary>
        public async Task<Dictionary<int, double>> GetTotalParBudgetLineAsync(int exerciceId)
        {
            using var context = CreateContext();

            return await context.Recensements
                .Where(r => r.ExerciceId == exerciceId)
                .GroupBy(r => r.BudgetLineId)
                .Select(g => new { BudgetLineId = g.Key, Total = g.Sum(r => r.MontantRecense) })
                .ToDictionaryAsync(x => x.BudgetLineId, x => x.Total);
        }

        #endregion
    }
}