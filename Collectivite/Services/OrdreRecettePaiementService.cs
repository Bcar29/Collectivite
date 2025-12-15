
using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class OrdreRecettePaiementService
    {
        /// <summary>
        /// Calcule la somme des montants des mouvements pour un ordre de recette
        /// </summary>
        public async Task<decimal> GetMontantEncaisseAsync(int idOrdreRecette)
        {
            using var context = new AppDbContext();

            return await context.Mouvements
                .Where(m => m.idOrdreRecette == idOrdreRecette)
                .SumAsync(m => m.Montant);
        }

        /// <summary>
        /// Retourne le statut de l'ordre de recette en fonction du montant encaissé
        /// </summary>
        public OrdreRecette.StatutOrdre GetStatut(decimal montantOrdre, decimal montantEncaisse)
        {
            if (montantEncaisse <= 0)
                return OrdreRecette.StatutOrdre.Non_Encaissé;

            if (montantEncaisse >= montantOrdre)
                return OrdreRecette.StatutOrdre.Enciassé;

            return OrdreRecette.StatutOrdre.Partiel;
        }

        /// <summary>
        /// Calcule le montant encaissé et retourne le statut d'un ordre de recette
        /// </summary>
        public async Task<(decimal MontantEncaisse, OrdreRecette.StatutOrdre Statut)> GetInfoEncaissementAsync(int idOrdreRecette, decimal montantOrdre)
        {
            var montantEncaisse = await GetMontantEncaisseAsync(idOrdreRecette);
            var statut = GetStatut(montantOrdre, montantEncaisse);

            return (montantEncaisse, statut);
        }
    }
}