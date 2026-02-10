using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class FactureService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        private readonly AuditService _auditService = new AuditService();

        #region Récupération

        public async Task<List<Facture>> GetAllFacturesAsync()
        {
            if (!SessionManager.HasPermission("Facture.View"))
                throw new UnauthorizedAccessException("Permission Facture.View requise pour consulter les factures.");

            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
                return new List<Facture>();

            return await context.Factures
                .Where(f => f.ExerciceId == exerciceService.CurrentExercice.Id)
                .Include(f => f.Tiers)
                .Include(f => f.Exercice)
                .Include(f => f.Contrats)
                .Include(f => f.Details)
                .AsNoTracking()
                .OrderByDescending(f => f.DateFacture)
                .ToListAsync();
        }

        public async Task<Facture?> GetFactureByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("Facture.View"))
                throw new UnauthorizedAccessException("Permission Facture.View requise.");

            using var context = CreateContext();

            return await context.Factures
                .Include(f => f.Tiers)
                .Include(f => f.Exercice)
                .Include(f => f.Contrats)
                .Include(f => f.Details)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        #endregion

        #region Numérotation

        public async Task<string> GenerateNextNumeroAsync()
        {
            using var context = CreateContext();

            // Utiliser l'exercice courant ou l'année actuelle par défaut
            var exerciceCourant = ExerciceService.Instance.CurrentExercice;
            var year = exerciceCourant?.GetAnnee() ?? DateTime.Now.Year;
            var prefix = $"F-{year}-";

            var factures = await context.Factures
                .Where(f => f.NumeroFacture.StartsWith(prefix))
                .OrderByDescending(f => f.NumeroFacture)
                .ToListAsync();

            if (!factures.Any())
                return prefix + "0001";

            var last = factures.First().NumeroFacture.Split('-').Last();

            return int.TryParse(last, out int seq)
                ? prefix + (seq + 1).ToString("D4")
                : prefix + "0001";
        }

        #endregion

        #region Création (AUDIT)

        public async Task<(bool Success, string Message, Facture? Facture)>
            CreateFactureAsync(Facture facture, List<DetailsFacture> details)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Facture.Create"))
                return (false, "Permission Facture.Create requise.", null);

            try
            {
                if (await context.Factures.AnyAsync(f => f.NumeroFacture == facture.NumeroFacture))
                    return (false, "Ce numéro de facture existe déjà.", null);

                context.Factures.Add(facture);
                await context.SaveChangesAsync();

                foreach (var d in details)
                {
                    d.FactureId = facture.Id;
                    context.DetailsFactures.Add(d);
                }

                await context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Création Facture",
                    $"Création facture N° {facture.NumeroFacture}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM"
                );

                return (true, "✅ Facture créée avec succès.", facture);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}", null);
            }
        }

        #endregion

        #region Modification (AUDIT)

        public async Task<(bool Success, string Message, Facture? fact)>
            UpdateFactureAsync(Facture facture, List<DetailsFacture> details)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Facture.Edit"))
                return (false, "Permission Facture.Edit requise.", null);

            try
            {
                var existing = await context.Factures
                    .Include(f => f.Details)
                    .FirstOrDefaultAsync(f => f.Id == facture.Id);

                if (existing == null)
                    return (false, "Facture introuvable.", null);

                existing.NumeroFacture = facture.NumeroFacture;
                existing.DateFacture = facture.DateFacture;
                existing.MontantHT = facture.MontantHT;
                existing.TauxTVA = facture.TauxTVA;
                existing.MontantTTC = facture.MontantTTC;
                existing.DateEcheance = facture.DateEcheance;
                existing.Description = facture.Description;
                existing.TiersId = facture.TiersId;
                existing.ExerciceId = facture.ExerciceId;
                existing.ContratId = facture.ContratId;
                existing.Status = facture.Status;
                existing.FichierJoin = facture.FichierJoin;

                context.DetailsFactures.RemoveRange(existing.Details);

                foreach (var d in details)
                {
                    d.FactureId = existing.Id;
                    context.DetailsFactures.Add(d);
                }

                await context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Modification Facture",
                    $"Modification facture N° {existing.NumeroFacture}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM"
                );

                return (true, "✅ Facture modifiée avec succès.", existing);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}", null);
            }
        }

        #endregion

        #region Suppression (AUDIT)

        public async Task<(bool Success, string Message)> DeleteFactureAsync(int id)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Facture.Delete"))
                return (false, "Permission Facture.Delete requise.");

            try
            {
                var facture = await context.Factures
                    .Include(f => f.Details)
                    .Include(f => f.Engagements)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (facture == null)
                    return (false, "Facture introuvable.");

                if (facture.Engagements != null && facture.Engagements.Any())
                    return (false, "Facture liée à des engagements.");

                context.DetailsFactures.RemoveRange(facture.Details);
                context.Factures.Remove(facture);
                await context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Suppression Facture",
                    $"Suppression facture N° {facture.NumeroFacture}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM"
                );

                return (true, "✅ Facture supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Changement de statut (AUDIT)

        public async Task<(bool Success, string Message)> ChangeStatusAsync(int factureId, StatusFact newStatus)
        {
            using var context = CreateContext();

            try
            {
                var facture = await context.Factures.FindAsync(factureId);

                if (facture == null)
                    return (false, "Facture introuvable.");

                var oldStatus = facture.Status;
                facture.Status = newStatus;

                await context.SaveChangesAsync();

                // 🔍 AUDIT
                await _auditService.LogAsync(
                    "Changement statut Facture",
                    $"Facture {facture.NumeroFacture} : {oldStatus} → {newStatus}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM"
                );

                return (true, $"✅ Statut changé en '{newStatus}'.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion
    }
}
