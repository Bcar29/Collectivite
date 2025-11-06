using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class RemaniementService
    {
        private readonly AppDbContext _context;

        public RemaniementService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère tous les remaniements avec leurs BudgetLines
        /// </summary>
        /// <summary>
        /// Récupère tous les remaniements avec leurs relations
        /// </summary>
        public async Task<List<Remaniement>> GetAllRemaniementsAsync()
        {
            // ✅ Étape 1: Charger les remaniements avec leur BudgetLine et Nomenclature
            var remaniements = await _context.Remaniements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                // ❌ NE PAS inclure bl.Remaniements ici (cycle!)
                .AsNoTracking()
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            // ✅ Étape 2: Charger tous les remaniements groupés par BudgetLine
            var budgetLineIds = remaniements.Select(r => r.IdBudgetLine).Distinct().ToList();

            var allRemaniementsByBudgetLine = await _context.Remaniements
                .Where(r => budgetLineIds.Contains(r.IdBudgetLine))
                .AsNoTracking()
                .ToListAsync();

            // ✅ Étape 3: Assigner manuellement les remaniements à chaque BudgetLine
            var remaniementsGrouped = allRemaniementsByBudgetLine
                .GroupBy(r => r.IdBudgetLine)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var remaniement in remaniements)
            {
                if (remaniement.BudgetLine != null &&
                    remaniementsGrouped.TryGetValue(remaniement.IdBudgetLine, out var relatedRemaniements))
                {
                    // Créer une nouvelle collection pour éviter les problèmes de tracking
                    remaniement.BudgetLine.Remaniements = relatedRemaniements;
                }
            }

            return remaniements;
        }

        /// <summary>
        /// Récupère toutes les BudgetLines sans enfants (lignes terminales)
        /// </summary>
        public async Task<List<BudgetLine>> GetBudgetLinesSansEnfantsAsync()
        {
            // Récupérer toutes les nomenclatures sans enfants
            var nomenclaturesSansEnfants = await _context.Nommenclatures
                .Where(n => !_context.Nommenclatures.Any(child => child.ParentId == n.Id))
                .Select(n => n.Id)
                .ToListAsync();

            // Récupérer les BudgetLines correspondantes
            return await _context.BudgetLines
                .Include(bl => bl.Nommenclature)
                .Include(bl => bl.BudgetPrimitif)
                    .ThenInclude(bp => bp.Exercice)
                .Where(bl => nomenclaturesSansEnfants.Contains(bl.NommenclatureId))
                .AsNoTracking()
                .OrderBy(bl => bl.Nommenclature.Chapitre)
                    .ThenBy(bl => bl.Nommenclature.Article)
                    .ThenBy(bl => bl.Nommenclature.Paragraphe)
                .ToListAsync();
        }

        /// <summary>
        /// Crée un remaniement et met à jour les montants des BudgetLines
        /// </summary>
        public async Task<(bool Success, string Message, Remaniement? Remaniement)> CreateRemaniementAsync(Remaniement remaniement, TypeRemaniement typeRemaniement)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validation
                if (remaniement.Montant == 0)
                {
                    return (false, "Le montant ne peut pas être zéro.", null);
                }

                if (string.IsNullOrWhiteSpace(remaniement.Motif))
                {
                    return (false, "Le motif est obligatoire.", null);
                }

                // Récupérer le BudgetLine concerné
                var budgetLine = await _context.BudgetLines
                    .Include(bl => bl.Nommenclature)
                    .FirstOrDefaultAsync(bl => bl.Id == remaniement.IdBudgetLine);

                if (budgetLine == null)
                {
                    return (false, "Ligne budgétaire introuvable.", null);
                }

                // ✅ APPLIQUER LE REMANIEMENT
                remaniement.TypeRemaniement = typeRemaniement;

                // Vérifier que le montant définitif ne deviendra pas négatif
                var montantActuelRemaniements = await _context.Remaniements
                    .Where(r => r.IdBudgetLine == budgetLine.Id)
                    .SumAsync(r => (double)(r.TypeRemaniement == TypeRemaniement.en_plus ? r.Montant : -r.Montant));

                var montantDefinitif = budgetLine.MontantPrevu + montantActuelRemaniements + 
                    (typeRemaniement == TypeRemaniement.en_plus ? remaniement.Montant : -remaniement.Montant);

                if (montantDefinitif < 0)
                {
                    return (false, $"Le montant définitif deviendrait négatif ({montantDefinitif:N0}). Opération annulée.", null);
                }

                // ✅ REMANIER LES PARENTS
                await RemanierBudgetParentsAsync(budgetLine.NommenclatureId, remaniement.Montant, budgetLine.BudgetPrimitifId, typeRemaniement);

                // Sauvegarder le remaniement
                _context.Remaniements.Add(remaniement);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return (true, "Remaniement effectué avec succès. Les budgets parents ont été mis à jour.", remaniement);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors du remaniement : {ex.Message}", null);
            }
        }

        /// <summary>
        /// Remanie récursivement tous les budgets parents
        /// </summary>
        private async Task RemanierBudgetParentsAsync(int nomenclatureId, double montant, int budgetPrimitifId, TypeRemaniement typeRemaniement)
        {
            // Récupérer la nomenclature actuelle
            var nomenclature = await _context.Nommenclatures
                .FirstOrDefaultAsync(n => n.Id == nomenclatureId);

            if (nomenclature == null || !nomenclature.ParentId.HasValue)
                return; // Pas de parent, on s'arrête

            // Récupérer le BudgetLine du parent
            var parentBudgetLine = await _context.BudgetLines
                .FirstOrDefaultAsync(bl =>
                    bl.NommenclatureId == nomenclature.ParentId.Value &&
                    bl.BudgetPrimitifId == budgetPrimitifId);

            if (parentBudgetLine != null)
            {
                // Créer un remaniement pour le parent
                var remaniementParent = new Remaniement
                {
                    IdBudgetLine = parentBudgetLine.Id,
                    Montant = montant,
                    TypeRemaniement = typeRemaniement,
                    Motif = "Remaniement automatique (impact du remaniement d'une ligne enfant)",
                    Date = DateTime.Now
                };

                _context.Remaniements.Add(remaniementParent);

                // Remanier récursivement le grand-parent
                await RemanierBudgetParentsAsync(parentBudgetLine.NommenclatureId, montant, budgetPrimitifId, typeRemaniement);
            }
        }

        /// <summary>
        /// Supprime un remaniement (⚠️ Ne modifie pas les montants - à implémenter si nécessaire)
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteRemaniementAsync(int id)
        {
            try
            {
                var remaniement = await _context.Remaniements.FindAsync(id);

                if (remaniement == null)
                {
                    return (false, "Remaniement introuvable.");
                }

                // ⚠️ ATTENTION : La suppression ne reverse pas le remaniement
                // Si vous voulez inverser le remaniement, il faut implémenter la logique inverse

                _context.Remaniements.Remove(remaniement);
                await _context.SaveChangesAsync();

                return (true, "Remaniement supprimé. ⚠️ Les montants n'ont pas été inversés.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère les remaniements d'une BudgetLine spécifique
        /// </summary>
        public async Task<List<Remaniement>> GetRemaniementsByBudgetLineAsync(int budgetLineId)
        {
            return await _context.Remaniements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Where(r => r.IdBudgetLine == budgetLineId)
                .OrderByDescending(r => r.Date)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Calcule le total des remaniements pour une BudgetLine
        /// </summary>
        public async Task<double> GetTotalRemaniementsAsync(int budgetLineId)
        {
            return await _context.Remaniements
                .Where(r => r.IdBudgetLine == budgetLineId)
                .SumAsync(r => (double)(r.TypeRemaniement == TypeRemaniement.en_plus ? r.Montant : -r.Montant));
        }

        /// <summary>
        /// Calcule le montant définitif d'une ligne budgétaire (MontantPrevu + RemaniementPlus - RemaniementMoins)
        /// </summary>
        public async Task<double> CalculerMontantDefinitifAsync(int budgetLineId)
        {
            var budgetLine = await _context.BudgetLines
                .Include(bl => bl.Remaniements)
                .FirstOrDefaultAsync(bl => bl.Id == budgetLineId);

            if (budgetLine == null)
                return 0;

            var remaniementPlus = budgetLine.Remaniements
                .Where(r => r.TypeRemaniement == TypeRemaniement.en_plus)
                .Sum(r => r.Montant);

            var remaniementMoins = budgetLine.Remaniements
                .Where(r => r.TypeRemaniement == TypeRemaniement.en_moins)
                .Sum(r => r.Montant);

            return budgetLine.MontantPrevu + remaniementPlus - remaniementMoins;
        }
    }
}