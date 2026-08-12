using Collectivite.Models;
using DocumentFormat.OpenXml.InkML;
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

            // Exclure les comptes de gestion/budgétaires (classes 6 et 7)
            comptesQuery = comptesQuery.Where(c =>
                !c.NumeroCompte.StartsWith("6") && !c.NumeroCompte.StartsWith("7"));

            var comptes = await comptesQuery.OrderBy(c => c.NumeroCompte).ToListAsync();

            // Définir les périodes
            var debutAnnee = new DateOnly(filtre.Annee, 1, 1);
            var debutMois = new DateOnly(filtre.Annee, filtre.Mois, 1);
            var finMois = debutMois.AddMonths(1).AddDays(-1);

            // ═══════════════════════════════════════
            // ÉTAPE 1 : Récupérer l'exercice en cours (basé sur filtre.Annee)
            // Ex: Si filtre.Annee = 2025, on cherche "Exercice 2025"
            // ═══════════════════════════════════════
            var exerciceEnCours = ExerciceService.Instance.CurrentExercice;
            // ═══════════════════════════════════════
            // ÉTAPE 2 : Extraire l'année du libellé avec GetAnnee()
            // et calculer l'année précédente
            // ═══════════════════════════════════════
            int? anneePrecedente = null;
            if (exerciceEnCours != null)
            {
                // Utilise la méthode GetAnnee() qui extrait l'année du libellé
                // Ex: "Exercice 2025" → 2025
                var anneeExercice = exerciceEnCours.GetAnnee();
                if (anneeExercice.HasValue)
                {
                    // Année précédente = 2025 - 1 = 2024
                    anneePrecedente = anneeExercice.Value - 1;
                }
            }

            // ═══════════════════════════════════════
            // ÉTAPE 3 : Chercher l'exercice précédent
            // Ex: Chercher l'exercice dont le libellé contient "2024"
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
            // Filtrées par date (jusqu'à la fin du mois sélectionné)
            // ═══════════════════════════════════════
            var ecritures = new List<EcritureComptable>();

            if (idExerciceEnCours.HasValue)
            {
                ecritures = await _context.EcritureComptables
                    .Where(e => e.idExercice == idExerciceEnCours.Value && e.DateEcriture <= finMois)
                    .ToListAsync();
            }

            // ═══════════════════════════════════════
            // CONSTRUIRE LA BALANCE
            // ═══════════════════════════════════════
            var balance = new List<BalanceLigneDTO>();

            foreach (var compte in comptes)
            {
                var ligne = new BalanceLigneDTO
                {
                    CompteId = compte.Id,
                    NumeroCompte = compte.NumeroCompte,
                    IntituleCompte = compte.IntituleCompte,
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
        /// Récupère les totaux de la balance
        /// </summary>
        public async Task<BalanceTotauxDTO> GetTotauxAsync(BalanceFiltreDTO filtre)
        {
            var balance = await GetBalanceAsync(filtre);
            return CalculerTotaux(balance);
        }

        /// <summary>
        /// Calcule les totaux à partir d'une balance déjà chargée, sans nouvelle requête.
        /// </summary>
        public BalanceTotauxDTO CalculerTotaux(List<BalanceLigneDTO> balance)
        {
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
            return CalculerStatistiques(balance);
        }

        /// <summary>
        /// Calcule les statistiques à partir d'une balance déjà chargée, sans nouvelle requête.
        /// </summary>
        public BalanceStatsDTO CalculerStatistiques(List<BalanceLigneDTO> balance)
        {
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
            var lignes = await GetBalanceAsync(filtre);
            var totaux = await GetTotauxAsync(filtre);
            return await BalanceExcelExporter.ExporterAsync(lignes, totaux, filtre);  // ✅ await
        }

        /// <summary>
        /// Exporte la balance en PDF
        /// </summary>
        public async Task<byte[]> ExportPdfAsync(BalanceFiltreDTO filtre)
        {
            var lignes = await GetBalanceAsync(filtre);
            var totaux = await GetTotauxAsync(filtre);
            return await BalancePdfExporter.ExporterAsync(lignes, totaux, filtre);  // ✅ await
        }
    }
}