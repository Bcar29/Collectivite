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

        #region Récupération

        /// <summary>
        /// Récupère toutes les factures avec leurs relations
        /// </summary>
        public async Task<List<Facture>> GetAllFacturesAsync()
        {
            if (!SessionManager.HasPermission("Facture.View"))
                throw new UnauthorizedAccessException("Permission Facture.View requise pour consulter les factures.");

            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<Facture>();
            }
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

        /// <summary>
        /// Récupère une facture par son ID
        /// </summary>
        public async Task<Facture?> GetFactureByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("Facture.View"))
                throw new UnauthorizedAccessException("Permission Facture.View requise pour consulter cette facture.");

            using var context = CreateContext();

            return await context.Factures
                .Include(f => f.Tiers)
                .Include(f => f.Exercice)
                .Include(f => f.Contrats)
                .Include(f => f.Details)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        /// <summary>
        /// Récupère les factures par statut
        /// </summary>
        public async Task<List<Facture>> GetFacturesByStatusAsync(StatusFact status)
        {
            if (!SessionManager.HasPermission("Facture.View"))
                throw new UnauthorizedAccessException("Permission Facture.View requise pour consulter les factures.");

            using var context = CreateContext();

            return await context.Factures
                .Include(f => f.Tiers)
                .Include(f => f.Exercice)
                .Include(f => f.Details)
                .Where(f => f.Status == status)
                .AsNoTracking()
                .OrderByDescending(f => f.DateFacture)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les factures d'un tiers
        /// </summary>
        public async Task<List<Facture>> GetFacturesByTiersAsync(int tiersId)
        {
            if (!SessionManager.HasPermission("Facture.View"))
                throw new UnauthorizedAccessException("Permission Facture.View requise pour consulter les factures.");

            using var context = CreateContext();

            return await context.Factures
                .Include(f => f.Tiers)
                .Include(f => f.Exercice)
                .Include(f => f.Details)
                .Where(f => f.TiersId == tiersId)
                .AsNoTracking()
                .OrderByDescending(f => f.DateFacture)
                .ToListAsync();
        }

        #endregion

        #region Numérotation

        /// <summary>
        /// Génère le prochain numéro de facture (format : F-YYYY-0001)
        /// </summary>
        public async Task<string> GenerateNextNumeroAsync()
        {
            using var context = CreateContext();

            var currentYear = DateTime.Now.Year;
            var prefix = $"F-{currentYear}-";

            var facturesThisYear = await context.Factures
                .Where(f => f.NumeroFacture.StartsWith(prefix))
                .OrderByDescending(f => f.NumeroFacture)
                .ToListAsync();

            if (!facturesThisYear.Any())
                return prefix + "0001";

            var lastNumero = facturesThisYear.First().NumeroFacture;
            var lastSequence = lastNumero.Substring(lastNumero.LastIndexOf('-') + 1);

            if (int.TryParse(lastSequence, out int seq))
            {
                return prefix + (seq + 1).ToString("D4");
            }

            return prefix + "0001";
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée une nouvelle facture avec ses détails
        /// </summary>
        public async Task<(bool Success, string Message, Facture? Facture)> CreateFactureAsync(
            Facture facture,
            List<DetailsFacture> details)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Facture.Create"))
                return (false, "Permission Facture.Create requise pour créer une facture.", null);

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(facture.NumeroFacture))
                    return (false, "Le numéro de facture est obligatoire.", null);

                if (facture.TiersId <= 0)
                    return (false, "Le tiers est obligatoire.", null);

                if (facture.ExerciceId <= 0)
                    return (false, "L'exercice est obligatoire.", null);

                if (details == null || !details.Any())
                    return (false, "Au moins un détail de facture est obligatoire.", null);

                // Vérifier l'unicité du numéro de facture
                var existingFacture = await context.Factures
                    .AnyAsync(f => f.NumeroFacture == facture.NumeroFacture);

                if (existingFacture)
                    return (false, $"Le numéro de facture '{facture.NumeroFacture}' existe déjà.", null);

                // Créer la facture
                var newFacture = new Facture
                {
                    NumeroFacture = facture.NumeroFacture.Trim(),
                    DateFacture = facture.DateFacture,
                    MontantHT = facture.MontantHT,
                    TauxTVA = facture.TauxTVA,
                    MontantTTC = facture.MontantTTC,
                    DateEcheance = facture.DateEcheance,
                    Description = facture.Description?.Trim() ?? "",
                    TiersId = facture.TiersId,
                    ExerciceId = facture.ExerciceId,
                    ContratId = facture.ContratId,
                    Status = facture.Status,
                    FichierJoin = facture.FichierJoin
                };

                context.Factures.Add(newFacture);
                await context.SaveChangesAsync();

                // Ajouter les détails
                foreach (var detail in details)
                {
                    var newDetail = new DetailsFacture
                    {
                        FactureId = newFacture.Id,
                        Libelle = detail.Libelle.Trim(),
                        Quantite = detail.Quantite,
                        PrixUnitaire = detail.PrixUnitaire,
                        MontantTotal = detail.MontantTotal
                    };

                    context.DetailsFactures.Add(newDetail);
                }

                await context.SaveChangesAsync();

                // Recharger avec les relations
                var savedFacture = await GetFactureByIdAsync(newFacture.Id);

                return (true, "✅ Facture créée avec succès.", savedFacture);
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
        /// Met à jour une facture et ses détails
        /// </summary>
        public async Task<(bool Success, string Message, Facture? fact)> UpdateFactureAsync(
            Facture facture,
            List<DetailsFacture> details)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Facture.Edit"))
                return (false, "Permission Facture.Edit requise pour modifier une facture.", null);

            try
            {
                var existingFacture = await context.Factures
                    .Include(f => f.Details)
                    .FirstOrDefaultAsync(f => f.Id == facture.Id);

                if (existingFacture == null)
                    return (false, "Facture introuvable.", null);

                // Vérifier l'unicité du numéro (sauf pour la facture actuelle)
                var duplicateNumero = await context.Factures
                    .AnyAsync(f => f.NumeroFacture == facture.NumeroFacture && f.Id != facture.Id);

                if (duplicateNumero)
                    return (false, $"Le numéro de facture '{facture.NumeroFacture}' existe déjà.", null);

                // Mettre à jour la facture
                existingFacture.NumeroFacture = facture.NumeroFacture.Trim();
                existingFacture.DateFacture = facture.DateFacture;
                existingFacture.MontantHT = facture.MontantHT;
                existingFacture.TauxTVA = facture.TauxTVA;
                existingFacture.MontantTTC = facture.MontantTTC;
                existingFacture.DateEcheance = facture.DateEcheance;
                existingFacture.Description = facture.Description?.Trim() ?? "";
                existingFacture.TiersId = facture.TiersId;
                existingFacture.ExerciceId = facture.ExerciceId;
                existingFacture.ContratId = facture.ContratId;
                existingFacture.Status = facture.Status;
                existingFacture.FichierJoin = facture.FichierJoin;

                // Supprimer les anciens détails
                context.DetailsFactures.RemoveRange(existingFacture.Details);

                // Ajouter les nouveaux détails
                foreach (var detail in details)
                {
                    var newDetail = new DetailsFacture
                    {
                        FactureId = existingFacture.Id,
                        Libelle = detail.Libelle.Trim(),
                        Quantite = detail.Quantite,
                        PrixUnitaire = detail.PrixUnitaire,
                        MontantTotal = detail.MontantTotal
                    };

                    context.DetailsFactures.Add(newDetail);
                }

                await context.SaveChangesAsync();

                return (true, "✅ Facture modifiée avec succès.", existingFacture);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}", null);
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime une facture et ses détails
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteFactureAsync(int id)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Facture.Delete"))
                return (false, "Permission Facture.Delete requise pour supprimer une facture.");

            try
            {
                var facture = await context.Factures
                    .Include(f => f.Details)
                    .Include(f => f.Engagements)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (facture == null)
                    return (false, "Facture introuvable.");

                // Vérifier si la facture a des engagements liés
                if (facture.Engagements != null && facture.Engagements.Any())
                {
                    return (false,
                        "Impossible de supprimer cette facture car elle est liée à des engagements.");
                }

                // Supprimer les détails
                context.DetailsFactures.RemoveRange(facture.Details);

                // Supprimer la facture
                context.Factures.Remove(facture);

                await context.SaveChangesAsync();

                return (true, "✅ Facture supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Changement de statut

        /// <summary>
        /// Change le statut d'une facture
        /// </summary>
        public async Task<(bool Success, string Message)> ChangeStatusAsync(int factureId, StatusFact newStatus)
        {
            using var context = CreateContext();

            try
            {
                var facture = await context.Factures.FindAsync(factureId);

                if (facture == null)
                    return (false, "Facture introuvable.");

                facture.Status = newStatus;
                await context.SaveChangesAsync();

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