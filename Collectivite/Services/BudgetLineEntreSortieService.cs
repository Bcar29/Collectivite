
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour mettre à jour le MontantEntreSortie du BudgetLine
    /// lors d'un paiement de mandat ou d'un encaissement d'ordre de recette
    /// </summary>
    public class BudgetLineEntreSortieService
    {
        /// <summary>
        /// Incrémente le MontantEntreSortie du BudgetLine lié à un mandat
        /// </summary>
        /// <param name="idMandat">ID du mandat payé</param>
        /// <param name="montantPaye">Montant du paiement</param>
        public async Task<bool> IncrémenterPourMandatAsync(int idMandat, decimal montantPaye)
        {
            using var context = new AppDbContext();

            // Mandat → Engagement → BudgetLine
            var mandat = await context.Mandats
                .Include(m => m.Engagement)
                    .ThenInclude(e => e.BudgetLine)
                .FirstOrDefaultAsync(m => m.Id == idMandat);

            if (mandat?.Engagement?.BudgetLine == null)
                return false;

            mandat.Engagement.BudgetLine.MontantEntreSortie += montantPaye;
            await context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Incrémente le MontantEntreSortie du BudgetLine lié à un ordre de recette
        /// </summary>
        /// <param name="idOrdreRecette">ID de l'ordre de recette encaissé</param>
        /// <param name="montantEncaisse">Montant de l'encaissement</param>
        public async Task<bool> IncrémenterPourOrdreRecetteAsync(int idOrdreRecette, decimal montantEncaisse)
        {
            using var context = new AppDbContext();

            // OrdreRecette → BudgetLine (direct)
            var ordreRecette = await context.OrdreRecettes
                .Include(o => o.BudgetLine)
                .FirstOrDefaultAsync(o => o.Id == idOrdreRecette);

            if (ordreRecette?.BudgetLine == null)
                return false;

            ordreRecette.BudgetLine.MontantEntreSortie += montantEncaisse;
            await context.SaveChangesAsync();

            return true;
        }
    }
}