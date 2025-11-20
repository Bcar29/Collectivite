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
        /// Récupère un remaniement par son ID
        /// </summary>
        public async Task<Remaniement?> GetRemaniementByIdAsync(int id)
        {
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

        #endregion

        #region Création

        public async Task<(bool Success, string Message, Remaniement? Remaniement)> CreateRemaniementAsync(
            Remaniement remaniement,
            TypeRemaniement type)
        {
            using var context = CreateContext();

            try
            {
                // Validation
                if (remaniement.IdBudgetLine <= 0)
                    return (false, "La ligne budgétaire est obligatoire.", null);

                if (remaniement.Montant <= 0)
                    return (false, "Le montant doit être supérieur à zéro.", null);

                if (string.IsNullOrWhiteSpace(remaniement.Motif))
                    return (false, "Le motif est obligatoire.", null);

                // Vérifier que la ligne budgétaire existe
                var budgetLine = await context.BudgetLines
                    .Include(bl => bl.Nommenclature)
                    .Include(bl => bl.Remaniements) // ✅ Pour calculer MontantDefinitif
                    .FirstOrDefaultAsync(bl => bl.Id == remaniement.IdBudgetLine);

                if (budgetLine == null)
                    return (false, "Ligne budgétaire introuvable.", null);

                // Vérifier que c'est bien une ligne sans enfants
                var hasChildren = await context.Nommenclatures
                    .AnyAsync(n => n.ParentId == budgetLine.NommenclatureId);

                if (hasChildren)
                    return (false, "Impossible de créer un remaniement sur une ligne avec des sous-lignes.", null);

                // ✅ Validation : Vérifier que le remaniement en moins ne rend pas le montant négatif
                if (type == TypeRemaniement.en_moins)
                {
                    var montantDefinitifActuel = budgetLine.MontantDefinitif;
                    var nouveauMontant = montantDefinitifActuel - (decimal)remaniement.Montant;

                    if (nouveauMontant < 0)
                    {
                        return (false,
                            $"⚠️ Impossible : le remaniement en moins rendrait le montant définitif négatif.\n\n" +
                            $"Montant définitif actuel : {montantDefinitifActuel:N0} GNF\n" +
                            $"Remaniement demandé : -{remaniement.Montant:N0} GNF\n" +
                            $"Montant résultant : {nouveauMontant:N0} GNF (NÉGATIF ❌)",
                            null);
                    }
                }

                // ✅ Créer un nouvel objet sans navigation
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

                // ✅ Mettre à jour MontantActu
                var updatedBudgetLine = await context.BudgetLines
                    .Include(bl => bl.Remaniements)
                    .FirstOrDefaultAsync(bl => bl.Id == remaniement.IdBudgetLine);

                if (updatedBudgetLine != null)
                {
                    updatedBudgetLine.UpdateMontantActu();
                    await context.SaveChangesAsync();
                }

                // ✅ Recharger SANS cycle
                var savedRemaniement = await GetRemaniementByIdAsync(newRemaniement.Id);

                var typeText = type == TypeRemaniement.en_plus ? "augmentation" : "diminution";
                return (true,
                    $"✅ Remaniement créé avec succès ({typeText} de {remaniement.Montant:N0} GNF).",
                    savedRemaniement);
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

        #region Suppression

        public async Task<(bool Success, string Message)> DeleteRemaniementAsync(int id)
        {
            using var context = CreateContext();

            try
            {
                var remaniement = await context.Remaniements
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (remaniement == null)
                    return (false, "Remaniement introuvable.");

                var budgetLineId = remaniement.IdBudgetLine;

                // Supprimer le remaniement
                context.Remaniements.Remove(remaniement);
                await context.SaveChangesAsync();

                // ✅ Mettre à jour MontantActu
                var updatedBudgetLine = await context.BudgetLines
                    .Include(bl => bl.Remaniements)
                    .FirstOrDefaultAsync(bl => bl.Id == budgetLineId);

                if (updatedBudgetLine != null)
                {
                    updatedBudgetLine.UpdateMontantActu();
                    await context.SaveChangesAsync();
                }

                return (true, "✅ Remaniement supprimé avec succès.");
            }
            catch (Exception ex)
            {
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