using Collectivite.Models;
using Collectivite.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Utils
{
    public class EcritureComptableHelper
    {
        /// <summary>
        /// Génère une écriture comptable à partir du Budget line
        /// </summary>
        /// <param name="lb">La ligne budgétaire</param>
        /// <param name="or">Ordre de recette</param>
        /// <param name="md">Ordre de recette</param>
        
        /// <returns>Tuple avec (succès, message, écriture créée)</returns>
        public static async Task<(bool Success, string Message, EcritureComptable? Ecriture)> 
            GenererEcritureComptableAsync(BudgetLine lb, OrdreRecette? or = null, Mandat? md = null)
        {
            
            if (lb?.Nommenclature == null)
            {
                return (false, "Ligne budgétaire ou nomenclature invalide.", null);
            }

            var code = lb.Nommenclature.code();
            
            if (code == "Aucun code disponible")
            {
                return (false, "Aucun code disponible pour cette nomenclature.", null);
            }

            using var context = new AppDbContext();

            try
            {
                
                var compteComptable = await context.CompteComptables
                    .Include(cc => cc.ContrePartie)
                    .FirstOrDefaultAsync(cc => cc.NumeroCompte == code);

                if (compteComptable == null)
                {
                    return (false, $"Compte comptable introuvable pour le code '{code}'.", null);
                }

                
                if (compteComptable.ContrePartie == null)
                {
                    return (false, 
                        $"Le compte '{compteComptable.NumeroCompte}' n'a pas de compte de contrepartie défini.", 
                        null);
                }

                EcritureComptable ecriture;

                if (lb.Nommenclature.Nature == NatureType.Recette)
                {
                    // Traitement pour les recettes
                    ecriture = new EcritureComptable
                    {
                        DateEcriture = DateOnly.FromDateTime(DateTime.Now),
                        CompteDebitId = compteComptable.ContrePartie.Id,
                        CompteCreditId = compteComptable.Id,
                        Montant = or.MontantOrdre,
                        OrdreRecetteId = or.Id
                    };
                }
                else if (lb.Nommenclature.Nature == NatureType.Depense)
                {
                    // Traitement pour les dépenses
                    ecriture = new EcritureComptable
                    {
                        DateEcriture = DateOnly.FromDateTime(DateTime.Now),
                        CompteDebitId = compteComptable.Id,
                        CompteCreditId = compteComptable.ContrePartie.Id,
                        Montant = md.MontantNet,
                        OrdreRecetteId = md.Id
                    };
                }
                else
                {
                    
                    return (false, $"Nature '{lb.Nommenclature.Nature}' non supportée.", null);
                }

                context.EcritureComptables.Add(ecriture);
                await context.SaveChangesAsync();

                return (true, "✅ Écriture comptable générée avec succès.", ecriture);
            }
            catch (Exception ex)
            {
                
                return (false, $"Erreur lors de la génération de l'écriture : {ex.Message}", null);
            }
        }
    }
}