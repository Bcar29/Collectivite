using Collectivite.Models;
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

            // Exclure les comptes de gestion/budgétaires (classes 6 et 7)
            comptesQuery = comptesQuery.Where(c =>
                !c.NumeroCompte.StartsWith("6") && !c.NumeroCompte.StartsWith("7"));

            var comptes = await comptesQuery.OrderBy(c => c.NumeroCompte).ToListAsync();

            // ═══════════════════════════════════════
            // ÉTAPE 1 : Récupérer l'exercice en cours via ExerciceService
            // ═══════════════════════════════════════
            var exerciceEnCours = ExerciceService.Instance.CurrentExercice;

            // ═══════════════════════════════════════
            // ÉTAPE 2 : Extraire l'année du libellé avec GetAnnee()
            // et calculer l'année précédente
            // ═══════════════════════════════════════
            int? anneePrecedente = null;
            if (exerciceEnCours != null)
            {
                var anneeExercice = exerciceEnCours.GetAnnee();
                if (anneeExercice.HasValue)
                {
                    anneePrecedente = anneeExercice.Value - 1;
                }
            }

            // ═══════════════════════════════════════
            // ÉTAPE 3 : Chercher l'exercice précédent
            // ═══════════════════════════════════════
            Exercice? exercicePrecedent = null;
            if (anneePrecedente.HasValue)
            {
                exercicePrecedent = await _context.Exercices
                    .FirstOrDefaultAsync(e => e.Libelle != null && e.Libelle.Contains(anneePrecedente.Value.ToString()));
            }

            int? idExercicePrecedent = exercicePrecedent?.Id;
            int? idExerciceEnCours = exerciceEnCours?.Id;

            // ═══════════════════════════════════════
            // ÉTAPE 4 : Récupérer les écritures de l'exercice PRÉCÉDENT
            // Pour calculer la Balance d'Entrée
            // ═══════════════════════════════════════
            var ecrituresAnneePrecedente = new List<EcritureComptable>();

            if (idExercicePrecedent.HasValue)
            {
                ecrituresAnneePrecedente = await _context.EcritureComptables
                    .Where(e => e.idExercice == idExercicePrecedent.Value)
                    .ToListAsync();
            }

            // ═══════════════════════════════════════
            // ÉTAPE 5 : Récupérer les écritures de l'exercice EN COURS
            // (toute l'année pour la balance annuelle)
            // ═══════════════════════════════════════
            var ecritures = new List<EcritureComptable>();

            if (idExerciceEnCours.HasValue)
            {
                ecritures = await _context.EcritureComptables
                    .Where(e => e.idExercice == idExerciceEnCours.Value)
                    .ToListAsync();
            }

            // ═══════════════════════════════════════
            // CONSTRUIRE LA BALANCE ANNUELLE
            // ═══════════════════════════════════════
            var lignes = new List<BalanceAnnuelleLigneDTO>();

            foreach (var compte in comptes)
            {
                var ligne = new BalanceAnnuelleLigneDTO
                {
                    CompteId = compte.Id,
                    NumeroCompte = compte.NumeroCompte,
                    IntituleCompte = compte.IntituleCompte
                };

                // ═══════════════════════════════════════
                // BALANCE D'ENTRÉE (solde de l'exercice précédent)
                // ═══════════════════════════════════════
                if (!EstCompteGestionOuBudgetaire(compte.NumeroCompte) && ecrituresAnneePrecedente.Count > 0)
                {
                    var soldeAnneePrecedente = CalculerSoldeCompte(ecrituresAnneePrecedente, compte.Id);

                    if (soldeAnneePrecedente >= 0)
                    {
                        // Solde débiteur → va dans Débit Balance Entrée
                        ligne.DebitBalanceEntree = soldeAnneePrecedente;
                        ligne.CreditBalanceEntree = 0;
                    }
                    else
                    {
                        // Solde créditeur → va dans Crédit Balance Entrée
                        ligne.DebitBalanceEntree = 0;
                        ligne.CreditBalanceEntree = Math.Abs(soldeAnneePrecedente);
                    }
                }
                else
                {
                    ligne.DebitBalanceEntree = 0;
                    ligne.CreditBalanceEntree = 0;
                }

                // ═══════════════════════════════════════
                // MOUVEMENTS ANNUELS (tous les mouvements de l'année)
                // ═══════════════════════════════════════
                ligne.DebitMouvAnnuel = ecritures
                    .Where(e => e.CompteDebitId == compte.Id)
                    .Sum(e => e.Montant);

                ligne.CreditMouvAnnuel = ecritures
                    .Where(e => e.CompteCreditId == compte.Id)
                    .Sum(e => e.Montant);

                // Filtrer les comptes vides si demandé
                if (!filtre.AfficherComptesVides &&
                    ligne.DebitBalanceEntree == 0 && ligne.CreditBalanceEntree == 0 &&
                    ligne.DebitMouvAnnuel == 0 && ligne.CreditMouvAnnuel == 0)
                {
                    continue;
                }

                lignes.Add(ligne);
            }

            // ═══════════════════════════════════════
            // APPLIQUER LES FILTRES
            // ═══════════════════════════════════════
            if (!string.IsNullOrWhiteSpace(filtre.RechercheTexte))
            {
                var recherche = filtre.RechercheTexte.ToLower();
                lignes = lignes
                    .Where(l => l.NumeroCompte.ToLower().Contains(recherche) ||
                                l.IntituleCompte.ToLower().Contains(recherche))
                    .ToList();
            }

            return lignes;
        }

        private static bool EstCompteGestionOuBudgetaire(string? numeroCompte)
        {
            if (string.IsNullOrWhiteSpace(numeroCompte))
            {
                return false;
            }

            return numeroCompte.StartsWith("6", StringComparison.Ordinal) ||
                   numeroCompte.StartsWith("7", StringComparison.Ordinal);
        }

        /// <summary>
        /// Calcule le solde d'un compte à partir d'une liste d'écritures
        /// Solde = Total Débit - Total Crédit
        /// Positif = Débiteur, Négatif = Créditeur
        /// </summary>
        private decimal CalculerSoldeCompte(List<EcritureComptable> ecritures, int compteId)
        {
            var totalDebit = ecritures
                .Where(e => e.CompteDebitId == compteId)
                .Sum(e => e.Montant);

            var totalCredit = ecritures
                .Where(e => e.CompteCreditId == compteId)
                .Sum(e => e.Montant);

            return totalDebit - totalCredit;
        }

        /// <summary>
        /// Calcule les totaux de la balance annuelle
        /// </summary>
        public async Task<BalanceAnnuelleTotauxDTO> GetTotauxAsync(BalanceAnnuelleFiltreDTO filtre)
        {
            var lignes = await GetBalanceAnnuelleAsync(filtre);
            return CalculerTotaux(lignes);
        }

        /// <summary>
        /// Calcule les totaux à partir d'une balance annuelle déjà chargée, sans nouvelle requête.
        /// </summary>
        public BalanceAnnuelleTotauxDTO CalculerTotaux(List<BalanceAnnuelleLigneDTO> lignes)
        {
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
            return CalculerStatistiques(lignes);
        }

        /// <summary>
        /// Calcule les statistiques à partir d'une balance annuelle déjà chargée, sans nouvelle requête.
        /// </summary>
        public BalanceAnnuelleStatsDTO CalculerStatistiques(List<BalanceAnnuelleLigneDTO> lignes)
        {
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

            if (!annees.Contains(DateTime.Now.Year))
            {
                annees.Insert(0, DateTime.Now.Year);
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