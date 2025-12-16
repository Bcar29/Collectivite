
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

            return await context.Engagements
                .Include(e => e.Exercice)
                .Include(e => e.Commune)
                .Include(e => e.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(e => e.Tiers)
                .Where(e => e.Etat == Engagement.EtatEngagement.Non_Validé)
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

            // Mettre à jour le MontantRealise du BudgetLine
            if (engagement.BudgetLine != null)
            {
                engagement.BudgetLine.MontantRealise += engagement.MontantEngagement;
            }

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

            return await context.Mandats
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.Tiers)
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.BudgetLine)
                        .ThenInclude(bl => bl.Nommenclature)
                .Where(m => m.Etat == Mandat.EtatMandat.Non_Validé)
                .OrderByDescending(m => m.DateEmission)
                .ToListAsync();
        }

        /// <summary>
        /// Valide un mandat
        /// </summary>
        public async Task<(bool Success, string Message)> ValiderMandatAsync(int idMandat)
        {
            if (!SessionManager.HasPermission("Valider.validate"))
                return (false, "Vous n'avez pas la permission de valider ce mandat.");

            using var context = new AppDbContext();

            var mandat = await context.Mandats
                .Include(m => m.Engagement)
                .FirstOrDefaultAsync(m => m.Id == idMandat);

            if (mandat == null)
                return (false, "Mandat introuvable.");

            if (mandat.Etat == Mandat.EtatMandat.Validé)
                return (false, "Ce mandat est déjà validé.");

            // Vérifier que l'engagement est validé
            if (mandat.Engagement?.Etat != Engagement.EtatEngagement.Validé)
                return (false, "L'engagement associé doit être validé avant de valider ce mandat.");

            // Valider le mandat
            mandat.Etat = Mandat.EtatMandat.Validé;

            await context.SaveChangesAsync();

            return (true, $"Mandat validé avec succès.");
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

            return await context.OrdreRecettes
                .Include(o => o.Exercice)
                .Include(o => o.Commune)
                .Include(o => o.BudgetLine)
                    .ThenInclude(bl => bl.Nommenclature)
                .Include(o => o.Tiers)
                .Where(o => o.Etat == OrdreRecette.EtatOdre.Non_Validé)
                .OrderByDescending(o => o.DateOrdre)
                .ToListAsync();
        }

        /// <summary>
        /// Valide un ordre de recette
        /// </summary>
        public async Task<(bool Success, string Message)> ValiderOrdreRecetteAsync(int idOrdreRecette)
        {
            if (!SessionManager.HasPermission("Valider.validate"))
                return (false, "Vous n'avez pas la permission de valider cet ordre de recette.");

            using var context = new AppDbContext();

            var ordreRecette = await context.OrdreRecettes
                .Include(o => o.BudgetLine)
                .FirstOrDefaultAsync(o => o.Id == idOrdreRecette);

            if (ordreRecette == null)
                return (false, "Ordre de recette introuvable.");

            if (ordreRecette.Etat == OrdreRecette.EtatOdre.Validé)
                return (false, "Cet ordre de recette est déjà validé.");

            // Valider l'ordre de recette
            ordreRecette.Etat = OrdreRecette.EtatOdre.Validé;

            // Mettre à jour le MontantRealise du BudgetLine
            if (ordreRecette.BudgetLine != null)
            {
                ordreRecette.BudgetLine.MontantRealise += ordreRecette.MontantOrdre;
            }

            await context.SaveChangesAsync();

            return (true, $"Ordre de recette validé avec succès.");
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