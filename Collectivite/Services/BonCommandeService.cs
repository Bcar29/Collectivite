using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class BonCommandeService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Génération de numéro

        /// <summary>
        /// Génère le prochain numéro de bon de commande (format: BC-YYYY-0001)
        /// </summary>
        public async Task<string> GenerateNextNumeroAsync()
        {
            using var context = CreateContext();

            var currentYear = DateTime.Now.Year;
            var prefix = $"BC-{currentYear}-";

            // Récupérer tous les bons de commande de l'année en cours
            var bonCommandesThisYear = await context.BonCommandes
                .Where(bc => bc.Numero.StartsWith(prefix))
                .OrderByDescending(bc => bc.Numero)
                .ToListAsync();

            if (!bonCommandesThisYear.Any())
            {
                return $"{prefix}0001";
            }

            // Extraire le dernier numéro et incrémenter
            var lastNumero = bonCommandesThisYear.First().Numero;
            var lastSequence = lastNumero.Substring(lastNumero.LastIndexOf('-') + 1);

            if (int.TryParse(lastSequence, out int sequence))
            {
                var nextSequence = sequence + 1;
                return $"{prefix}{nextSequence:D4}";
            }

            return $"{prefix}0001";
        }

        #endregion

        #region Récupération

        /// <summary>
        /// Récupère tous les bons de commande avec leurs relations
        /// </summary>
        public async Task<List<BonCommande>> GetAllBonCommandesAsync()
        {
            if (!SessionManager.HasPermission("BonCommande.View"))
                throw new UnauthorizedAccessException("Permission BonCommande.View requise.");

            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<BonCommande>();
            }

            using var context = CreateContext();

            return await context.BonCommandes
                .Include(bc => bc.ExpressionBesoin)
                    .ThenInclude(eb => eb.Exercice)
                .Include(bc => bc.Engagements)
                    .ThenInclude(e => e.Tiers)
                .Include(bc => bc.Details)
                .AsNoTracking()
                .OrderByDescending(bc => bc.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un bon de commande par son ID
        /// </summary>
        public async Task<BonCommande?> GetBonCommandeByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("BonCommande.View"))
                throw new UnauthorizedAccessException("Permission BonCommande.View requise.");

            using var context = CreateContext();

            return await context.BonCommandes
                .Include(bc => bc.ExpressionBesoin)
                    .ThenInclude(eb => eb.Exercice)
                .Include(bc => bc.Engagements)
                    .ThenInclude(e => e.Tiers)
                .Include(bc => bc.Engagements)
                    .ThenInclude(e => e.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                .Include(bc => bc.Details)
                .AsNoTracking()
                .FirstOrDefaultAsync(bc => bc.Id == id);
        }

        /// <summary>
        /// Récupère les bons de commande par expression de besoin
        /// </summary>
        public async Task<List<BonCommande>> GetBonCommandesByExpressionBesoinAsync(int expressionBesoinId)
        {
            if (!SessionManager.HasPermission("BonCommande.View"))
                throw new UnauthorizedAccessException("Permission BonCommande.View requise.");

            using var context = CreateContext();

            return await context.BonCommandes
                .Include(bc => bc.Details)
                .Include(bc => bc.Engagements)
                .Where(bc => bc.ExpressionBesoinId == expressionBesoinId)
                .AsNoTracking()
                .OrderByDescending(bc => bc.DateCreation)
                .ToListAsync();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouveau bon de commande avec ses détails et engagements
        /// </summary>
        public async Task<(bool Success, string Message, BonCommande? BonCommande)> CreateBonCommandeAsync(
            BonCommande bonCommande,
            List<DetailBonCommande> details,
            List<int> engagementIds)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("BonCommande.Create"))
                return (false, "Permission BonCommande.Create requise.", null);

            try
            {
                // Validation
                if (bonCommande.ExpressionBesoinId <= 0)
                    return (false, "L'expression de besoin est obligatoire.", null);

                if (details == null || !details.Any())
                    return (false, "Au moins un détail est obligatoire.", null);

                // Vérifier que l'expression de besoin existe
                var expressionBesoin = await context.ExpressionBesoins
                    .FirstOrDefaultAsync(eb => eb.Id == bonCommande.ExpressionBesoinId);

                if (expressionBesoin == null)
                    return (false, "Expression de besoin introuvable.", null);

                // Générer le numéro automatiquement
                var numero = await GenerateNextNumeroAsync();

                // Créer le bon de commande
                var newBonCommande = new BonCommande
                {
                    Numero = numero,
                    DateCreation = bonCommande.DateCreation,
                    ExpressionBesoinId = bonCommande.ExpressionBesoinId
                };

                context.BonCommandes.Add(newBonCommande);
                await context.SaveChangesAsync();

                // Ajouter les détails
                foreach (var detail in details)
                {
                    var newDetail = new DetailBonCommande
                    {
                        BonCommandeId = newBonCommande.Id,
                        Designation = detail.Designation.Trim(),
                        Quantite = detail.Quantite,
                        PrixUnitaire = detail.PrixUnitaire
                    };

                    context.DetailsBonCommandes.Add(newDetail);
                }

                await context.SaveChangesAsync();

                // Recharger avec les relations
                var savedBonCommande = await GetBonCommandeByIdAsync(newBonCommande.Id);

                return (true, $"✅ Bon de commande {numero} créé avec succès.", savedBonCommande);
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
        /// Met à jour un bon de commande, ses détails et engagements
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateBonCommandeAsync(
            BonCommande bonCommande,
            List<DetailBonCommande> details,
            List<int> engagementIds)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("BonCommande.Edit"))
                return (false, "Permission BonCommande.Edit requise.");

            try
            {
                var existingBon = await context.BonCommandes
                    .Include(bc => bc.Details)
                    .Include(bc => bc.Engagements)
                    .FirstOrDefaultAsync(bc => bc.Id == bonCommande.Id);

                if (existingBon == null)
                    return (false, "Bon de commande introuvable.");

                // Validation
                if (details == null || !details.Any())
                    return (false, "Au moins un détail est obligatoire.");

                if (engagementIds == null || !engagementIds.Any())
                    return (false, "Au moins un engagement est obligatoire.");

                // Vérifier que tous les engagements existent
                var engagements = await context.Engagements
                    .Where(e => engagementIds.Contains(e.Id))
                    .ToListAsync();

                if (engagements.Count != engagementIds.Count)
                    return (false, "Un ou plusieurs engagements sont introuvables.");

                // Mettre à jour le bon de commande
                existingBon.DateCreation = bonCommande.DateCreation;
                existingBon.ExpressionBesoinId = bonCommande.ExpressionBesoinId;

                // Délier les anciens engagements
                var oldEngagements = await context.Engagements
                    .Where(e => e.BonCommandeId == existingBon.Id)
                    .ToListAsync();

                foreach (var oldEngagement in oldEngagements)
                {
                    oldEngagement.BonCommandeId = null;
                }

                // Lier les nouveaux engagements
                foreach (var engagement in engagements)
                {
                    engagement.BonCommandeId = existingBon.Id;
                }

                // Supprimer les anciens détails
                context.DetailsBonCommandes.RemoveRange(existingBon.Details);

                // Ajouter les nouveaux détails
                foreach (var detail in details)
                {
                    var newDetail = new DetailBonCommande
                    {
                        BonCommandeId = existingBon.Id,
                        Designation = detail.Designation.Trim(),
                        Quantite = detail.Quantite,
                        PrixUnitaire = detail.PrixUnitaire
                    };

                    context.DetailsBonCommandes.Add(newDetail);
                }

                await context.SaveChangesAsync();

                return (true, "✅ Bon de commande modifié avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un bon de commande et ses détails
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteBonCommandeAsync(int id)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("BonCommande.Delete"))
                return (false, "Permission BonCommande.Delete requise.");

            try
            {
                var bonCommande = await context.BonCommandes
                    .Include(bc => bc.Details)
                    .Include(bc => bc.Engagements)
                    .FirstOrDefaultAsync(bc => bc.Id == id);

                if (bonCommande == null)
                    return (false, "Bon de commande introuvable.");

                // Délier les engagements
                foreach (var engagement in bonCommande.Engagements)
                {
                    engagement.BonCommandeId = null;
                }

                // Supprimer les détails
                context.DetailsBonCommandes.RemoveRange(bonCommande.Details);

                // Supprimer le bon de commande
                context.BonCommandes.Remove(bonCommande);

                await context.SaveChangesAsync();

                return (true, "✅ Bon de commande supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Statistiques

        /// <summary>
        /// Calcule le montant total d'un bon de commande
        /// </summary>
        public async Task<double> GetMontantTotalAsync(int bonCommandeId)
        {
            using var context = CreateContext();

            var details = await context.DetailsBonCommandes
                .Where(d => d.BonCommandeId == bonCommandeId)
                .AsNoTracking()
                .ToListAsync();

            return details.Sum(d => d.Total);
        }

        #endregion
    }
}