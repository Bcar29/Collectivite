using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    class BudgetPrimitifService
    {
        private readonly AppDbContext _context;
        public BudgetPrimitifService(AppDbContext context)
        {
            _context = context;
        }
        //recuper tous les exercices
        public async Task<List<Exercice>> GetAllExercie()
        {
            return await _context.Exercices
                .OrderByDescending(e => e.DateFin)
                .ToListAsync();
        }
        // recuperer tous les BudgetPrimitif
        public async Task<List<BudgetPrimitif>> GetAllBudgetPrimitifAsync()
        {
            return await _context.BudgetsPrimitifs
                .Include(e => e.Exercice)
                .ToListAsync();
        }

        // ajouter un budgetprimitif
        public async Task<(bool Success, string Message, BudgetPrimitif? BudgetPrimitif)> CreateBudgetPrimitifAsync(BudgetPrimitif budgePrimitif)
        {
            try
            {
                // Validation : Vérifier qu'il n'existe pas déjà un budget pour cet exercice
                var existe = await _context.BudgetsPrimitifs
                    .AnyAsync(b => b.ExerciceId == budgePrimitif.ExerciceId);
                if (existe)
                {
                    return (false, $"un budget existe dejà pour cet exercice.", null);
                }
                _context.BudgetsPrimitifs.Add(budgePrimitif);
                await _context.SaveChangesAsync();
                return (true, "budget créée avec succès.", budgePrimitif);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de l'ajout du budget : {ex.Message}", null);
            }
        }

        // mettre à jour un un budget
        public async Task<(bool Success, string Message)> UpdateBudgetPrimitifAsync(BudgetPrimitif budgetPrimitif)
        {
            try
            {
                var existingBudgetPrimitif = await _context.BudgetsPrimitifs
                    .FirstOrDefaultAsync(b => b.Id == budgetPrimitif.Id);
                if (existingBudgetPrimitif == null)
                {
                    return (false, "Budget Primitif non trouvée.");
                }
                // Validation : Vérifier qu'il n'existe pas déjà un budget primitif pour cet exercice
                var existe = await _context.BudgetsPrimitifs
                    .AnyAsync(b => b.ExerciceId == budgetPrimitif.Id && b.Id != budgetPrimitif.Id);
                if (existe)
                {
                    return (false, $"cet budget existe déjà.");
                }
                existingBudgetPrimitif.ExerciceId = budgetPrimitif.ExerciceId;
                existingBudgetPrimitif.Exercice = budgetPrimitif.Exercice;
                existingBudgetPrimitif.DateVote = budgetPrimitif.DateVote;
                existingBudgetPrimitif.Montant = budgetPrimitif.Montant;
                
                await _context.SaveChangesAsync();
                return (true, "budget mise à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour du budget : {ex.Message}");
            }
        }

        // supprimer un budget primitif
        public async Task<(bool Success, string Message)> DeleteBudgetPrimitifAsync(int budgetPrimitifId)
        {
            try
            {
                var existingBudgetPrimif = await _context.BudgetsPrimitifs
                    .FirstOrDefaultAsync(b => b.Id == budgetPrimitifId);
                if (existingBudgetPrimif == null)
                {
                    return (false, "Budget Primitif non trouvée.");
                }
                _context.BudgetsPrimitifs.Remove(existingBudgetPrimif);
                await _context.SaveChangesAsync();
                return (true, "Budget primitif supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression du budget : {ex.Message}");
            }
        }
    }
}
