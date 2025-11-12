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

        #region Récupération

        /// <summary>
        /// Récupère tous les bons de commande avec leurs relations
        /// </summary>
        public async Task<List<BonCommande>> GetAllBonCommandesAsync()
        {
            using var context = CreateContext();

            return await context.BonCommandes
                .Include(bc => bc.Engagement)
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
            using var context = CreateContext();

            return await context.BonCommandes
                .Include(bc => bc.Engagement)
                    .ThenInclude(e => e.Tiers)
                .Include(bc => bc.Engagement)
                    .ThenInclude(e => e.Exercice)
                .Include(bc => bc.Engagement)
                    .ThenInclude(e => e.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                .Include(bc => bc.Details)
                .AsNoTracking()
                .FirstOrDefaultAsync(bc => bc.Id == id);
        }

        /// <summary>
        /// Récupère les bons de commande par engagement
        /// </summary>
        public async Task<List<BonCommande>> GetBonCommandesByEngagementAsync(int engagementId)
        {
            using var context = CreateContext();

            return await context.BonCommandes
                .Include(bc => bc.Details)
                .Where(bc => bc.EngagementId == engagementId)
                .AsNoTracking()
                .OrderByDescending(bc => bc.DateCreation)
                .ToListAsync();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouveau bon de commande avec ses détails
        /// </summary>
        public async Task<(bool Success, string Message, BonCommande? BonCommande)> CreateBonCommandeAsync(
            BonCommande bonCommande,
            List<DetailBonCommande> details)
        {
            using var context = CreateContext();

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(bonCommande.Numero))
                    return (false, "Le numéro du bon de commande est obligatoire.", null);

                if (bonCommande.EngagementId <= 0)
                    return (false, "L'engagement est obligatoire.", null);

                if (details == null || !details.Any())
                    return (false, "Au moins un détail est obligatoire.", null);

                // Vérifier l'unicité du numéro
                var existingBon = await context.BonCommandes
                    .AnyAsync(bc => bc.Numero == bonCommande.Numero);

                if (existingBon)
                    return (false, $"Le numéro '{bonCommande.Numero}' existe déjà.", null);

                // Vérifier que l'engagement existe
                var engagement = await context.Engagements
                    .Include(e => e.Tiers)
                    .FirstOrDefaultAsync(e => e.Id == bonCommande.EngagementId);

                if (engagement == null)
                    return (false, "Engagement introuvable.", null);

                // Créer le bon de commande
                var newBonCommande = new BonCommande
                {
                    Numero = bonCommande.Numero.Trim(),
                    DateCreation = bonCommande.DateCreation,
                    EngagementId = bonCommande.EngagementId,
                    FichierJoin = bonCommande.FichierJoin
                };

                context.BonCommandes.Add(newBonCommande);
                await context.SaveChangesAsync();

                // ✅ Ajouter les détails et les lier automatiquement au BonCommande
                foreach (var detail in details)
                {
                    var newDetail = new DetailBonCommande
                    {
                        BonCommandeId = newBonCommande.Id, // ✅ Lien automatique
                        Designation = detail.Designation.Trim(),
                        Quantite = detail.Quantite,
                        PrixUnitaire = detail.PrixUnitaire
                    };

                    context.DetailsBonCommandes.Add(newDetail);
                }

                await context.SaveChangesAsync();

                // Recharger avec les relations
                var savedBonCommande = await GetBonCommandeByIdAsync(newBonCommande.Id);

                return (true, "✅ Bon de commande créé avec succès.", savedBonCommande);
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
        /// Met à jour un bon de commande et ses détails
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateBonCommandeAsync(
            BonCommande bonCommande,
            List<DetailBonCommande> details)
        {
            using var context = CreateContext();

            try
            {
                var existingBon = await context.BonCommandes
                    .Include(bc => bc.Details)
                    .FirstOrDefaultAsync(bc => bc.Id == bonCommande.Id);

                if (existingBon == null)
                    return (false, "Bon de commande introuvable.");

                // Vérifier l'unicité du numéro (sauf pour le bon actuel)
                var duplicateNumero = await context.BonCommandes
                    .AnyAsync(bc => bc.Numero == bonCommande.Numero && bc.Id != bonCommande.Id);

                if (duplicateNumero)
                    return (false, $"Le numéro '{bonCommande.Numero}' existe déjà.");

                // Mettre à jour le bon de commande
                existingBon.Numero = bonCommande.Numero.Trim();
                existingBon.DateCreation = bonCommande.DateCreation;
                existingBon.EngagementId = bonCommande.EngagementId;
                existingBon.FichierJoin = bonCommande.FichierJoin;

                // Supprimer les anciens détails
                context.DetailsBonCommandes.RemoveRange(existingBon.Details);

                // ✅ Ajouter les nouveaux détails avec lien automatique
                foreach (var detail in details)
                {
                    var newDetail = new DetailBonCommande
                    {
                        BonCommandeId = existingBon.Id, // ✅ Lien automatique
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

            try
            {
                var bonCommande = await context.BonCommandes
                    .Include(bc => bc.Details)
                    .FirstOrDefaultAsync(bc => bc.Id == id);

                if (bonCommande == null)
                    return (false, "Bon de commande introuvable.");

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