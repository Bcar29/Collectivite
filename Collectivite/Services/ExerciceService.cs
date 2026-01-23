using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class ExerciceService
    {
        private static ExerciceService? _instance;
        private Exercice? _currentExercice;

        public static ExerciceService Instance => _instance ??= new ExerciceService();

        private readonly AuditService _auditService = new AuditService();

        public Exercice? CurrentExercice
        {
            get => _currentExercice;
            set => _currentExercice = value;
        }

        public event EventHandler<Exercice>? ExerciceChanged;

        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Initialisation / Sélection

        public async Task InitializeCurrentExerciceAsync()
        {
            var exercices = await GetAllExerciceAsync();
            _currentExercice = exercices.FirstOrDefault(e => !e.EstCloture)
                               ?? exercices.LastOrDefault();
        }

        public void SetCurrentExercice(Exercice exercice)
        {
            _currentExercice = exercice;
            ExerciceChanged?.Invoke(this, exercice);
        }

        #endregion

        #region READ (sans audit)

        public async Task<List<Exercice>> GetAllExerciceAsync()
        {
            using var context = CreateContext();
            return await context.Exercices
                .OrderByDescending(e => e.Id)
                .ToListAsync();
        }

        public async Task<List<Exercice>> GetOpenExercicesAsync()
        {
            using var context = CreateContext();
            return await context.Exercices
                .Where(e => !e.EstCloture)
                .OrderByDescending(e => e.DateDebut)
                .ToListAsync();
        }

        public async Task<Exercice?> GetActiveExerciceAsync()
        {
            using var context = CreateContext();
            return await context.Exercices
                .Where(e => !e.EstCloture)
                .OrderByDescending(e => e.DateDebut)
                .FirstOrDefaultAsync();
        }

        public async Task<Exercice?> GetExerciceByIdAsync(int exerciceId)
        {
            using var context = CreateContext();
            return await context.Exercices.FirstOrDefaultAsync(e => e.Id == exerciceId);
        }

        public async Task<DetailCommune?> LastDetailCommune()
        {
            using var context = CreateContext();
            return await context.DetailCommunes
                .Where(d => d.Exercice == null)
                .OrderByDescending(d => d.Id)
                .FirstOrDefaultAsync();
        }

        #endregion

        #region CREATE (AUDIT)

        public async Task<(bool Success, string Message, Exercice? Exercice)>
            CreateAsync(Exercice exercice)
        {
            try
            {
                using var context = CreateContext();

                if (string.IsNullOrWhiteSpace(exercice.Libelle))
                    return (false, "Le libellé est obligatoire.", null);

                if (await context.Exercices.AnyAsync(e => e.Libelle == exercice.Libelle))
                    return (false, $"{exercice.Libelle} existe déjà.", null);

                if (exercice.DateFin <= exercice.DateDebut)
                    return (false, "La date de fin doit être après la date de début.", null);

                context.Exercices.Add(exercice);
                await context.SaveChangesAsync();

                // Création automatique du budget primitif
                var budgetPrimitif = new BudgetPrimitif
                {
                    ExerciceId = exercice.Id,
                    MontantTotal = 0,
                    MontantDepense = 0,
                    MontantRecette = 0,
                    Status = BudgetPrimitif.Statusbudget.DRAFT
                };

                context.BudgetsPrimitifs.Add(budgetPrimitif);
                await context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Création Exercice",
                    $"Création de l'exercice '{exercice.Libelle}' ({exercice.DateDebut:yyyy} - {exercice.DateFin:yyyy})",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, "Exercice créé avec succès.", exercice);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur création exercice : {ex.Message}", null);
            }
        }

        #endregion

        #region UPDATE (AUDIT)

        public async Task<(bool Success, string Message)> UpdateAsync(Exercice exercice)
        {
            try
            {
                using var context = CreateContext();

                var duplicate = await context.Exercices
                    .AnyAsync(e => e.Libelle == exercice.Libelle && e.Id != exercice.Id);

                if (duplicate)
                    return (false, $"{exercice.Libelle} existe déjà.");

                var existing = await context.Exercices.FindAsync(exercice.Id);
                if (existing == null)
                    return (false, "Exercice non trouvé.");

                if (exercice.DateFin <= exercice.DateDebut)
                    return (false, "La date de fin doit être après la date de début.");

                existing.Libelle = exercice.Libelle;
                existing.DateDebut = exercice.DateDebut;
                existing.DateFin = exercice.DateFin;
                existing.EstCloture = exercice.EstCloture;

                await context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Modification Exercice",
                    $"Modification de l'exercice '{existing.Libelle}'",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, "Exercice mis à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur mise à jour : {ex.Message}");
            }
        }

        #endregion

        #region DELETE (AUDIT)

        public async Task<(bool Success, string Message)> DeleteAsync(int exerciceId)
        {
            try
            {
                using var context = CreateContext();

                var exercice = await context.Exercices.FindAsync(exerciceId);
                if (exercice == null)
                    return (false, "Exercice non trouvé.");

                //var hasDependencies = await context.BudgetsPrimitifs
                //    .AnyAsync(b => b.ExerciceId == exerciceId);

                //if (hasDependencies)
                //    return (false, "Impossible de supprimer : dépendances existantes.");

                context.Exercices.Remove(exercice);
                await context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Suppression Exercice",
                    $"Suppression de l'exercice '{exercice.Libelle}'",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, "Exercice supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur suppression : {ex.Message}");
            }
        }

        #endregion

        #region CLÔTURE (AUDIT)

        public async Task<(bool Success, string Message)> CloturerAsync(int id)
        {
            try
            {
                using var context = CreateContext();

                var exercice = await context.Exercices.FindAsync(id);
                if (exercice == null)
                    return (false, "Exercice introuvable.");

                if (exercice.EstCloture)
                    return (false, "Exercice déjà clôturé.");

                exercice.EstCloture = true;
                await context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Clôture Exercice",
                    $"Clôture de l'exercice '{exercice.Libelle}'",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, $"Exercice '{exercice.Libelle}' clôturé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur clôture : {ex.Message}");
            }
        }

        #endregion
    }
}
