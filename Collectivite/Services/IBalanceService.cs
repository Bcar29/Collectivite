
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public interface IBalanceService
    {
        /// <summary>
        /// Récupère la balance comptable avec les filtres spécifiés
        /// </summary>
        Task<List<BalanceLigneDTO>> GetBalanceAsync(BalanceFiltreDTO filtre);

        /// <summary>
        /// Récupère les totaux de la balance
        /// </summary>
        Task<BalanceTotauxDTO> GetTotauxAsync(BalanceFiltreDTO filtre);

        /// <summary>
        /// Calcule les totaux à partir d'une balance déjà chargée, sans nouvelle requête.
        /// </summary>
        BalanceTotauxDTO CalculerTotaux(List<BalanceLigneDTO> balance);

        /// <summary>
        /// Récupère les statistiques de la balance
        /// </summary>
        Task<BalanceStatsDTO> GetStatistiquesAsync(BalanceFiltreDTO filtre);

        /// <summary>
        /// Calcule les statistiques à partir d'une balance déjà chargée, sans nouvelle requête.
        /// </summary>
        BalanceStatsDTO CalculerStatistiques(List<BalanceLigneDTO> balance);

        /// <summary>
        /// Récupère la liste des années disponibles
        /// </summary>
        Task<List<int>> GetAnneesDisponiblesAsync();

        /// <summary>
        /// Récupère la liste des classes de comptes
        /// </summary>
        Task<List<string>> GetClassesComptesAsync();

        /// <summary>
        /// Exporte la balance en Excel
        /// </summary>
        Task<byte[]> ExportExcelAsync(BalanceFiltreDTO filtre);

        /// <summary>
        /// Exporte la balance en PDF
        /// </summary>
        Task<byte[]> ExportPdfAsync(BalanceFiltreDTO filtre);
    }
}