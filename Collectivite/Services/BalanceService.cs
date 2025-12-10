
using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class BalanceService : IBalanceService
    {
        private readonly AppDbContext _context;

        public BalanceService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère la balance comptable avec les filtres spécifiés
        /// </summary>
        public async Task<List<BalanceLigneDTO>> GetBalanceAsync(BalanceFiltreDTO filtre)
        {
            // Récupérer tous les comptes
            var comptesQuery = _context.CompteComptables.AsQueryable();

            // Filtrer par classe de compte si spécifié
            if (!string.IsNullOrWhiteSpace(filtre.ClasseCompte))
            {
                comptesQuery = comptesQuery.Where(c => c.NumeroCompte.StartsWith(filtre.ClasseCompte));
            }

            // Filtrer par numéro de compte spécifique
            if (!string.IsNullOrWhiteSpace(filtre.NumeroCompte))
            {
                comptesQuery = comptesQuery.Where(c => c.NumeroCompte == filtre.NumeroCompte);
            }

            var comptes = await comptesQuery.OrderBy(c => c.NumeroCompte).ToListAsync();

            // Définir les périodes
            var debutAnnee = new DateOnly(filtre.Annee, 1, 1);
            var debutMois = new DateOnly(filtre.Annee, filtre.Mois, 1);
            var finMois = debutMois.AddMonths(1).AddDays(-1);

            // Récupérer toutes les écritures de l'année
            var ecritures = await _context.EcritureComptables
                .Where(e => e.DateEcriture.Year == filtre.Annee && e.DateEcriture <= finMois)
                .ToListAsync();

            // Construire la balance
            var balance = new List<BalanceLigneDTO>();

            foreach (var compte in comptes)
            {
                var ligne = new BalanceLigneDTO
                {
                    CompteId = compte.Id,
                    NumeroCompte = compte.NumeroCompte,
                    IntituleCompte = compte.IntituleCompte
                };

                // ═══════════════════════════════════════
                // BALANCE D'ENTRÉE (solde initial - exercice précédent)
                // Pour simplifier, on considère que c'est 0 au début de l'exercice
                // Dans un système complet, il faudrait récupérer le solde de clôture de l'exercice précédent
                // ═══════════════════════════════════════
                ligne.DebitBalanceEntree = 0;
                ligne.CreditBalanceEntree = 0;

                // ═══════════════════════════════════════
                // MOUVEMENTS ANTÉRIEURS (du début de l'année jusqu'au mois précédent)
                // ═══════════════════════════════════════
                var ecrituresAnterieures = ecritures
                    .Where(e => e.DateEcriture >= debutAnnee && e.DateEcriture < debutMois)
                    .ToList();

                ligne.DebitMouvAnterieur = ecrituresAnterieures
                    .Where(e => e.CompteDebitId == compte.Id)
                    .Sum(e => e.Montant);

                ligne.CreditMouvAnterieur = ecrituresAnterieures
                    .Where(e => e.CompteCreditId == compte.Id)
                    .Sum(e => e.Montant);

                // ═══════════════════════════════════════
                // MOUVEMENTS DU MOIS
                // ═══════════════════════════════════════
                var ecrituresMois = ecritures
                    .Where(e => e.DateEcriture >= debutMois && e.DateEcriture <= finMois)
                    .ToList();

                ligne.DebitMouvMois = ecrituresMois
                    .Where(e => e.CompteDebitId == compte.Id)
                    .Sum(e => e.Montant);

                ligne.CreditMouvMois = ecrituresMois
                    .Where(e => e.CompteCreditId == compte.Id)
                    .Sum(e => e.Montant);

                // Filtrer les comptes vides si demandé
                if (!filtre.AfficherComptesVides &&
                    ligne.DebitTotal == 0 && ligne.CreditTotal == 0)
                {
                    continue;
                }

                balance.Add(ligne);
            }

            // Filtrer par recherche texte
            if (!string.IsNullOrWhiteSpace(filtre.RechercheTexte))
            {
                var recherche = filtre.RechercheTexte.ToLower();
                balance = balance
                    .Where(l => l.NumeroCompte.ToLower().Contains(recherche) ||
                                l.IntituleCompte.ToLower().Contains(recherche))
                    .ToList();
            }

            return balance;
        }

        /// <summary>
        /// Récupère les totaux de la balance
        /// </summary>
        public async Task<BalanceTotauxDTO> GetTotauxAsync(BalanceFiltreDTO filtre)
        {
            var balance = await GetBalanceAsync(filtre);

            return new BalanceTotauxDTO
            {
                // Totaux Débit
                TotalDebitBalanceEntree = balance.Sum(l => l.DebitBalanceEntree),
                TotalDebitMouvAnterieur = balance.Sum(l => l.DebitMouvAnterieur),
                TotalDebitMouvMois = balance.Sum(l => l.DebitMouvMois),

                // Totaux Crédit
                TotalCreditBalanceEntree = balance.Sum(l => l.CreditBalanceEntree),
                TotalCreditMouvAnterieur = balance.Sum(l => l.CreditMouvAnterieur),
                TotalCreditMouvMois = balance.Sum(l => l.CreditMouvMois),

                // Totaux Solde
                TotalSoldeDebiteur = balance.Sum(l => l.SoldeDebiteur),
                TotalSoldeCrebiteur = balance.Sum(l => l.SoldeCrebiteur)
            };
        }

        /// <summary>
        /// Récupère les statistiques de la balance
        /// </summary>
        public async Task<BalanceStatsDTO> GetStatistiquesAsync(BalanceFiltreDTO filtre)
        {
            var balance = await GetBalanceAsync(filtre);

            return new BalanceStatsDTO
            {
                NombreComptes = balance.Count,
                NombreComptesDebiteurs = balance.Count(l => l.SoldeDebiteur > 0),
                NombreComptesCrediteures = balance.Count(l => l.SoldeCrebiteur > 0),
                NombreComptesEquilibres = balance.Count(l => l.SoldeDebiteur == 0 && l.SoldeCrebiteur == 0 && l.DebitTotal > 0),
                TotalMouvements = balance.Sum(l => l.DebitTotal)
            };
        }

        /// <summary>
        /// Récupère la liste des années disponibles
        /// </summary>
        public async Task<List<int>> GetAnneesDisponiblesAsync()
        {
            var annees = await _context.EcritureComptables
                .Select(e => e.DateEcriture.Year)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();

            if (!annees.Contains(DateTime.Now.Year))
            {
                annees.Insert(0, DateTime.Now.Year);
            }

            return annees;
        }

        /// <summary>
        /// Récupère la liste des classes de comptes
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
        /// Exporte la balance en Excel
        /// </summary>
        public async Task<byte[]> ExportExcelAsync(BalanceFiltreDTO filtre)
        {
            var balance = await GetBalanceAsync(filtre);
            var totaux = await GetTotauxAsync(filtre);
            return BalanceExcelExporter.Exporter(balance, totaux, filtre);
        }

        /// <summary>
        /// Exporte la balance en PDF
        /// </summary>
        public async Task<byte[]> ExportPdfAsync(BalanceFiltreDTO filtre)
        {
            var balance = await GetBalanceAsync(filtre);
            var totaux = await GetTotauxAsync(filtre);
            return BalancePdfExporter.Exporter(balance, totaux, filtre);
        }
    }
}