
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public interface IGrandLivreService
    {
        /// <summary>
        /// Récupère tous les comptes avec leurs mouvements pour le Grand Livre
        /// </summary>
        Task<List<GrandLivreCompteDTO>> GetGrandLivreAsync(GrandLivreFiltreDTO? filtre = null);

        /// <summary>
        /// Récupère un compte spécifique avec ses mouvements
        /// </summary>
        Task<GrandLivreCompteDTO?> GetCompteDetailAsync(int compteId, GrandLivreFiltreDTO? filtre = null);

        /// <summary>
        /// Récupère les statistiques globales
        /// </summary>
        Task<GrandLivreStatsDTO> GetStatistiquesAsync(GrandLivreFiltreDTO? filtre = null);

        /// <summary>
        /// Récupère la liste des années disponibles
        /// </summary>
        Task<List<int>> GetAnneesDisponiblesAsync();

        /// <summary>
        /// Récupère la liste des comptes pour le filtre
        /// </summary>
        Task<List<(string Numero, string Intitule)>> GetComptesListAsync();

        /// <summary>
        /// Exporte le Grand Livre en Excel
        /// </summary>
        Task<byte[]> ExportExcelAsync(GrandLivreFiltreDTO? filtre = null);

        /// <summary>
        /// Exporte le Grand Livre en PDF
        /// </summary>
        Task<byte[]> ExportPdfAsync(GrandLivreFiltreDTO? filtre = null);
    }
}