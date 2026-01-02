
using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour valider les engagements, mandats et ordres de recette
    /// </summary>
    public class ValidationService
    {
        #region Engagements

        /// <summary>
        /// Récupère tous les engagements non validés
        /// </summary>
        public async Task<List<Engagement>> GetEngagementsNonValidesAsync()
        {
            using var context = new AppDbContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<Engagement>();
            }

            return await context.Engagements
                .Include(e => e.Exercice)
                .Include(e => e.Commune)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.Tiers)
                .Where(e => e.Etat == Engagement.EtatEngagement.Non_Validé && e.ExerciceId == exerciceService.CurrentExercice.Id)
                .OrderByDescending(e => e.DateEngagement)
                .ToListAsync();
        }

        /// <summary>
        /// Valide un engagement
        /// </summary>
        public async Task<(bool Success, string Message)> ValiderEngagementAsync(int idEngagement)
        {
            // Vérifier la permission côté service
            if (!SessionManager.HasPermission("Valider.validate"))
                return (false, "Vous n'avez pas la permission de valider cet engagement.");

            using var context = new AppDbContext();

            var engagement = await context.Engagements
                .Include(e => e.BudgetLine)
                .FirstOrDefaultAsync(e => e.Id == idEngagement);

            if (engagement == null)
                return (false, "Engagement introuvable.");

            if (engagement.Etat == Engagement.EtatEngagement.Validé)
                return (false, "Cet engagement est déjà validé.");

            // Valider l'engagement
            engagement.Etat = Engagement.EtatEngagement.Validé;
            await AuditService.Instance.LogAsync(
                    $"Engagement validé {engagement.Id}",
                    $"Engagement validé | ID: {engagement.Id} | Montant: {engagement.MontantEngagement:N0}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");
            await context.SaveChangesAsync();

            return (true, $"Engagement validé avec succès.");
        }

        /// <summary>
        /// Rejette un engagement (le supprime ou le marque comme rejeté)
        /// </summary>
        public async Task<(bool Success, string Message)> RejeterEngagementAsync(int idEngagement, string motif)
        {
            // Vérifier la permission côté service
            if (!SessionManager.HasPermission("Validation.rejet"))
                return (false, "Vous n'avez pas la permission de rejeter cet engagement.");

            using var context = new AppDbContext();

            var engagement = await context.Engagements.FindAsync(idEngagement);

            if (engagement == null)
                return (false, "Engagement introuvable.");

            // Option : supprimer ou marquer comme rejeté
            // Ici on supprime simplement
            context.Engagements.Remove(engagement);
            await context.SaveChangesAsync();

            await AuditService.Instance.LogAsync(
                    $"Engagement rejeté: {motif}",
                    $"Engagement rejeté | ID: {engagement.Id} | Montant: {engagement.MontantEngagement:N0}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");
            return (true, $"Engagement rejeté et supprimé.");
        }

        #endregion

        #region Mandats

        /// <summary>
        /// Récupère tous les mandats non validés
        /// </summary>
        public async Task<List<Mandat>> GetMandatsNonValidesAsync()
        {
            using var context = new AppDbContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<Mandat>();
            }

            return await context.Mandats
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Tiers)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                .Where(m => m.Etat == Mandat.EtatMandat.Non_Validé && exerciceService.CurrentExercice.Id == m.Engagement.ExerciceId)
                .OrderByDescending(m => m.DateEmission)
                .ToListAsync();
        }

        /// <summary>
        /// Valide un mandat
        /// </summary>
        public async Task<(bool Success, string Message)> ValiderMandatAsync(int idMandat)
        {
            try
            {
                if (!SessionManager.HasPermission("Valider.validate"))
                    return (false, "Vous n'avez pas la permission de valider ce mandat.");

                using var context = new AppDbContext();

                var mandat = await context.Mandats
                    .Include(m => m.Engagement)
                        .ThenInclude(e => e.BudgetLine)
                    .FirstOrDefaultAsync(m => m.Id == idMandat);

                if (mandat == null)
                    return (false, "Mandat introuvable.");

                if (mandat.Etat == Mandat.EtatMandat.Validé)
                    return (false, "Ce mandat est déjà validé.");

                // Vérifier que l'engagement est validé
                if (mandat.Engagement?.Etat != Engagement.EtatEngagement.Validé)
                    return (false, "L'engagement associé doit être validé avant de valider ce mandat.");

                // 1️⃣ Valider le mandat
                mandat.Etat = Mandat.EtatMandat.Validé;
                await context.SaveChangesAsync(); // ← tu voulais garder ça 👍

                // 2️⃣ Mettre à jour la ligne budgétaire + recalcul hiérarchique
                if (mandat.Engagement.BudgetLine != null)
                {
                    mandat.Engagement.BudgetLine.MontantRealise += mandat.MontantNet;

                    await OrdreRecetteService.RecalculateRealisation(
                        context,
                        mandat.Engagement.BudgetLine.NommenclatureId,
                        mandat.Engagement.BudgetLine.BudgetPrimitifId
                    );

                    await context.SaveChangesAsync(); // 🔥 indispensable
                    await AuditService.Instance.LogAsync(
                    $"Mandat Validé : {mandat.NumeroMandat}",
                    $"Validation du mandat | ID: {mandat.Id} | ID: {mandat.NumeroMandat}  | Montant: {mandat.MontantNet:N0}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");
                }

                return (true, "Mandat validé avec succès.");
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                return (false, $"Erreur de base de données : {inner}");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }


        /// <summary>
        /// Rejette un mandat
        /// </summary>
        public async Task<(bool Success, string Message)> RejeterMandatAsync(int idMandat, string motif)
        {
            if (!SessionManager.HasPermission("Validation.rejet"))
                return (false, "Vous n'avez pas la permission de rejeter ce mandat.");

            using var context = new AppDbContext();

            var mandat = await context.Mandats.FindAsync(idMandat);

            if (mandat == null)
                return (false, "Mandat introuvable.");

            context.Mandats.Remove(mandat);
            await context.SaveChangesAsync();

            await AuditService.Instance.LogAsync(
                    $"Mandat Rejeté : {motif}",
                    $"rejet du mandat | ID: {mandat.Id} | ID: {mandat.NumeroMandat}  | Montant: {mandat.MontantNet:N0}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");
            return (true, $"Mandat rejeté et supprimé.");
        }

        #endregion

        #region Ordres de Recette

        /// <summary>
        /// Récupère tous les ordres de recette non validés
        /// </summary>
        public async Task<List<OrdreRecette>> GetOrdresRecetteNonValidesAsync()
        {
            using var context = new AppDbContext();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                return new List<OrdreRecette>();
            }

            return await context.OrdreRecettes
                .Include(o => o.Exercice)
                .Include(o => o.Commune)
                .Include(o => o.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(o => o.Tiers)
                .Where(o => o.Etat == OrdreRecette.EtatOdre.Non_Validé && o.ExerciceId == exerciceService.CurrentExercice.Id)
                .OrderByDescending(o => o.DateOrdre)
                .ToListAsync();
        }

        /// <summary>
        /// Valide un ordre de recette
        /// </summary>
        public async Task<(bool Success, string Message)> ValiderOrdreRecetteAsync(int idOrdreRecette)
        {
            try
            {
                if (!SessionManager.HasPermission("Valider.validate"))
                    return (false, "Vous n'avez pas la permission de valider cet ordre de recette.");

                using var context = new AppDbContext();

                var ordreRecette = await context.OrdreRecettes
                    .Include(o => o.BudgetLine)
                        .ThenInclude(b => b.Nommenclature)
                    .FirstOrDefaultAsync(o => o.Id == idOrdreRecette);

                if (ordreRecette == null)
                    return (false, "Ordre de recette introuvable.");

                if (ordreRecette.Etat == OrdreRecette.EtatOdre.Validé)
                    return (false, "Cet ordre de recette est déjà validé.");

                // 1️⃣ Valider l'ordre de recette
                ordreRecette.Etat = OrdreRecette.EtatOdre.Validé;
                await context.SaveChangesAsync(); // ← même logique que Mandat ✔️

                // 2️⃣ Mise à jour budgétaire + recalcul hiérarchique
                if (ordreRecette.BudgetLine != null)
                {
                    ordreRecette.BudgetLine.MontantRealise += ordreRecette.MontantOrdre;

                    await OrdreRecetteService.RecalculateRealisation(
                        context,
                        ordreRecette.BudgetLine.NommenclatureId,
                        ordreRecette.BudgetLine.BudgetPrimitifId
                    );
                    //ajouter 60% du montant de l'ordre comme realiser sur le prelevement en recette et en depense
                    if (ordreRecette.BudgetLine.Nommenclature.Nature == NatureType.Recette && ordreRecette.BudgetLine.Nommenclature.Section == SectionType.Fonctionnement)
                    {
                        var n662 = await context.BudgetLines
                            .FirstOrDefaultAsync(n => n.Nommenclature.Article == "662");

                        var n110 = await context.BudgetLines
                            .FirstOrDefaultAsync(n => n.Nommenclature.Article == "110");
                        if (n110 != null)
                        {
                            n110.MontantRealise += ordreRecette.MontantOrdre * 0.6m;
                            await OrdreRecetteService.RecalculateRealisation(
                                context,
                                n110.NommenclatureId,
                                n110.BudgetPrimitifId
                            );
                        }
                        if (n662 != null)
                        {
                            n662.MontantRealise += ordreRecette.MontantOrdre * 0.6m;
                            await OrdreRecetteService.RecalculateRealisation(
                                context,
                                n662.NommenclatureId,
                                n662.BudgetPrimitifId
                            );
                        }
                    }
                    await context.SaveChangesAsync(); // 🔥 indispensable
                    await AuditService.Instance.LogAsync(
                        $"Ordre recette  Validé : {ordreRecette.NumeroOrdre}",
                        $"Validation de Ordre Recette | ID: {ordreRecette.Id} | ID: {ordreRecette.NumeroOrdre}  | Montant: {ordreRecette.MontantOrdre:N0}",
                        SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");
                }

                return (true, "Ordre de recette validé avec succès.");
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                return (false, $"Erreur de base de données : {inner}");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }


        /// <summary>
        /// Rejette un ordre de recette
        /// </summary>
        public async Task<(bool Success, string Message)> RejeterOrdreRecetteAsync(int idOrdreRecette, string motif)
        {
            if (!SessionManager.HasPermission("Validation.rejet"))
                return (false, "Vous n'avez pas la permission de rejeter cet ordre de recette.");

            using var context = new AppDbContext();

            var ordreRecette = await context.OrdreRecettes.FindAsync(idOrdreRecette);

            if (ordreRecette == null)
                return (false, "Ordre de recette introuvable.");

            context.OrdreRecettes.Remove(ordreRecette);
            await context.SaveChangesAsync();

            await AuditService.Instance.LogAsync(
                    $"Ordre recette  Rejeté : {ordreRecette.NumeroOrdre}",
                    $"rejet de Ordre Recette | ID: {ordreRecette.Id} | ID: {ordreRecette.NumeroOrdre}  | Montant: {ordreRecette.MontantOrdre:N0}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");
            return (true, $"Ordre de recette rejeté et supprimé.");
        }

        #endregion

        #region Statistiques

        /// <summary>
        /// Obtient le nombre total d'éléments en attente de validation
        /// </summary>
        public async Task<(int Engagements, int Mandats, int OrdresRecette)> GetCountEnAttenteAsync()
        {
            using var context = new AppDbContext();

            var engagements = await context.Engagements
                .CountAsync(e => e.Etat == Engagement.EtatEngagement.Non_Validé);

            var mandats = await context.Mandats
                .CountAsync(m => m.Etat == Mandat.EtatMandat.Non_Validé);

            var ordresRecette = await context.OrdreRecettes
                .CountAsync(o => o.Etat == OrdreRecette.EtatOdre.Non_Validé);

            return (engagements, mandats, ordresRecette);
        }

        #endregion
    }
}