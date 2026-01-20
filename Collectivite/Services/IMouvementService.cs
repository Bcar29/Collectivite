
using Collectivite.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Interface pour le service de gestion des mouvements (paiements et encaissements)
    /// </summary>
    public interface IMouvementService
    {
        // ═══════════════════════════════════════
        // MANDATS
        // ═══════════════════════════════════════

        /// <summary>
        /// Récupère les mandats non totalement payés
        /// </summary>
        Task<List<MandatPaiementDTO>> GetMandatsNonPayesAsync();

        /// <summary>
        /// Récupère un mandat avec son état de paiement
        /// </summary>
        Task<MandatPaiementDTO?> GetMandatPaiementAsync(int mandatId);

        /// <summary>
        /// Récupère l'historique des paiements d'un mandat
        /// </summary>
        Task<List<MouvementHistoriqueDTO>> GetHistoriquePaiementsMandatAsync(int mandatId);

        /// <summary>
        /// Effectue un paiement sur un mandat
        /// </summary>
        Task<(bool Success, string Message, Mouvement? Mouvement)> PayerMandatAsync(MouvementCreationDTO dto);

        // ═══════════════════════════════════════
        // ORDRES DE RECETTE
        // ═══════════════════════════════════════

        /// <summary>
        /// Récupère les ordres de recette non totalement encaissés
        /// </summary>
        Task<List<OrdreRecetteEncaissementDTO>> GetOrdresRecetteNonEncaissesAsync();

        /// <summary>
        /// Récupère un ordre de recette avec son état d'encaissement
        /// </summary>
        Task<OrdreRecetteEncaissementDTO?> GetOrdreRecetteEncaissementAsync(int ordreRecetteId);

        /// <summary>
        /// Récupère l'historique des encaissements d'un ordre de recette
        /// </summary>
        Task<List<MouvementHistoriqueDTO>> GetHistoriqueEncaissementsOrdreRecetteAsync(int ordreRecetteId);

        /// <summary>
        /// Effectue un encaissement sur un ordre de recette
        /// </summary>
        Task<(bool Success, string Message, Mouvement? Mouvement)> EncaisserOrdreRecetteAsync(MouvementCreationDTO dto);

        // ═══════════════════════════════════════
        // UTILITAIRES
        // ═══════════════════════════════════════

        /// <summary>
        /// Récupère un mouvement par son ID
        /// </summary>
        Task<Mouvement?> GetMouvementByIdAsync(int id);

        /// <summary>
        /// Supprime un mouvement (avec annulation de l'écriture comptable)
        /// </summary>
        Task<(bool Success, string Message)> SupprimerMouvementAsync(int mouvementId);

        /// <summary>
        /// Récupère les comptes de trésorerie disponibles
        /// </summary>
        Task<List<CompteComptable>> GetComptesTresorerieAsync();

        /// <summary>
        /// Récupère le solde actuel d'un compte par son numéro (toutes les écritures)
        /// </summary>
        /// <param name="numeroCompte">Le numéro du compte (ex: "55" pour Caisse, "53" pour Banque)</param>
        /// <returns>Le solde du compte (positif = débiteur, négatif = créditeur)</returns>
        Task<decimal> GetSoldeCompteParNumeroAsync(string numeroCompte);
    }
}