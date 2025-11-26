using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class BudgetPrimitifService
    {
        //private readonly AppDbContext _context;
        //public BudgetPrimitifService(AppDbContext context)
        //{
        //    _context = context;
        //}
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }
        //recuper tous les exercices
        public async Task<List<Exercice>> GetAllExercie()
        {
            using var context = CreateContext();

            return await context.Exercices
                .OrderByDescending(e => e.DateFin)
                .ToListAsync();
        }
        // recuperer tous les BudgetPrimitif
        public async Task<List<BudgetPrimitif>> GetAllBudgetPrimitifAsync()
        {
            try
            {
                using var context = CreateContext();
                var exerciceService = ExerciceService.Instance;

                if (exerciceService.CurrentExercice == null)
                {
                    throw new InvalidOperationException("Aucun exercice n'est sélectionné.");
                }
                //var exerciceId = Properties.Settings.Default.ExerciceId;

                return await context.BudgetsPrimitifs
                    .Include(e => e.Exercice)
                    .Where(e => e.ExerciceId ==exerciceService.CurrentExercice.Id)
                    //.OrderByDescending(e => e.DateCreation) // Optionnel : tri
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Logger l'erreur si vous avez un système de logging
                throw new Exception($"Erreur lors de la récupération des budgets primitifs : {ex.Message}", ex);
            }
        }

        // ajouter un budgetprimitif
        public async Task<(bool Success, string Message, BudgetPrimitif? BudgetPrimitif)> CreateBudgetPrimitifAsync(BudgetPrimitif budgePrimitif)
        {
            try
            {
                using var context = CreateContext();

                // Validation : Vérifier qu'il n'existe pas déjà un budget pour cet exercice
                var existe = await context.BudgetsPrimitifs
                    .AnyAsync(b => b.ExerciceId == budgePrimitif.ExerciceId);
                if (existe)
                {
                    return (false, $"un budget existe dejà pour cet exercice.", null);
                }
                context.BudgetsPrimitifs.Add(budgePrimitif);
                await context.SaveChangesAsync();
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
                using var context = CreateContext();
                var existingBudgetPrimitif = await context.BudgetsPrimitifs
                    .FirstOrDefaultAsync(b => b.Id == budgetPrimitif.Id);
                if (existingBudgetPrimitif == null)
                {
                    return (false, "Budget Primitif non trouvée.");
                }
                // Validation : Vérifier qu'il n'existe pas déjà un budget primitif pour cet exercice
                var existe = await context.BudgetsPrimitifs
                    .AnyAsync(b => b.ExerciceId == budgetPrimitif.Id && b.Id != budgetPrimitif.Id);
                if (existe)
                {
                    return (false, $"cet budget existe déjà.");
                }
                existingBudgetPrimitif.ExerciceId = budgetPrimitif.ExerciceId;
                existingBudgetPrimitif.Exercice = budgetPrimitif.Exercice;
                existingBudgetPrimitif.DateApprobation = budgetPrimitif.DateApprobation;
                existingBudgetPrimitif.DateValidation = budgetPrimitif.DateValidation;
                //existingBudgetPrimitif.Montant = budgetPrimitif.Montant;
                
                await context.SaveChangesAsync();
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
                using var context = CreateContext();

                var existingBudgetPrimif = await context.BudgetsPrimitifs
                    .FirstOrDefaultAsync(b => b.Id == budgetPrimitifId);
                if (existingBudgetPrimif == null)
                {
                    return (false, "Budget Primitif non trouvée.");
                }
                context.BudgetsPrimitifs.Remove(existingBudgetPrimif);
                await context.SaveChangesAsync();
                return (true, "Budget primitif supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression du budget : {ex.Message}");
            }
        }

        //valider le budget primitif
        /// <summary>
        /// Valide un budget primitif et enregistre la date de validation
        /// Un budget ne peut être validé qu'une seule fois
        /// </summary>
        public async Task<(bool Success, string Message)> ValiderBudgetPrimitif(
            int budgetPrimitifId,
            DateOnly dateValidation)
        {
            try
            {
                using var context = CreateContext();

                var budget = await context.BudgetsPrimitifs
                    .Include(b => b.Exercice)
                    .FirstOrDefaultAsync(b => b.Id == budgetPrimitifId);

                if (budget == null)
                    return (false, "❌ Budget primitif introuvable.");

                // Vérification 1 : Le budget !est déjà validé
                if (budget.Status == BudgetPrimitif.Statusbudget.VALIDATED)
                    return (false, "❌ Ce budget est déjà validé.");

                // Vérification 2 : La date de validation doit être >= date d'approbation
                if (dateValidation < budget.DateApprobation)
                {
                    return (false,
                        $"❌ La date de validation ({dateValidation:dd/MM/yyyy}) ne peut pas être antérieure " +
                        $"à la date d'approbation ({budget.DateApprobation:dd/MM/yyyy}).");
                }

                // Vérification 3 : La date de validation doit être dans l'exercice budgétaire
                if (budget.Exercice != null)
                {
                    if (dateValidation < budget.Exercice.DateDebut || dateValidation > budget.Exercice.DateFin)
                    {
                        return (false,
                            $"❌ La date de validation doit être comprise dans l'exercice budgétaire " +
                            $"({budget.Exercice.DateDebut:dd/MM/yyyy} - {budget.Exercice.DateFin:dd/MM/yyyy}).");
                    }
                }

                // Vérification 4 : Le budget doit avoir des lignes budgétaires
                var hasLines = await context.BudgetLines
                    .AnyAsync(bl => bl.BudgetPrimitifId == budgetPrimitifId);

                if (!hasLines)
                {
                    return (false,
                        "❌ Impossible de valider un budget sans lignes budgétaires. " +
                        "Veuillez d'abord ajouter des lignes au budget.");
                }

                // Validation du budget
                budget.Status = BudgetPrimitif.Statusbudget.VALIDATED;
                budget.DateValidation = dateValidation;

                await context.SaveChangesAsync();

                return (true,
                    $"✅ Budget primitif validé avec succès.\n\n" +
                    $"Exercice : {budget.Exercice?.Libelle ?? "N/A"}\n" +
                    $"Date de validation : {dateValidation:dd/MM/yyyy}\n" +
                    $"Montant total : {budget.MontantTotal:N0} GNF\n\n" +
                    $"⚠️ Le budget ne pourra plus être modifié.");
            }
            catch (Exception ex)
            {
                return (false, $"❌ Erreur lors de la validation : {ex.Message}");
            }
        }
    }
}
