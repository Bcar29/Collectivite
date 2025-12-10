
using Collectivite.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour la gestion de la Balance Annuelle
    /// </summary>
    public class BalanceAnnuelleService : IBalanceAnnuelleService
    {
        private readonly AppDbContext _context;

        public BalanceAnnuelleService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère les lignes de la balance annuelle
        /// </summary>
        public async Task<List<BalanceAnnuelleLigneDTO>> GetBalanceAnnuelleAsync(BalanceAnnuelleFiltreDTO filtre)
        {
            // Récupérer tous les comptes
            var comptes = await _context.CompteComptables
                .OrderBy(c => c.NumeroCompte)
                .ToListAsync();

            // Définir la période de l'année
            var debutAnnee = new DateOnly(filtre.Annee, 1, 1);
            var finAnnee = new DateOnly(filtre.Annee, 12, 31);

            // Récupérer toutes les écritures de l'année
            var ecritures = await _context.EcritureComptables
                .Where(e => e.DateEcriture >= debutAnnee && e.DateEcriture <= finAnnee)
                .ToListAsync();

            var lignes = new List<BalanceAnnuelleLigneDTO>();

            foreach (var compte in comptes)
            {
                var ligne = new BalanceAnnuelleLigneDTO
                {
                    CompteId = compte.Id,
                    NumeroCompte = compte.NumeroCompte,
                    IntituleCompte = compte.IntituleCompte
                };

                // Balance d'entrée (solde de clôture de l'exercice précédent)
                // Dans un système complet, on récupérerait le solde de clôture N-1
                // Pour l'instant, on initialise à 0
                ligne.DebitBalanceEntree = 0;
                ligne.CreditBalanceEntree = 0;

                // Mouvements annuels (tous les mouvements de l'année)
                ligne.DebitMouvAnnuel = ecritures
                    .Where(e => e.CompteDebitId == compte.Id)
                    .Sum(e => e.Montant);

                ligne.CreditMouvAnnuel = ecritures
                    .Where(e => e.CompteCreditId == compte.Id)
                    .Sum(e => e.Montant);

                lignes.Add(ligne);
            }

            // Appliquer les filtres
            if (!string.IsNullOrEmpty(filtre.ClasseCompte))
            {
                lignes = lignes.Where(l => l.NumeroCompte.StartsWith(filtre.ClasseCompte)).ToList();
            }

            if (!string.IsNullOrEmpty(filtre.NumeroCompte))
            {
                lignes = lignes.Where(l => l.NumeroCompte == filtre.NumeroCompte).ToList();
            }

            if (!string.IsNullOrEmpty(filtre.RechercheTexte))
            {
                var recherche = filtre.RechercheTexte.ToLower();
                lignes = lignes.Where(l =>
                    l.NumeroCompte.ToLower().Contains(recherche) ||
                    l.IntituleCompte.ToLower().Contains(recherche)
                ).ToList();
            }

            // Filtrer les comptes vides si demandé
            if (!filtre.AfficherComptesVides)
            {
                lignes = lignes.Where(l =>
                    l.DebitBalanceEntree != 0 ||
                    l.DebitMouvAnnuel != 0 ||
                    l.CreditBalanceEntree != 0 ||
                    l.CreditMouvAnnuel != 0
                ).ToList();
            }

            return lignes;
        }

        /// <summary>
        /// Calcule les totaux de la balance annuelle
        /// </summary>
        public async Task<BalanceAnnuelleTotauxDTO> GetTotauxAsync(BalanceAnnuelleFiltreDTO filtre)
        {
            var lignes = await GetBalanceAnnuelleAsync(filtre);

            return new BalanceAnnuelleTotauxDTO
            {
                TotalDebitBalanceEntree = lignes.Sum(l => l.DebitBalanceEntree),
                TotalDebitMouvAnnuel = lignes.Sum(l => l.DebitMouvAnnuel),
                TotalCreditBalanceEntree = lignes.Sum(l => l.CreditBalanceEntree),
                TotalCreditMouvAnnuel = lignes.Sum(l => l.CreditMouvAnnuel),
                TotalSoldeDebiteur = lignes.Sum(l => l.SoldeDebiteur),
                TotalSoldeCrebiteur = lignes.Sum(l => l.SoldeCrebiteur)
            };
        }

        /// <summary>
        /// Récupère les statistiques de la balance annuelle
        /// </summary>
        public async Task<BalanceAnnuelleStatsDTO> GetStatistiquesAsync(BalanceAnnuelleFiltreDTO filtre)
        {
            var lignes = await GetBalanceAnnuelleAsync(filtre);

            return new BalanceAnnuelleStatsDTO
            {
                NombreComptes = lignes.Count,
                NombreComptesDebiteurs = lignes.Count(l => l.SoldeDebiteur > 0),
                NombreComptesCrediteures = lignes.Count(l => l.SoldeCrebiteur > 0),
                NombreComptesEquilibres = lignes.Count(l => l.SoldeDebiteur == 0 && l.SoldeCrebiteur == 0),
                TotalMouvements = lignes.Sum(l => l.DebitMouvAnnuel + l.CreditMouvAnnuel)
            };
        }

        /// <summary>
        /// Récupère les années disponibles dans les écritures
        /// </summary>
        public async Task<List<int>> GetAnneesDisponiblesAsync()
        {
            var annees = await _context.EcritureComptables
                .Select(e => e.DateEcriture.Year)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();

            // S'assurer que l'année courante est présente
            var anneeCourante = DateTime.Now.Year;
            if (!annees.Contains(anneeCourante))
            {
                annees.Insert(0, anneeCourante);
            }

            return annees;
        }

        /// <summary>
        /// Récupère les classes de comptes disponibles
        /// </summary>
        public async Task<List<string>> GetClassesComptesAsync()
        {
            var classes = await _context.CompteComptables
                .Select(c => c.NumeroCompte.Substring(0, 1))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return classes;
        }

        /// <summary>
        /// Exporte la balance annuelle en Excel
        /// </summary>
        public async Task<byte[]> ExportExcelAsync(BalanceAnnuelleFiltreDTO filtre)
        {
            var lignes = await GetBalanceAnnuelleAsync(filtre);
            var totaux = await GetTotauxAsync(filtre);

            return BalanceAnnuelleExcelExporter.Exporter(lignes, totaux, filtre);
        }

        /// <summary>
        /// Exporte la balance annuelle en PDF
        /// </summary>
        public async Task<byte[]> ExportPdfAsync(BalanceAnnuelleFiltreDTO filtre)
        {
            var lignes = await GetBalanceAnnuelleAsync(filtre);
            var totaux = await GetTotauxAsync(filtre);

            return BalanceAnnuellePdfExporter.Exporter(lignes, totaux, filtre);
        }
    }
}