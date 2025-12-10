using Collectivite.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Interface pour le service de la Balance Annuelle
    /// </summary>
    public interface IBalanceAnnuelleService
    {
        /// <summary>
        /// Récupère les lignes de la balance annuelle selon les filtres
        /// </summary>
        Task<List<BalanceAnnuelleLigneDTO>> GetBalanceAnnuelleAsync(BalanceAnnuelleFiltreDTO filtre);

        /// <summary>
        /// Récupère les totaux de la balance annuelle
        /// </summary>
        Task<BalanceAnnuelleTotauxDTO> GetTotauxAsync(BalanceAnnuelleFiltreDTO filtre);

        /// <summary>
        /// Récupère les statistiques de la balance annuelle
        /// </summary>
        Task<BalanceAnnuelleStatsDTO> GetStatistiquesAsync(BalanceAnnuelleFiltreDTO filtre);

        /// <summary>
        /// Récupère la liste des années disponibles
        /// </summary>
        Task<List<int>> GetAnneesDisponiblesAsync();

        /// <summary>
        /// Récupère les classes de comptes disponibles
        /// </summary>
        Task<List<string>> GetClassesComptesAsync();

        /// <summary>
        /// Exporte la balance annuelle en Excel
        /// </summary>
        Task<byte[]> ExportExcelAsync(BalanceAnnuelleFiltreDTO filtre);

        /// <summary>
        /// Exporte la balance annuelle en PDF
        /// </summary>
        Task<byte[]> ExportPdfAsync(BalanceAnnuelleFiltreDTO filtre);
    }
}