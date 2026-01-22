using Collectivite.Models;
using Collectivite.Utils;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.Services
{
    public class MandatService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération
        #region Génération de numéro

        /// <summary>
        /// Génère le prochain numéro de mandat (format: M-YYYY-0001)
        /// </summary>
        public async Task<string> GenerateNextNumeroAsync()
        {
            using var context = CreateContext();

            var exerciceService = ExerciceService.Instance;
            var year = exerciceService.CurrentExercice?.GetAnnee() ?? DateTime.Now.Year;
            var prefix = $"M-{year}-";

            // Récupérer tous les mandats de l'année en cours
            var mandatsThisYear = await context.Mandats
                .Where(m => m.NumeroMandat.StartsWith(prefix))
                .OrderByDescending(m => m.NumeroMandat)
                .ToListAsync();

            if (!mandatsThisYear.Any())
            {
                return $"{prefix}0001";
            }

            // Extraire le dernier numéro et incrémenter
            var lastNumero = mandatsThisYear.First().NumeroMandat;
            var lastSequence = lastNumero.Substring(lastNumero.LastIndexOf('-') + 1);

            if (int.TryParse(lastSequence, out int sequence))
            {
                var nextSequence = sequence + 1;
                return $"{prefix}{nextSequence:D4}";
            }

            return $"{prefix}0001";
        }

        #endregion

        /// <summary>
        /// Récupère la ligne budgétaire d'un engagement
        /// </summary>
        public async Task<BudgetLine?> GetBudgetLineByEngagementIdAsync(int engagementId)
        {
            using var context = CreateContext();

            var engagement = await context.Engagements
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Remaniements)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == engagementId);

            return engagement?.BudgetLine;
        }

        /// <summary>
        /// Récupère tous les mandats avec leurs relations
        /// </summary>
        public async Task<List<Mandat>> GetAllMandatsAsync()
        {
            if (!SessionManager.HasPermission("Mandat.View"))
                throw new UnauthorizedAccessException("Permission Mandat.View requise pour consulter les mandats.");

            using var context = CreateContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<Mandat>();
            }

            return await context.Mandats
                .Where(m => m.Engagement.ExerciceId == exerciceService.CurrentExercice.Id)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Exercice)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Tiers)
                .Include(m => m.EcritureComptables)
                .AsNoTracking()
                .OrderByDescending(m => m.DateEmission)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un mandat par son ID
        /// </summary>
        public async Task<Mandat?> GetMandatByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("Mandat.View"))
                throw new UnauthorizedAccessException("Permission Mandat.View requise pour consulter ce mandat.");

            using var context = CreateContext();

            return await context.Mandats
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Exercice)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Tiers)
                .Include(m => m.EcritureComptables)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Récupère un mandat avec pièces jointes liées (engagement, facture, bon de commande)
        /// </summary>
        public async Task<Mandat?> GetMandatWithPiecesAsync(int id)
        {
            if (!SessionManager.HasPermission("Mandat.View"))
                throw new UnauthorizedAccessException("Permission Mandat.View requise pour consulter ce mandat.");

            using var context = CreateContext();

            return await context.Mandats
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Facture)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.BonCommande)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Récupère les mandats par engagement
        /// </summary>
        public async Task<List<Mandat>> GetMandatsByEngagementAsync(int engagementId)
        {
            if (!SessionManager.HasPermission("Mandat.View"))
                throw new UnauthorizedAccessException("Permission Mandat.View requise pour consulter les mandats.");

            using var context = CreateContext();

            return await context.Mandats
                .Include(m => m.Engagement)
                .Where(m => m.EngagementId == engagementId)
                .AsNoTracking()
                .OrderByDescending(m => m.DateEmission)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les mandats filtrés
        /// </summary>
        public async Task<List<Mandat>> GetMandatsFilteredAsync(
            string? numeroMandat = null,
            string? bordereau = null,
            TypeMois? mois = null,
            int? engagementId = null,
            decimal? montantMin = null,
            decimal? montantMax = null,
            DateTime? dateEmissionDebut = null,
            DateTime? dateEmissionFin = null,
            bool? estPaye = null)
        {
            if (!SessionManager.HasPermission("Mandat.View"))
                throw new UnauthorizedAccessException("Permission Mandat.View requise pour consulter les mandats.");

            using var context = CreateContext();

            var query = context.Mandats
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Exercice)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Tiers)
                .AsQueryable();

            // Filtre par numéro de mandat
            if (!string.IsNullOrWhiteSpace(numeroMandat))
            {
                query = query.Where(m => m.NumeroMandat.Contains(numeroMandat));
            }

            // Filtre par bordereau
            if (!string.IsNullOrWhiteSpace(bordereau))
            {
                query = query.Where(m => m.Bordereau != null && m.Bordereau.Contains(bordereau));
            }

            // Filtre par mois
            if (mois.HasValue)
            {
                query = query.Where(m => m.Mois == mois.Value);
            }

            // Filtre par engagement
            if (engagementId.HasValue)
            {
                query = query.Where(m => m.EngagementId == engagementId.Value);
            }

            // Filtre par montant
            if (montantMin.HasValue)
            {
                query = query.Where(m => m.MontantNet >= montantMin.Value);
            }
            if (montantMax.HasValue)
            {
                query = query.Where(m => m.MontantNet <= montantMax.Value);
            }

            // Filtre par date d'émission
            if (dateEmissionDebut.HasValue)
            {
                query = query.Where(m => m.DateEmission >= dateEmissionDebut.Value);
            }
            if (dateEmissionFin.HasValue)
            {
                query = query.Where(m => m.DateEmission <= dateEmissionFin.Value);
            }

            // Filtre par statut de paiement
            if (estPaye.HasValue)
            {
                if (estPaye.Value)
                {
                    query = query.Where(m => m.DatePaiement != null);
                }
                else
                {
                    query = query.Where(m => m.DatePaiement == null);
                }
            }

            return await query
                .AsNoTracking()
                .OrderByDescending(m => m.DateEmission)
                .ToListAsync();
        }

        #endregion

        #region Création

        /// <summary>
        /// Crée un nouveau mandat
        /// </summary>
        public async Task<(bool Success, string Message, Mandat? Mandat)> CreateMandatAsync(Mandat mandat)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Mandat.Create"))
                return (false, "Permission Mandat.Create requise pour créer un mandat.", null);

            try
            {
                // -----------------------
                // 1️⃣ VALIDATIONS SIMPLES
                // -----------------------
                if (string.IsNullOrWhiteSpace(mandat.NumeroMandat))
                    return (false, "Le numéro du mandat est obligatoire.", null);

                if (mandat.EngagementId <= 0)
                    return (false, "L'engagement est obligatoire.", null);

                if (mandat.MontantBrut <= 0)
                    return (false, "Le montant brut doit être supérieur à zéro.", null);

                if (mandat.MontantNet <= 0)
                    return (false, "Le montant net doit être supérieur à zéro.", null);

                if (string.IsNullOrWhiteSpace(mandat.MontantLettre))
                    return (false, "Le montant en lettres est obligatoire.", null);

                if (string.IsNullOrWhiteSpace(mandat.Objet))
                    return (false, "L'objet du mandat est obligatoire.", null);
                var exerciceService = ExerciceService.Instance;
                if (exerciceService.CurrentExercice != null)
                {
                    // 3. Recettes Perçues 
                    var RecettesPercues = await context.Mouvements
                        .Where(m => m.OrdreRecette != null && m.OrdreRecette.ExerciceId == exerciceService.CurrentExercice.Id)
                        .SumAsync(m => m.Montant);

                    // 3. Depense payées
                    var DepensesPayees = await context.Mouvements
                         .Where(m => m.Mandat != null && m.Mandat.Engagement.ExerciceId == exerciceService.CurrentExercice.Id)
                         .SumAsync(m => m.Montant);

                    var SoldeDisponible = RecettesPercues - DepensesPayees;

                    if (SoldeDisponible < mandat.MontantNet )
                    {
                        return (false, "Solde inferieur au montant du mandat ", null);
                    }
                }
                


                // -------------------------------
                // 2️⃣ VALIDATION : ENGAGEMENT EXISTE
                // -------------------------------
                var engagement = await context.Engagements.FindAsync(mandat.EngagementId);
                if (engagement == null)
                    return (false, "Engagement introuvable.", null);


                // ----------------------------------------------
                // 3️⃣ UNICITÉ DU NUMÉRO MANDAT (AVANT ENREGISTREMENT)
                // ----------------------------------------------
                var existingMandat = await context.Mandats
                    .FirstOrDefaultAsync(m => m.NumeroMandat == mandat.NumeroMandat);

                if (existingMandat != null)
                    return (false, $"Un mandat avec le numéro '{mandat.NumeroMandat}' existe déjà.", null);


                // ----------------------------------------------------------------
                // 4️⃣ VALIDATION : LIGNE BUDGETAIRE + NOMENCLATURE + COMPTE COMPTABLE
                // ----------------------------------------------------------------
                var budgetLine = await context.BudgetLines
                    .Where(bl => bl.Id == engagement.BudgetLineId)
                    .Select(bl => new { bl.Id, bl.Nommenclature.CodeNomenclature })
                    .FirstOrDefaultAsync();

                if (budgetLine == null)
                    return (false, "La ligne budgétaire spécifiée dans l'engagement n'existe pas.", null);

                if (string.IsNullOrWhiteSpace(budgetLine.CodeNomenclature))
                    return (false, "La ligne budgétaire spécifiée ne possède pas de nomenclature.", null);

                // 👉 Vérification du compte comptable AVANT de créer le mandat
                var compteComptableExists = await context.CompteComptables
                    .FirstOrDefaultAsync(cc => cc.NumeroCompte == budgetLine.CodeNomenclature);

                if (compteComptableExists == null || compteComptableExists.ContrePartieId == null)
                    return (false, $"Veuillez configurer '{budgetLine.CodeNomenclature}' dans le plan comptable.", null);


                // ----------------------------
                // 5️⃣ CREATION DU MANDAT (VALIDÉ)
                // ----------------------------
                var newMandat = new Mandat
                {
                    NumeroMandat = mandat.NumeroMandat,
                    Bordereau = mandat.Bordereau,
                    Mois = mandat.Mois,
                    EngagementId = mandat.EngagementId,
                    MontantBrut = mandat.MontantBrut,
                    Rts = mandat.Rts,
                    AutresPrecomptes = mandat.AutresPrecomptes,
                    MontantNet = mandat.MontantNet,
                    MontantLettre = mandat.MontantLettre,
                    DateEmission = mandat.DateEmission,
                    Objet = mandat.Objet,
                    FichierJoin = mandat.FichierJoin,
                    FichierName = mandat.FichierName,
                    DatePaiement = mandat.DatePaiement,
                    Etat = mandat.Etat
                };

                context.Mandats.Add(newMandat);
                await context.SaveChangesAsync();

                await AuditService.Instance.LogAsync(
                    "Nouveau Mandat",
                    $"mandat créé | ID: {mandat.Id} | Numero : {mandat.NumeroMandat} | Montant: {mandat.MontantNet:N0}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM");
                // -------------------------------
                // 7️⃣ RECHARGER LE MANDAT COMPLET
                // -------------------------------
                var savedMandat = await GetMandatByIdAsync(newMandat.Id);


                return (true, "✅ Mandat créé avec succès.", savedMandat);
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
        /// Met à jour un mandat
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateMandatAsync(Mandat mandat)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Mandat.Edit"))
                return (false, "Permission Mandat.Edit requise pour modifier un mandat.");

            try
            {
                var existingMandat = await context.Mandats.FindAsync(mandat.Id);

                if (existingMandat == null)
                    return (false, "Mandat introuvable.");

                // Vérifier l'unicité du numéro (sauf pour le mandat en cours)
                var duplicateMandat = await context.Mandats
                    .FirstOrDefaultAsync(m => m.NumeroMandat == mandat.NumeroMandat && m.Id != mandat.Id);

                if (duplicateMandat != null)
                    return (false, $"Un autre mandat avec le numéro '{mandat.NumeroMandat}' existe déjà.");

                // Mettre à jour
                existingMandat.NumeroMandat = mandat.NumeroMandat;
                existingMandat.Bordereau = mandat.Bordereau;
                existingMandat.Mois = mandat.Mois;
                existingMandat.EngagementId = mandat.EngagementId;
                existingMandat.MontantBrut = mandat.MontantBrut;
                existingMandat.Rts = mandat.Rts;
                existingMandat.AutresPrecomptes = mandat.AutresPrecomptes;
                existingMandat.MontantNet = mandat.MontantNet;
                existingMandat.MontantLettre = mandat.MontantLettre;
                existingMandat.DateEmission = mandat.DateEmission;
                existingMandat.Objet = mandat.Objet;
                existingMandat.FichierJoin = mandat.FichierJoin;
                existingMandat.FichierName = mandat.FichierName;
                existingMandat.DatePaiement = mandat.DatePaiement;
                existingMandat.Etat = mandat.Etat;

                await context.SaveChangesAsync();
                await AuditService.Instance.LogAsync(
                    "Modification Mandat",
                    $"mandat modifié | ID: {existingMandat.Id} | Numero : {existingMandat.NumeroMandat} | Montant: {existingMandat.MontantNet:N0}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM");
                return (true, "✅ Mandat modifié avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un mandat
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteMandatAsync(int id)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Mandat.Delete"))
                return (false, "Permission Mandat.Delete requise pour supprimer un mandat.");

            try
            {
                var mandat = await context.Mandats
                    .Include(m => m.EcritureComptables)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mandat == null)
                    return (false, "Mandat introuvable.");

                // Vérifier s'il y a des écritures comptables liées
                if (mandat.EcritureComptables != null && mandat.EcritureComptables.Any())
                {
                    return (false,
                        $"Impossible de supprimer ce mandat car il est lié à {mandat.EcritureComptables.Count} écriture(s) comptable(s).");
                }

                context.Mandats.Remove(mandat);
                await context.SaveChangesAsync();
                await AuditService.Instance.LogAsync(
                    "Suppression Mandat",
                    $"mandat supprimé | ID: {mandat.Id} | Numero : {mandat.NumeroMandat} | Montant: {mandat.MontantNet:N0}",
                    SessionManager.CurrentUser?.Username ?? "SYSTEM");
                return (true, "✅ Mandat supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Paiement

        /// <summary>
        /// Marque un mandat comme payé
        /// </summary>
        public async Task<(bool Success, string Message)> MarquerCommePaye(int mandatId, DateTime datePaiement)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Mandat.Edit"))
                return (false, "Permission Mandat.Edit requise pour marquer un mandat comme payé.");

            try
            {
                var mandat = await context.Mandats.FindAsync(mandatId);

                if (mandat == null)
                    return (false, "Mandat introuvable.");

                if (mandat.DatePaiement != null)
                    return (false, "Ce mandat a déjà été payé.");

                mandat.DatePaiement = datePaiement;
                await context.SaveChangesAsync();

                return (true, "✅ Mandat marqué comme payé.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        /// <summary>
        /// Annule le paiement d'un mandat
        /// </summary>
        public async Task<(bool Success, string Message)> AnnulerPaiement(int mandatId)
        {
            using var context = CreateContext();

            if (!SessionManager.HasPermission("Mandat.Edit"))
                return (false, "Permission Mandat.Edit requise pour annuler le paiement d'un mandat.");

            try
            {
                var mandat = await context.Mandats.FindAsync(mandatId);

                if (mandat == null)
                    return (false, "Mandat introuvable.");

                if (mandat.DatePaiement == null)
                    return (false, "Ce mandat n'a pas encore été payé.");

                mandat.DatePaiement = null;
                await context.SaveChangesAsync();

                return (true, "✅ Paiement annulé.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Statistiques

        /// <summary>
        /// Obtient le total des mandats par mois
        /// </summary>
        public async Task<Dictionary<TypeMois, decimal>> GetTotalParMoisAsync()
        {
            using var context = CreateContext();

            return await context.Mandats
                .GroupBy(m => m.Mois)
                .Select(g => new { Mois = g.Key, Total = g.Sum(m => m.MontantNet) })
                .ToDictionaryAsync(x => x.Mois, x => x.Total);
        }

        /// <summary>
        /// Obtient le total des mandats payés et non payés
        /// </summary>
        public async Task<(decimal TotalPaye, decimal TotalNonPaye)> GetStatutsPaiementAsync()
        {
            using var context = CreateContext();

            var totalPaye = await context.Mandats
                .Where(m => m.DatePaiement != null)
                .SumAsync(m => m.MontantNet);

            var totalNonPaye = await context.Mandats
                .Where(m => m.DatePaiement == null)
                .SumAsync(m => m.MontantNet);

            return (totalPaye, totalNonPaye);
        }

        #endregion
    }
}