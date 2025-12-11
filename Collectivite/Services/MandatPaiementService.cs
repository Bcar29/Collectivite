
using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class MandatPaiementService
    {
        /// <summary>
        /// Calcule la somme des montants des mouvements pour un mandat
        /// </summary>
        public async Task<decimal> GetMontantPayeAsync(int idMandat)
        {
            using var context = new AppDbContext();

            return await context.Mouvements
                .Where(m => m.idMandat == idMandat)
                .SumAsync(m => m.Montant);
        }

        /// <summary>
        /// Retourne le statut du mandat en fonction du montant payé
        /// </summary>
        public Mandat.StatutMandat GetStatut(decimal montantNet, decimal montantPaye)
        {
            if (montantPaye <= 0)
                return Mandat.StatutMandat.Non_Payé;

            if (montantPaye >= montantNet)
                return Mandat.StatutMandat.Payé;

            return Mandat.StatutMandat.Partiel;
        }

        /// <summary>
        /// Calcule le montant payé et retourne le statut d'un mandat
        /// </summary>
        public async Task<(decimal MontantPaye, Mandat.StatutMandat Statut)> GetInfoPaiementAsync(int idMandat, decimal montantNet)
        {
            var montantPaye = await GetMontantPayeAsync(idMandat);
            var statut = GetStatut(montantNet, montantPaye);

            return (montantPaye, statut);
        }
    }
}