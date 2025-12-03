
using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class GrandLivreService : IGrandLivreService
    {
        private readonly AppDbContext _context;

        public GrandLivreService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère tous les comptes avec leurs mouvements pour le Grand Livre
        /// </summary>
        public async Task<List<GrandLivreCompteDTO>> GetGrandLivreAsync(GrandLivreFiltreDTO? filtre = null)
        {
            // Récupérer tous les comptes
            var comptes = await _context.CompteComptables
                .OrderBy(c => c.NumeroCompte)
                .ToListAsync();

            // Récupérer toutes les écritures avec filtres
            var ecrituresQuery = _context.EcritureComptables
                .Include(e => e.CompteDebit)
                .Include(e => e.CompteCredit)
                .Include(e => e.OrdreRecette)
                .Include(e => e.Mandat)
                .AsQueryable();

            // Appliquer les filtres
            if (filtre != null)
            {
                if (filtre.Annee.HasValue)
                {
                    ecrituresQuery = ecrituresQuery.Where(e => e.DateEcriture.Year == filtre.Annee.Value);
                }

                if (filtre.Mois.HasValue)
                {
                    ecrituresQuery = ecrituresQuery.Where(e => e.DateEcriture.Month == filtre.Mois.Value);
                }

                if (filtre.DateDebut.HasValue)
                {
                    ecrituresQuery = ecrituresQuery.Where(e => e.DateEcriture >= filtre.DateDebut.Value);
                }

                if (filtre.DateFin.HasValue)
                {
                    ecrituresQuery = ecrituresQuery.Where(e => e.DateEcriture <= filtre.DateFin.Value);
                }

                if (!string.IsNullOrWhiteSpace(filtre.NumeroCompte))
                {
                    ecrituresQuery = ecrituresQuery.Where(e =>
                        e.CompteDebit.NumeroCompte == filtre.NumeroCompte ||
                        e.CompteCredit.NumeroCompte == filtre.NumeroCompte);
                }
            }

            var ecritures = await ecrituresQuery.OrderBy(e => e.DateEcriture).ToListAsync();

            // Construire le Grand Livre
            var grandLivre = new List<GrandLivreCompteDTO>();

            foreach (var compte in comptes)
            {
                var compteGL = new GrandLivreCompteDTO
                {
                    CompteId = compte.Id,
                    NumeroCompte = compte.NumeroCompte,
                    IntituleCompte = compte.IntituleCompte,
                    Mouvements = new List<GrandLivreMouvementDTO>()
                };

                // Écritures où ce compte est débité
                var ecrituresDebit = ecritures
                    .Where(e => e.CompteDebitId == compte.Id)
                    .Select(e => new GrandLivreMouvementDTO
                    {
                        EcritureId = e.Id,
                        DateEcriture = e.DateEcriture,
                        Libelle = GetLibelleEcriture(e),
                        CompteContrepartie = e.CompteCredit.NumeroCompte,
                        MontantDebit = e.Montant,
                        MontantCredit = 0,
                        Reference = GetReference(e),
                        TypeDocument = e.OrdreRecetteId.HasValue ? "Recette" : "Dépense"
                    });

                // Écritures où ce compte est crédité
                var ecrituresCredit = ecritures
                    .Where(e => e.CompteCreditId == compte.Id)
                    .Select(e => new GrandLivreMouvementDTO
                    {
                        EcritureId = e.Id,
                        DateEcriture = e.DateEcriture,
                        Libelle = GetLibelleEcriture(e),
                        CompteContrepartie = e.CompteDebit.NumeroCompte,
                        MontantDebit = 0,
                        MontantCredit = e.Montant,
                        Reference = GetReference(e),
                        TypeDocument = e.OrdreRecetteId.HasValue ? "Recette" : "Dépense"
                    });

                // Combiner et trier par date
                compteGL.Mouvements = ecrituresDebit
                    .Concat(ecrituresCredit)
                    .OrderBy(m => m.DateEcriture)
                    .ThenBy(m => m.EcritureId)
                    .ToList();

                // Calculer le solde cumulé
                decimal soldeCumule = 0;
                foreach (var mouvement in compteGL.Mouvements)
                {
                    soldeCumule += mouvement.MontantDebit - mouvement.MontantCredit;
                    mouvement.SoldeCumulé = soldeCumule;
                }

                // Filtrer les comptes vides si demandé
                if (filtre?.IncluреComptesVides == false && compteGL.Mouvements.Count == 0)
                {
                    continue;
                }

                // Filtrer par numéro de compte spécifique
                if (!string.IsNullOrWhiteSpace(filtre?.NumeroCompte) &&
                    compte.NumeroCompte != filtre.NumeroCompte)
                {
                    continue;
                }

                grandLivre.Add(compteGL);
            }

            // Filtrer par recherche texte
            if (!string.IsNullOrWhiteSpace(filtre?.RechercheTexte))
            {
                var recherche = filtre.RechercheTexte.ToLower();
                grandLivre = grandLivre
                    .Where(c => c.NumeroCompte.ToLower().Contains(recherche) ||
                                c.IntituleCompte.ToLower().Contains(recherche))
                    .ToList();
            }

            return grandLivre;
        }

        /// <summary>
        /// Récupère un compte spécifique avec ses mouvements
        /// </summary>
        public async Task<GrandLivreCompteDTO?> GetCompteDetailAsync(int compteId, GrandLivreFiltreDTO? filtre = null)
        {
            var compte = await _context.CompteComptables.FindAsync(compteId);
            if (compte == null) return null;

            var filtreCompte = filtre ?? new GrandLivreFiltreDTO();
            filtreCompte.NumeroCompte = compte.NumeroCompte;

            var grandLivre = await GetGrandLivreAsync(filtreCompte);
            return grandLivre.FirstOrDefault();
        }

        /// <summary>
        /// Récupère les statistiques globales
        /// </summary>
        public async Task<GrandLivreStatsDTO> GetStatistiquesAsync(GrandLivreFiltreDTO? filtre = null)
        {
            var grandLivre = await GetGrandLivreAsync(filtre);

            return new GrandLivreStatsDTO
            {
                NombreComptes = grandLivre.Count(c => c.Mouvements.Any()),
                NombreEcritures = grandLivre.Sum(c => c.Mouvements.Count) / 2, // Divisé par 2 car chaque écriture apparaît 2 fois
                TotalDebits = grandLivre.Sum(c => c.TotalDebit),
                TotalCredits = grandLivre.Sum(c => c.TotalCredit)
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

            // Ajouter l'année courante si elle n'existe pas
            if (!annees.Contains(DateTime.Now.Year))
            {
                annees.Insert(0, DateTime.Now.Year);
            }

            return annees;
        }

        /// <summary>
        /// Récupère la liste des comptes pour le filtre
        /// </summary>
        public async Task<List<(string Numero, string Intitule)>> GetComptesListAsync()
        {
            return await _context.CompteComptables
                .OrderBy(c => c.NumeroCompte)
                .Select(c => new ValueTuple<string, string>(c.NumeroCompte, c.IntituleCompte))
                .ToListAsync();
        }

        /// <summary>
        /// Exporte le Grand Livre en Excel
        /// </summary>
        public async Task<byte[]> ExportExcelAsync(GrandLivreFiltreDTO? filtre = null)
        {
            // TODO: Implémenter avec ClosedXML ou EPPlus
            var grandLivre = await GetGrandLivreAsync(filtre);

            // Placeholder - à implémenter avec une bibliothèque Excel
            throw new NotImplementedException("Export Excel à implémenter avec ClosedXML ou EPPlus");
        }

        /// <summary>
        /// Exporte le Grand Livre en PDF
        /// </summary>
        public async Task<byte[]> ExportPdfAsync(GrandLivreFiltreDTO? filtre = null)
        {
            // TODO: Implémenter avec iTextSharp ou QuestPDF
            var grandLivre = await GetGrandLivreAsync(filtre);

            // Placeholder - à implémenter avec une bibliothèque PDF
            throw new NotImplementedException("Export PDF à implémenter avec QuestPDF ou iTextSharp");
        }

        #region Méthodes privées

        private static string GetLibelleEcriture(EcritureComptable ecriture)
        {
            if (ecriture.OrdreRecette != null)
            {
                return $"Ordre de recette - {ecriture.OrdreRecette.Motifs ?? "N/A"}";
            }
            if (ecriture.Mandat != null)
            {
                return $"Mandat - {ecriture.Mandat.Objet ?? "N/A"}";
            }
            return "Écriture comptable";
        }

        private static string GetReference(EcritureComptable ecriture)
        {
            if (ecriture.OrdreRecetteId.HasValue)
            {
                return $"OR-{ecriture.OrdreRecetteId}";
            }
            if (ecriture.MandatId.HasValue)
            {
                return $"MAN-{ecriture.MandatId}";
            }
            return $"EC-{ecriture.Id}";
        }

        #endregion
    }
}