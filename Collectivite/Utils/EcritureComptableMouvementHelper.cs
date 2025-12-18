
using Collectivite.Models;
using Collectivite.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Utils
{
    /// <summary>
    /// Helper pour générer automatiquement les écritures comptables lors des mouvements
    /// </summary>
    public class EcritureComptableMouvementHelper
    {
        private readonly AppDbContext _context;

        // Numéros de compte standards
        private const string NUMERO_COMPTE_CAISSE = "53";
        private const string NUMERO_COMPTE_BANQUE = "55";

        public EcritureComptableMouvementHelper(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Génère l'écriture comptable pour un paiement de mandat
        /// </summary>
        /// <param name="mouvement">Le mouvement créé</param>
        /// <param name="mandat">Le mandat concerné</param>
        /// <param name="modeReglement">Le mode de règlement</param>
        /// <returns>L'écriture comptable générée</returns>
        public async Task<EcritureComptable> GenererEcriturePaiementMandatAsync(
            Mouvement mouvement,
            Mandat mandat,
            ModeReglement modeReglement)
        {
            // 1. Récupérer la première écriture liée au mandat
            var premiereEcriture = await _context.EcritureComptables
                .Where(e => e.MandatId == mandat.Id)
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync();

            if (premiereEcriture == null)
            {
                throw new InvalidOperationException(
                    $"Aucune écriture comptable trouvée pour le mandat {mandat.NumeroMandat}");
            }

            // 2. Déterminer le compte de trésorerie selon le mode de règlement
            var compteTresorerie = await GetCompteTresorerieAsync(modeReglement);

            // 3. Créer la deuxième écriture
            // Pour un paiement de mandat :
            // - CompteDebitId = CompteCreditId de la première écriture (on solde le compte fournisseur)
            // - CompteCreditId = Compte de trésorerie (caisse ou banque)
            var deuxiemeEcriture = new EcritureComptable
            {
                DateEcriture = mouvement.Date,
                CompteDebitId = premiereEcriture.CompteCreditId,  // On débite le compte qui était crédité
                CompteCreditId = compteTresorerie.Id,              // On crédite la trésorerie
                Montant = mouvement.Montant,
                MandatId = mandat.Id,
                idExercice = ExerciceService.Instance.CurrentExercice.Id
                // Note: Ajoutez MouvementId si votre modèle le supporte
            };

            return deuxiemeEcriture;
        }

        /// <summary>
        /// Génère l'écriture comptable pour un encaissement d'ordre de recette
        /// </summary>
        /// <param name="mouvement">Le mouvement créé</param>
        /// <param name="ordreRecette">L'ordre de recette concerné</param>
        /// <param name="modeReglement">Le mode de règlement</param>
        /// <returns>L'écriture comptable générée</returns>
        public async Task<EcritureComptable> GenererEcritureEncaissementOrdreRecetteAsync(
            Mouvement mouvement,
            OrdreRecette ordreRecette,
            ModeReglement modeReglement)
        {
            // 1. Récupérer la première écriture liée à l'ordre de recette
            var premiereEcriture = await _context.EcritureComptables
                .Where(e => e.OrdreRecetteId == ordreRecette.Id)
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync();

            if (premiereEcriture == null)
            {
                throw new InvalidOperationException(
                    $"Aucune écriture comptable trouvée pour l'ordre de recette {ordreRecette.NumeroOrdre}");
            }

            // 2. Déterminer le compte de trésorerie selon le mode de règlement
            var compteTresorerie = await GetCompteTresorerieAsync(modeReglement);

            // 3. Créer la deuxième écriture
            // Pour un encaissement d'ordre de recette (inverse du mandat) :
            // - CompteDebitId = Compte de trésorerie (on reçoit l'argent)
            // - CompteCreditId = CompteDebitId de la première écriture (on solde le compte client)
            var deuxiemeEcriture = new EcritureComptable
            {
                DateEcriture = mouvement.Date,
                CompteDebitId = compteTresorerie.Id,               // On débite la trésorerie (on reçoit)
                CompteCreditId = premiereEcriture.CompteDebitId,   // On crédite le compte qui était débité
                Montant = mouvement.Montant,
                OrdreRecetteId = ordreRecette.Id,
                idExercice = ExerciceService.Instance.CurrentExercice.Id
                // Note: Ajoutez MouvementId si votre modèle le supporte
            };

            return deuxiemeEcriture;
        }

        /// <summary>
        /// Récupère le compte de trésorerie selon le mode de règlement
        /// </summary>
        /// <param name="modeReglement">Le mode de règlement</param>
        /// <returns>Le compte comptable correspondant</returns>
        public async Task<CompteComptable> GetCompteTresorerieAsync(ModeReglement modeReglement)
        {
            string numeroCompte = modeReglement switch
            {
                ModeReglement.Espece => NUMERO_COMPTE_CAISSE,       // Compte 53 - Caisse
                ModeReglement.Virement => NUMERO_COMPTE_BANQUE,     // Compte 55 - Banque
                ModeReglement.Cheque => NUMERO_COMPTE_BANQUE,       // Compte 55 - Banque
                _ => NUMERO_COMPTE_CAISSE
            };

            // Rechercher le compte qui commence par le numéro
            var compte = await _context.CompteComptables
                .Where(c => c.NumeroCompte.StartsWith(numeroCompte))
                .OrderBy(c => c.NumeroCompte.Length)  // Prendre le compte principal d'abord
                .ThenBy(c => c.NumeroCompte)
                .FirstOrDefaultAsync();

            if (compte == null)
            {
                throw new InvalidOperationException(
                    $"Le compte de trésorerie {numeroCompte} n'existe pas dans le plan comptable. " +
                    "Veuillez créer le compte avant d'effectuer des mouvements.");
            }

            return compte;
        }

        /// <summary>
        /// Vérifie si un mandat a une écriture comptable initiale
        /// </summary>
        public async Task<bool> MandatAEcritureInitialeAsync(int mandatId)
        {
            return await _context.EcritureComptables
                .AnyAsync(e => e.MandatId == mandatId);
        }

        /// <summary>
        /// Vérifie si un ordre de recette a une écriture comptable initiale
        /// </summary>
        public async Task<bool> OrdreRecetteAEcritureInitialeAsync(int ordreRecetteId)
        {
            return await _context.EcritureComptables
                .AnyAsync(e => e.OrdreRecetteId == ordreRecetteId);
        }
    }
}