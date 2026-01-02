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

        public async Task<string> GenerateNextNumeroAsync()
        {
            using var context = CreateContext();

            var exerciceService = ExerciceService.Instance;
            var annee = exerciceService.CurrentExercice?.GetAnnee() ?? DateTime.Now.Year;
            var prefix = $"BC-{annee}-";

            var lastBc = await context.BonCommandes
                .Where(bc => bc.Numero.StartsWith(prefix))
                .OrderByDescending(bc => bc.Numero)
                .FirstOrDefaultAsync();

            if (lastBc == null)
                return $"{prefix}0001";

            var lastSequence = lastBc.Numero.Split('-').Last();

            return int.TryParse(lastSequence, out int seq)
                ? $"{prefix}{(seq + 1):D4}"
                : $"{prefix}0001";
        }

        #endregion

        #region READ

        public async Task<List<BonCommande>> GetAllBonCommandesAsync()
        {
            if (!SessionManager.HasPermission("BonCommande.View"))
                throw new UnauthorizedAccessException("Permission BonCommande.View requise.");

            var exercice = ExerciceService.Instance.CurrentExercice;
            if (exercice == null)
                return new List<BonCommande>();

            using var context = CreateContext();

            var list = await context.BonCommandes
                .Where(bc => bc.ExpressionBesoin.ExerciceId == exercice.Id)
                .Include(bc => bc.ExpressionBesoin)
                .Include(bc => bc.Engagements)
                .Include(bc => bc.Details)
                .AsNoTracking()
                .OrderByDescending(bc => bc.DateCreation)
                .ToListAsync();



            return list;
        }

        public async Task<BonCommande?> GetBonCommandeByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("BonCommande.View"))
                throw new UnauthorizedAccessException("Permission BonCommande.View requise.");

            using var context = CreateContext();

            var bc = await context.BonCommandes
                .Include(b => b.ExpressionBesoin)
                    .ThenInclude(e => e.Exercice)
                .Include(b => b.Engagements)
                .Include(b => b.Details)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bc != null)
            {
                await AuditService.Instance.LogAsync(
                    "Consultation Bon de Commande",
                    $"BC consulté | ID: {bc.Id} | Numéro: {bc.Numero}",
                   SessionManager.CurrentUser?.Username ?? "SYSTEM");
            }

            return bc;
        }

        #endregion

        #region CREATE

        public async Task<(bool Success, string Message, BonCommande? BonCommande)>
            CreateBonCommandeAsync(
                BonCommande bonCommande,
                List<DetailBonCommande> details,
                List<int> engagementIds)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("BonCommande.Create"))
                return (false, "Permission BonCommande.Create requise.", null);

            try
            {
                if (bonCommande.ExpressionBesoinId <= 0 || details == null || !details.Any())
                    return (false, "Données invalides.", null);

                var numero = await GenerateNextNumeroAsync();

                var newBc = new BonCommande
                {
                    Numero = numero,
                    DateCreation = DateTime.Now,
                    ExpressionBesoinId = bonCommande.ExpressionBesoinId
                };

                context.BonCommandes.Add(newBc);
                await context.SaveChangesAsync();

                foreach (var d in details)
                {
                    context.DetailsBonCommandes.Add(new DetailBonCommande
                    {
                        BonCommandeId = newBc.Id,
                        Designation = d.Designation.Trim(),
                        Quantite = d.Quantite,
                        PrixUnitaire = d.PrixUnitaire
                    });
                }

                await context.SaveChangesAsync();

                var montant = details.Sum(d => d.Quantite * d.PrixUnitaire);

                await AuditService.Instance.LogAsync(
                    "Création Bon de Commande",
                    $"BC créé | ID: {newBc.Id} | Numéro: {numero} | Montant: {montant:N0}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, $"✅ Bon de commande {numero} créé.", newBc);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        #endregion

        #region UPDATE

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
                var bc = await context.BonCommandes
                    .Include(b => b.Details)
                    .Include(b => b.Engagements)
                    .FirstOrDefaultAsync(b => b.Id == bonCommande.Id);

                if (bc == null)
                    return (false, "Bon introuvable.");

                bc.DateCreation = bonCommande.DateCreation;
                bc.ExpressionBesoinId = bonCommande.ExpressionBesoinId;

                context.DetailsBonCommandes.RemoveRange(bc.Details);

                foreach (var d in details)
                {
                    context.DetailsBonCommandes.Add(new DetailBonCommande
                    {
                        BonCommandeId = bc.Id,
                        Designation = d.Designation,
                        Quantite = d.Quantite,
                        PrixUnitaire = d.PrixUnitaire
                    });
                }

                await context.SaveChangesAsync();

                await AuditService.Instance.LogAsync(
                    "Modification Bon de Commande",
                    $"BC modifié | ID: {bc.Id} | Numéro: {bc.Numero}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, "✅ Bon de commande modifié.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        #endregion

        #region DELETE

        public async Task<(bool Success, string Message)> DeleteBonCommandeAsync(int id)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("BonCommande.Delete"))
                return (false, "Permission BonCommande.Delete requise.");

            try
            {
                var bc = await context.BonCommandes
                    .Include(b => b.Details)
                    .Include(b => b.Engagements)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bc == null)
                    return (false, "Bon introuvable.");

                context.DetailsBonCommandes.RemoveRange(bc.Details);
                context.BonCommandes.Remove(bc);

                await context.SaveChangesAsync();

                await AuditService.Instance.LogAsync(
                    "Suppression Bon de Commande",
                    $"BC supprimé | ID: {bc.Id} | Numéro: {bc.Numero}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, "✅ Bon de commande supprimé.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
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
