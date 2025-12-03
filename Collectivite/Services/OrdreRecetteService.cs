using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class OrdreRecetteService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération

        /// <summary>
        /// Récupère tous les ordres de recette avec leurs relations
        /// </summary>
        public async Task<List<OrdreRecette>> GetAllOrdresRecetteAsync()
        {
            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                throw new InvalidOperationException("Aucun exercice n'est sélectionné.");
            }

            return await context.OrdreRecettes
                .Where(o => o.ExerciceId == exerciceService.CurrentExercice.Id)
                .Include(o => o.BudgetLine)
                .ThenInclude(bl => bl.Nommenclature)
                .Include(o => o.Exercice)
                .Include(o => o.Commune)
                .Include(o => o.Tiers)
                .AsNoTracking()
                .OrderByDescending(o => o.DateOrdre)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un ordre de recette par son ID
        /// </summary>
        public async Task<OrdreRecette?> GetOrdreRecetteByIdAsync(int id)
        {
            using var context = CreateContext();

            return await context.OrdreRecettes
                .Include(o => o.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(o => o.Exercice)
                .Include(o => o.Commune)
                .Include(o => o.Tiers)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        /// <summary>
        /// Recherche avec filtres multiples
        /// </summary>
        public async Task<List<OrdreRecette>> SearchOrdresRecetteAsync(
            string? numeroOrdre = null,
            int? exerciceId = null,
            int? communeId = null,
            int? tiersId = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null,
            decimal? montantMin = null,
            decimal? montantMax = null)
        {
            using var context = CreateContext();

            var query = context.OrdreRecettes
                .Include(o => o.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(o => o.Exercice)
                .Include(o => o.Commune)
                .Include(o => o.Tiers)
                .AsNoTracking()
                .AsQueryable();

            // Filtre par numéro d'ordre
            if (!string.IsNullOrWhiteSpace(numeroOrdre))
            {
                query = query.Where(o => o.NumeroOrdre.Contains(numeroOrdre));
            }

            // Filtre par exercice
            if (exerciceId.HasValue && exerciceId.Value > 0)
            {
                query = query.Where(o => o.ExerciceId == exerciceId.Value);
            }

            // Filtre par commune
            if (communeId.HasValue && communeId.Value > 0)
            {
                query = query.Where(o => o.CommuneId == communeId.Value);
            }

            // Filtre par tiers
            if (tiersId.HasValue && tiersId.Value > 0)
            {
                query = query.Where(o => o.TiersId == tiersId.Value);
            }

            // Filtre par période
            if (dateDebut.HasValue)
            {
                query = query.Where(o => o.DateOrdre >= dateDebut.Value);
            }

            if (dateFin.HasValue)
            {
                query = query.Where(o => o.DateOrdre <= dateFin.Value);
            }

            // Filtre par montant
            if (montantMin.HasValue && montantMin.Value > 0)
            {
                query = query.Where(o => o.MontantOrdre >= montantMin.Value);
            }

            if (montantMax.HasValue && montantMax.Value > 0)
            {
                query = query.Where(o => o.MontantOrdre <= montantMax.Value);
            }

            return await query.OrderByDescending(o => o.DateOrdre).ToListAsync();
        }

        /// <summary>
        /// Récupère les ordres par exercice
        /// </summary>
        public async Task<List<OrdreRecette>> GetOrdresRecetteByExerciceAsync(int exerciceId)
        {
            using var context = CreateContext();

            return await context.OrdreRecettes
                .Include(o => o.BudgetLine)
                .ThenInclude(bl => bl.Nommenclature)
                .Include(o => o.Commune)
                .Include(o => o.Tiers)
                .Where(o => o.ExerciceId == exerciceId)
                .AsNoTracking()
                .OrderByDescending(o => o.DateOrdre)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les ordres par commune
        /// </summary>
        public async Task<List<OrdreRecette>> GetOrdresRecetteByCommuneAsync(int communeId)
        {
            using var context = CreateContext();

            return await context.OrdreRecettes
                .Include(o => o.BudgetLine)
                .ThenInclude(bl => bl.Nommenclature)
                .Include(o => o.Exercice)
                .Include(o => o.Tiers)
                .Where(o => o.CommuneId == communeId)
                .AsNoTracking()
                .OrderByDescending(o => o.DateOrdre)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les lignes budgétaires sans enfants
        /// </summary>
        public async Task<List<BudgetLine>> GetBudgetLinesSansEnfantsAsync()
        {
            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                throw new InvalidOperationException("Aucun exercice n'est sélectionné.");
            }

            var allLines = await context.BudgetLines
                .Where(bl => bl.BudgetPrimitif.ExerciceId == exerciceService.CurrentExercice.Id && bl.BudgetPrimitif.Status == BudgetPrimitif.Statusbudget.VALIDATED && bl.Nommenclature.Nature == NatureType.Recette)
                .Include(bl => bl.Nommenclature)
                //.Include(bl => bl.Remaniements)
                .AsNoTracking()
                .ToListAsync();

            var allNommenclatures = await context.Nommenclatures
                .Include(n => n.Enfants)
                .AsNoTracking()
                .ToListAsync();

            var nommenclaturesSansEnfants = allNommenclatures
                .Where(n => n.Enfants == null || !n.Enfants.Any())
                .Select(n => n.Id)
                .ToHashSet();

            return allLines
                .Where(bl => nommenclaturesSansEnfants.Contains(bl.NommenclatureId))
                .OrderBy(bl => bl.Nommenclature.Chapitre)
                .ThenBy(bl => bl.Nommenclature.Article)
                .ToList();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouvel ordre de recette
        /// </summary>
        public async Task<(bool Success, string Message, OrdreRecette? OrdreRecette)> CreateOrdreRecetteAsync(
             OrdreRecette ordreRecette)
        {
            using var context = CreateContext();

            try
            {
                // --- VALIDATION ---
                if (string.IsNullOrWhiteSpace(ordreRecette.NumeroOrdre))
                    return (false, "Le numéro d'ordre est obligatoire.", null);

                if (ordreRecette.BudgetLineId <= 0)
                    return (false, "La ligne budgétaire est obligatoire.", null);

                if (ordreRecette.ExerciceId <= 0)
                    return (false, "L'exercice est obligatoire.", null);

                if (ordreRecette.CommuneId <= 0)
                    return (false, "La commune est obligatoire.", null);

                if (string.IsNullOrWhiteSpace(ordreRecette.Comptable))
                    return (false, "Le nom du comptable est obligatoire.", null);

                if (ordreRecette.MontantOrdre <= 0)
                    return (false, "Le montant doit être supérieur à zéro.", null);

                if (string.IsNullOrWhiteSpace(ordreRecette.MontantOrdreLettre))
                    return (false, "Le montant en lettres est obligatoire.", null);


                // --- Vérifier l'unicité du numéro d'ordre ---
                var existingOrdre = await context.OrdreRecettes
                    .AnyAsync(o => o.NumeroOrdre == ordreRecette.NumeroOrdre);

                if (existingOrdre)
                    return (false, $"Le numéro d'ordre '{ordreRecette.NumeroOrdre}' existe déjà.", null);


                // --- Vérifier que l’exercice n'est pas clôturé ---
                var exercice = await context.Exercices.FindAsync(ordreRecette.ExerciceId);
                if (exercice != null && exercice.EstCloture)
                    return (false, "Impossible d'ajouter un ordre sur un exercice clôturé.", null);


                // --- Création ---
                var newOrdre = new OrdreRecette
                {
                    NumeroOrdre = ordreRecette.NumeroOrdre.Trim(),
                    BudgetLineId = ordreRecette.BudgetLineId,
                    ExerciceId = ordreRecette.ExerciceId,
                    CommuneId = ordreRecette.CommuneId,
                    Comptable = ordreRecette.Comptable.Trim(),
                    TiersId = ordreRecette.TiersId,
                    Motifs = ordreRecette.Motifs?.Trim(),
                    MontantOrdre = ordreRecette.MontantOrdre,
                    MontantOrdreLettre = ordreRecette.MontantOrdreLettre.Trim(),
                    DateOrdre = ordreRecette.DateOrdre
                };

                context.OrdreRecettes.Add(newOrdre);
                await context.SaveChangesAsync();


                // --- Mise à jour du montant réalisé ---
                var budgetLine = await context.BudgetLines
                    .FirstOrDefaultAsync(b => b.Id == newOrdre.BudgetLineId);

                if (budgetLine != null)
                {
                    budgetLine.MontantRealise += newOrdre.MontantOrdre;
                    await context.SaveChangesAsync();

                    // 🔥 recalcul hiérarchique
                    using var ctx = CreateContext();
                    await RecalculateRealisation(
                        ctx,
                        budgetLine.NommenclatureId,
                        budgetLine.BudgetPrimitifId
                    );
                }


                // --- Recharger avec ses relations ---
                var savedOrdre = await GetOrdreRecetteByIdAsync(newOrdre.Id);

                return (true, "Ordre de recette créé avec succès.", savedOrdre);
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                return (false, $"Erreur de base de données : {inner}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}", null);
            }
        }


        public static  async Task RecalculateRealisation(AppDbContext context, int childNomenclatureId, int budgetPrimitifId)
        {
            
            // Charger toute la hiérarchie une seule fois
            var allNodes = await context.Nommenclatures.ToListAsync();

            // Charger toutes les lignes budget pour éviter les requêtes répétées
            var allBudgetLines = await context.BudgetLines
                .Where(b => b.BudgetPrimitifId == budgetPrimitifId)
                .ToListAsync();

            // Trouver le point de départ
            var child = allNodes.FirstOrDefault(n => n.Id == childNomenclatureId);
            if (child == null) return;

            var parentId = child.ParentId;

            while (parentId.HasValue)
            {
                // Récupérer les enfants directs du parent
                var childrenIds = allNodes
                    .Where(n => n.ParentId == parentId.Value)
                    .Select(n => n.Id)
                    .ToList();

                // SOMME des montants réalisés des enfants
                var sommeEnfants = allBudgetLines
                    .Where(b => childrenIds.Contains(b.NommenclatureId))
                    .Sum(b => b.MontantRealise);

                // Trouver la ligne budget du parent
                var parentLine = allBudgetLines
                    .FirstOrDefault(b => b.NommenclatureId == parentId.Value);

                if (parentLine != null)
                    parentLine.MontantRealise = sommeEnfants;

                // Monter au parent suivant
                parentId = allNodes
                    .FirstOrDefault(n => n.Id == parentId.Value)?
                    .ParentId;
            }

            await context.SaveChangesAsync();
        }


        #endregion

        #region Modification

        /// <summary>
        /// Met à jour un ordre de recette
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateOrdreRecetteAsync(OrdreRecette ordreRecette)
        {
            using var context = CreateContext();

            try
            {
                var existingOrdre = await context.OrdreRecettes.FindAsync(ordreRecette.Id);

                if (existingOrdre == null)
                    return (false, "Ordre de recette introuvable.");

                // Vérifier l'unicité du numéro (sauf pour l'ordre actuel)
                var duplicateNumero = await context.OrdreRecettes
                    .AnyAsync(o => o.NumeroOrdre == ordreRecette.NumeroOrdre && o.Id != ordreRecette.Id);

                if (duplicateNumero)
                    return (false, $"Le numéro d'ordre '{ordreRecette.NumeroOrdre}' existe déjà.");

                // Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(ordreRecette.ExerciceId);
                if (exercice != null && exercice.EstCloture)
                    return (false, "Impossible de modifier un ordre sur un exercice clôturé.");

                // Mettre à jour
                existingOrdre.NumeroOrdre = ordreRecette.NumeroOrdre.Trim();
                existingOrdre.BudgetLineId = ordreRecette.BudgetLineId;
                existingOrdre.ExerciceId = ordreRecette.ExerciceId;
                existingOrdre.CommuneId = ordreRecette.CommuneId;
                existingOrdre.Comptable = ordreRecette.Comptable?.Trim() ?? "";
                existingOrdre.TiersId = ordreRecette.TiersId;
                existingOrdre.Motifs = ordreRecette.Motifs?.Trim();
                existingOrdre.MontantOrdre = ordreRecette.MontantOrdre;
                existingOrdre.MontantOrdreLettre = ordreRecette.MontantOrdreLettre.Trim();
                existingOrdre.DateOrdre = ordreRecette.DateOrdre;

                await context.SaveChangesAsync();

                return (true, "✅ Ordre de recette modifié avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un ordre de recette
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteOrdreRecetteAsync(int id)
        {
            using var context = CreateContext();

            try
            {
                var ordreRecette = await context.OrdreRecettes
                    .Include(o => o.EcritureComptables)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (ordreRecette == null)
                    return (false, "Ordre de recette introuvable.");

                // Vérifier s'il y a des écritures comptables liées
                if (ordreRecette.EcritureComptables != null && ordreRecette.EcritureComptables.Any())
                {
                    return (false,
                        "Impossible de supprimer cet ordre car il est lié à des écritures comptables.");
                }

                // Vérifier que l'exercice n'est pas clôturé
                var exercice = await context.Exercices.FindAsync(ordreRecette.ExerciceId);
                if (exercice != null && exercice.EstCloture)
                    return (false, "Impossible de supprimer un ordre sur un exercice clôturé.");

                context.OrdreRecettes.Remove(ordreRecette);
                await context.SaveChangesAsync();

                return (true, "✅ Ordre de recette supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Statistiques

        /// <summary>
        /// Obtient le total des montants par exercice
        /// </summary>
        public async Task<Dictionary<int, decimal>> GetTotalParExerciceAsync()
        {
            using var context = CreateContext();

            return await context.OrdreRecettes
                .GroupBy(o => o.ExerciceId)
                .Select(g => new { ExerciceId = g.Key, Total = g.Sum(o => o.MontantOrdre) })
                .ToDictionaryAsync(x => x.ExerciceId, x => x.Total);
        }

        /// <summary>
        /// Obtient le total des montants par commune
        /// </summary>
        public async Task<Dictionary<int, decimal>> GetTotalParCommuneAsync(int exerciceId)
        {
            using var context = CreateContext();

            return await context.OrdreRecettes
                .Where(o => o.ExerciceId == exerciceId)
                .GroupBy(o => o.CommuneId)
                .Select(g => new { CommuneId = g.Key, Total = g.Sum(o => o.MontantOrdre) })
                .ToDictionaryAsync(x => x.CommuneId, x => x.Total);
        }

        /// <summary>
        /// Génère le prochain numéro d'ordre
        /// </summary>
        public async Task<string> GenerateNextNumeroOrdreAsync(int exerciceId, int communeId)
        {
            using var context = CreateContext();

            var exercice = await context.Exercices.FindAsync(exerciceId);
            var commune = await context.Communes.FindAsync(communeId);

            if (exercice == null || commune == null)
                return $"OR-{DateTime.Now:yyyyMMdd}-0001";

            var count = await context.OrdreRecettes
                .Where(o => o.ExerciceId == exerciceId && o.CommuneId == communeId)
                .CountAsync();

            return $"OR-{exercice.Libelle}-{commune.Nom.Substring(0, 3).ToUpper()}-{(count + 1):D4}";
        }

        #endregion
    }
}