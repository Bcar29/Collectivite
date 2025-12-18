using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectivite.Utils;

namespace Collectivite.Services
{
    public class RemaniementService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération des données

        /// <summary>
        /// Récupère tous les remaniements avec leurs relations
        /// </summary>
        public async Task<List<Remaniement>> GetAllRemaniementsAsync()
        {
            if (!SessionManager.HasPermission("Remaniement.View"))
                throw new UnauthorizedAccessException("Permission Remaniement.View requise pour consulter les remaniements.");

            using var context = CreateContext();

            // ✅ CORRECTION : NE PAS charger BudgetLine.Remaniements (cycle)
            return await context.Remaniements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .AsNoTracking()
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère toutes les lignes budgétaires appartenant à un budget primitif validé
        /// (avec leurs remaniements pour le calcul des totaux)
        /// </summary>
        public async Task<List<BudgetLine>> GetBudgetLinesForValidatedBudgetAsync()
        {
            if (!SessionManager.HasPermission("Remaniement.View"))
                throw new UnauthorizedAccessException("Permission Remaniement.View requise pour consulter les lignes budgétaires pour remaniements.");

            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<BudgetLine>();
            }

            return await context.BudgetLines
                .Where(bl => bl.BudgetPrimitif.Status == BudgetPrimitif.Statusbudget.VALIDATED && bl.BudgetPrimitif.ExerciceId == exerciceService.CurrentExercice.Id)
                .Include(bl => bl.Nommenclature)
                .ThenInclude(n => n.Enfants)
                .Include(bl => bl.Remaniements)
                .Include(bl => bl.BudgetPrimitif)
                .AsNoTracking()
                .OrderBy(bl => bl.Nommenclature.Chapitre)
                .ThenBy(bl => bl.Nommenclature.Article)
                .ThenBy(bl => bl.Nommenclature.Paragraphe)
                .ThenBy(bl => bl.Nommenclature.SousParagraphe)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un remaniement par son ID
        /// </summary>
        public async Task<Remaniement?> GetRemaniementByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("Remaniement.View"))
                throw new UnauthorizedAccessException("Permission Remaniement.View requise pour consulter les remaniements.");

            using var context = CreateContext();

            // ✅ CORRECTION : NE PAS charger BudgetLine.Remaniements (cycle)
            return await context.Remaniements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Récupère les remaniements par ligne budgétaire
        /// </summary>
        public async Task<List<Remaniement>> GetRemaniementsByBudgetLineAsync(int budgetLineId)
        {
            if (!SessionManager.HasPermission("Remaniement.View"))
                throw new UnauthorizedAccessException("Permission Remaniement.View requise pour consulter les remaniements.");

            using var context = CreateContext();

            return await context.Remaniements
                .Include(r => r.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Where(r => r.IdBudgetLine == budgetLineId)
                .AsNoTracking()
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère toutes les lignes budgétaires sans enfants (feuilles de l'arbre)
        /// </summary>
        public async Task<List<BudgetLine>> GetBudgetLinesSansEnfantsAsync()
        {
            using var context = CreateContext();

            // ✅ ICI on peut charger les Remaniements car on part de BudgetLine
            var allLines = await context.BudgetLines
                .Include(bl => bl.Nommenclature)
                .Include(bl => bl.Remaniements) // ✅ OK : pour calculer MontantDefinitif
                .AsNoTracking()
                .ToListAsync();

            // Récupérer tous les Nommenclatures avec leurs enfants
            var allNommenclatures = await context.Nommenclatures
                .Include(n => n.Enfants)
                .AsNoTracking()
                .ToListAsync();

            // Filtrer pour garder seulement celles qui n'ont pas d'enfants
            var nommenclaturesSansEnfants = allNommenclatures
                .Where(n => n.Enfants == null || !n.Enfants.Any())
                .Select(n => n.Id)
                .ToHashSet();

            // Retourner les BudgetLines correspondantes
            return allLines
                .Where(bl => nommenclaturesSansEnfants.Contains(bl.NommenclatureId))
                .OrderBy(bl => bl.Nommenclature.Chapitre)
                .ThenBy(bl => bl.Nommenclature.Article)
                .ThenBy(bl => bl.Nommenclature.Paragraphe)
                .ToList();
        }

        /// <summary>
        /// 🆕 Récupère toutes les lignes budgétaires parentes dans la hiérarchie
        /// </summary>
        private async Task<List<BudgetLine>> GetParentBudgetLinesAsync(AppDbContext context, int nommenclatureId, int budgetPrimitifId)
        {
            var parentLines = new List<BudgetLine>();

            // Charger la nomenclature avec son parent
            var currentNommenclature = await context.Nommenclatures
                .Include(n => n.Parent)
                .FirstOrDefaultAsync(n => n.Id == nommenclatureId);

            // Remonter la hiérarchie
            while (currentNommenclature?.Parent != null)
            {
                // Trouver la BudgetLine correspondant à ce parent
                var parentBudgetLine = await context.BudgetLines
                    .Include(bl => bl.Nommenclature)
                    .Include(bl => bl.Remaniements)
                    .FirstOrDefaultAsync(bl => 
                        bl.NommenclatureId == currentNommenclature.Parent.Id && 
                        bl.BudgetPrimitifId == budgetPrimitifId);

                if (parentBudgetLine != null)
                {
                    parentLines.Add(parentBudgetLine);
                }

                // Remonter au parent suivant
                currentNommenclature = await context.Nommenclatures
                    .Include(n => n.Parent)
                    .FirstOrDefaultAsync(n => n.Id == currentNommenclature.ParentId);
            }

            return parentLines;
        }

        #endregion

        #region Création

        public async Task<(bool Success, string Message, Remaniement? Remaniement)>
    CreateRemaniementAsync(Remaniement remaniement, TypeRemaniement type)
        {
            if (!SessionManager.HasPermission("Remaniement.Create"))
                return (false,
                    "Permission Remaniement.Create requise pour créer un remaniement.",
                    null);

            using var context = CreateContext();
            var strategy = context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                (bool Success, string Message, Remaniement? Remaniement) result;

                await using var transaction =
                    await context.Database.BeginTransactionAsync();

                try
                {
                    // 🔹 Validations
                    if (remaniement.IdBudgetLine <= 0)
                        return (false, "La ligne budgétaire est obligatoire.", null);

                    if (remaniement.Montant <= 0)
                        return (false, "Le montant doit être supérieur à zéro.", null);

                    if (string.IsNullOrWhiteSpace(remaniement.Motif))
                        return (false, "Le motif est obligatoire.", null);

                    // 🔹 Charger la ligne budgétaire
                    var budgetLine = await context.BudgetLines
                        .Include(bl => bl.Nommenclature)
                        .Include(bl => bl.Remaniements)
                        .Include(bl => bl.BudgetPrimitif)
                        .FirstOrDefaultAsync(bl => bl.Id == remaniement.IdBudgetLine);

                    if (budgetLine == null)
                        return (false, "Ligne budgétaire introuvable.", null);

                    // 🔹 Vérifier que c’est une feuille (pas d’enfants)
                    var hasChildren = await context.Nommenclatures
                        .AnyAsync(n => n.ParentId == budgetLine.NommenclatureId);

                    if (hasChildren)
                        return (false,
                            "Impossible de créer un remaniement sur une ligne avec des sous-lignes.",
                            null);

                    // 🔹 Validation remaniement en moins
                    if (type == TypeRemaniement.en_moins)
                    {
                        var montantDefinitifActuel = budgetLine.MontantDefinitif;
                        var nouveauMontant =
                            montantDefinitifActuel - (decimal)remaniement.Montant;

                        if (nouveauMontant < 0)
                        {
                            return (false,
                                $"⚠️ Impossible : le remaniement rendrait le montant négatif.\n\n" +
                                $"Montant définitif actuel : {montantDefinitifActuel:N0} GNF\n" +
                                $"Remaniement demandé : -{remaniement.Montant:N0} GNF\n" +
                                $"Montant résultant : {nouveauMontant:N0} GNF ❌",
                                null);
                        }
                    }

                    // 🔹 Récupérer les lignes parentes
                    var parentBudgetLines = await GetParentBudgetLinesAsync(
                        context,
                        budgetLine.NommenclatureId,
                        budgetLine.BudgetPrimitifId);

                    // 1️⃣ Remaniement enfant
                    var newRemaniement = new Remaniement
                    {
                        IdBudgetLine = remaniement.IdBudgetLine,
                        Date = remaniement.Date,
                        Montant = remaniement.Montant,
                        Motif = remaniement.Motif.Trim(),
                        TypeRemaniement = type
                    };

                    context.Remaniements.Add(newRemaniement);
                    await context.SaveChangesAsync();

                    var remaniementsCreated = new List<string>
            {
                $"✅ Ligne enfant : {budgetLine.Nommenclature.Intitule} ({remaniement.Montant:N0} GNF)"
            };

                    // 2️⃣ Remaniements parents
                    foreach (var parentLine in parentBudgetLines)
                    {
                        var parentRemaniement = new Remaniement
                        {
                            IdBudgetLine = parentLine.Id,
                            Date = remaniement.Date,
                            Montant = remaniement.Montant,
                            Motif = $"[Propagation] {remaniement.Motif.Trim()}",
                            TypeRemaniement = type
                        };

                        context.Remaniements.Add(parentRemaniement);

                        remaniementsCreated.Add(
                            $"✅ Ligne parente : {parentLine.Nommenclature.Intitule} ({remaniement.Montant:N0} GNF)");
                    }

                    await context.SaveChangesAsync();

                    // 3️⃣ Mise à jour MontantActu enfant
                    budgetLine.UpdateMontantActu();

                    // 4️⃣ Mise à jour MontantActu parents
                    foreach (var parentLine in parentBudgetLines)
                    {
                        var parentToUpdate = await context.BudgetLines
                            .Include(bl => bl.Remaniements)
                            .FirstOrDefaultAsync(bl => bl.Id == parentLine.Id);

                        parentToUpdate?.UpdateMontantActu();
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 🔹 Recharger sans cycle
                    var savedRemaniement =
                        await GetRemaniementByIdAsync(newRemaniement.Id);

                    var typeText =
                        type == TypeRemaniement.en_plus ? "augmentation" : "diminution";

                    var message =
                        $"✅ Remaniement créé avec succès ({typeText} de {remaniement.Montant:N0} GNF).\n\n" +
                        $"📊 Remaniements créés :\n" +
                        string.Join("\n", remaniementsCreated);

                    result = (true, message, savedRemaniement);
                    return result;
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                    return (false, $"Erreur base de données : {inner}", null);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Erreur : {ex.Message}", null);
                }
            });
        }

        #endregion

        #region Suppression

        public async Task<(bool Success, string Message)> DeleteRemaniementAsync(int id)
        {
            if (!SessionManager.HasPermission("Remaniement.Delete"))
                return (false, "Permission Remaniement.Delete requise pour supprimer un remaniement.");

            using var context = CreateContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var remaniement = await context.Remaniements
                    .Include(r => r.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (remaniement == null)
                    return (false, "Remaniement introuvable.");

                var budgetLineId = remaniement.IdBudgetLine;
                var budgetLine = remaniement.BudgetLine;

                // 🆕 Récupérer les lignes parentes
                var parentBudgetLines = await GetParentBudgetLinesAsync(
                    context,
                    budgetLine.NommenclatureId,
                    budgetLine.BudgetPrimitifId);

                // Supprimer le remaniement principal
                context.Remaniements.Remove(remaniement);

                // 🆕 Supprimer les remaniements correspondants sur les parents
                // (même date, même montant, même type, motif avec [Propagation])
                foreach (var parentLine in parentBudgetLines)
                {
                    var parentRemaniement = await context.Remaniements
                        .FirstOrDefaultAsync(r =>
                            r.IdBudgetLine == parentLine.Id &&
                            r.Date == remaniement.Date &&
                            r.Montant == remaniement.Montant &&
                            r.TypeRemaniement == remaniement.TypeRemaniement &&
                            r.Motif.StartsWith("[Propagation]"));

                    if (parentRemaniement != null)
                    {
                        context.Remaniements.Remove(parentRemaniement);
                    }
                }

                await context.SaveChangesAsync();

                // ✅ Mettre à jour MontantActu pour la ligne enfant
                var updatedBudgetLine = await context.BudgetLines
                    .Include(bl => bl.Remaniements)
                    .FirstOrDefaultAsync(bl => bl.Id == budgetLineId);

                if (updatedBudgetLine != null)
                {
                    updatedBudgetLine.UpdateMontantActu();
                }

                // 🆕 Mettre à jour MontantActu pour toutes les lignes parentes
                foreach (var parentLine in parentBudgetLines)
                {
                    var parentToUpdate = await context.BudgetLines
                        .Include(bl => bl.Remaniements)
                        .FirstOrDefaultAsync(bl => bl.Id == parentLine.Id);

                    if (parentToUpdate != null)
                    {
                        parentToUpdate.UpdateMontantActu();
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "✅ Remaniement supprimé avec succès (y compris les propagations aux parents).");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        #endregion

        #region Statistiques

        public async Task<RemaniementStatistiques> GetStatistiquesAsync()
        {
            using var context = CreateContext();

            var remaniements = await context.Remaniements
                .AsNoTracking()
                .ToListAsync();

            var totalEnPlus = remaniements
                .Where(r => r.TypeRemaniement == TypeRemaniement.en_plus)
                .Sum(r => r.Montant);

            var totalEnMoins = remaniements
                .Where(r => r.TypeRemaniement == TypeRemaniement.en_moins)
                .Sum(r => r.Montant);

            var countEnPlus = remaniements.Count(r => r.TypeRemaniement == TypeRemaniement.en_plus);
            var countEnMoins = remaniements.Count(r => r.TypeRemaniement == TypeRemaniement.en_moins);

            return new RemaniementStatistiques
            {
                TotalRemaniements = remaniements.Count,
                TotalEnPlus = totalEnPlus,
                TotalEnMoins = totalEnMoins,
                CountEnPlus = countEnPlus,
                CountEnMoins = countEnMoins,
                SoldeNet = totalEnPlus - totalEnMoins
            };
        }

        #endregion
    }

    public class RemaniementStatistiques
    {
        public int TotalRemaniements { get; set; }
        public double TotalEnPlus { get; set; }
        public double TotalEnMoins { get; set; }
        public int CountEnPlus { get; set; }
        public int CountEnMoins { get; set; }
        public double SoldeNet { get; set; }
    }
}