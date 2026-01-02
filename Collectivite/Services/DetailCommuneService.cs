using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service de gestion des détails des communes
    /// </summary>
    /// <param name="context">Contexte de base de données</param>
    public class DetailCommuneService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        // ══════════════════════════════════════════════════════════
        // RÉCUPÉRER TOUS LES DÉTAILS
        // ══════════════════════════════════════════════════════════
        public async Task<List<DetailCommune>> GetAllAsync()
        {
            return await _context.DetailCommunes
                .Include(d => d.Commune)
                .Include(d => d.Exercice)
                .AsNoTracking()
                .OrderBy(d => d.Commune.Nom)
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════
        // RÉCUPÉRER LES DÉTAILS D'UNE COMMUNE SPÉCIFIQUE
        // ══════════════════════════════════════════════════════════
        public async Task<List<DetailCommune>> GetByCommuneAsync(int communeId)
        {
            return await _context.DetailCommunes
                .Include(d => d.Commune)
                .Include(d => d.Exercice)
                .AsNoTracking()
                .Where(d => d.IdCommune == communeId)
                .ToListAsync();
        }

        public async Task<DetailCommune?> GetDetailCommuneByIdAsync(int communeId)
        {
            try
            {
                using var context = new AppDbContext();
                var exerciceService = ExerciceService.Instance;

                var detailCommune = await context.DetailCommunes
                    .Include(dc => dc.Commune)
                    .Include(dc => dc.Exercice)
                    .FirstOrDefaultAsync(dc => dc.IdCommune == communeId);
                    //.FirstOrDefaultAsync(dc => dc.IdCommune == communeId && dc.Exercice.Id == exerciceService.CurrentExercice.Id);

                return detailCommune;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération des détails de la commune : {ex.Message}");
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        // RÉCUPÉRER UN DÉTAIL PAR ID
        // ══════════════════════════════════════════════════════════
        public async Task<DetailCommune?> GetByIdAsync(int id)
        {
            return await _context.DetailCommunes
                .Include(d => d.Commune)
                .Include(d => d.Exercice)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        // ══════════════════════════════════════════════════════════
        // CRÉER UN NOUVEAU DÉTAIL
        // ══════════════════════════════════════════════════════════
        public async Task<(bool Success, string Message, DetailCommune? DetailCommune)> CreateAsync(DetailCommune detailCommune)
        {
            try
            {
                // Validation : Vérifier que la commune existe
                var communeExiste = await _context.Communes.AnyAsync(c => c.Id == detailCommune.IdCommune);

                if (!communeExiste)
                {
                    return (false, "La commune spécifiée n'existe pas.", null);
                }

                // Validation : Vérifier qu'il n'existe pas déjà un détail pour cette commune
                var detailExiste = await _context.DetailCommunes
                    .AnyAsync(d => d.IdCommune == detailCommune.IdCommune);

                if (detailExiste)
                {
                    return (false, "Un détail existe déjà pour cette commune.", null);
                }

                // Validation : Cohérence des données démographiques
                if (detailCommune.PopulationHommes + detailCommune.PopulationFemmes != detailCommune.PopulationTotale)
                {
                    detailCommune.PopulationTotale = detailCommune.PopulationHommes + detailCommune.PopulationFemmes;
                }

                // Calcul automatique de la densité
                if (detailCommune.Superficie > 0)
                {
                    detailCommune.Densite = Math.Round(detailCommune.PopulationTotale / detailCommune.Superficie, 2);
                }

                // Validation : Cohérence des écoles
                var totalEcoles = detailCommune.NombreEcolesPrescolaire +
                                  detailCommune.NombreEcolesPrimaire +
                                  detailCommune.NombreEcolesCollege +
                                  detailCommune.NombreEcolesLycee;

                if (totalEcoles != detailCommune.NombreEcoles)
                {
                    detailCommune.NombreEcoles = totalEcoles;
                }

                _context.DetailCommunes.Add(detailCommune);
                await _context.SaveChangesAsync();

                return (true, "Détails de la commune créés avec succès.", detailCommune);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création : {ex.Message}", null);
            }
        }

        // ══════════════════════════════════════════════════════════
        // METTRE À JOUR UN DÉTAIL
        // ══════════════════════════════════════════════════════════
        public async Task<(bool Success, string Message)> UpdateAsync(DetailCommune detailCommune)
        {
            try
            {
                // Validation : Vérifier que le détail existe
                var detailExistant = await _context.DetailCommunes.FirstOrDefaultAsync(d => d.Id == detailCommune.Id);
                if (detailExistant == null)
                {
                    return (false, "Détail introuvable.");
                }

                // Validation : Cohérence des données démographiques
                if (detailCommune.PopulationHommes + detailCommune.PopulationFemmes != detailCommune.PopulationTotale)
                {
                    detailCommune.PopulationTotale = detailCommune.PopulationHommes + detailCommune.PopulationFemmes;
                }

                // Calcul automatique de la densité
                if (detailCommune.Superficie > 0)
                {
                    detailCommune.Densite = Math.Round(detailCommune.PopulationTotale / detailCommune.Superficie, 2);
                }

                // Validation : Effectifs cohérents
                if (detailCommune.EffectifPermanent + detailCommune.EffectifTemporaire > detailCommune.EffectifTotalPersonnel)
                {
                    return (false, "La somme des effectifs permanent et temporaire ne peut pas dépasser l'effectif total.");
                }

                // Validation : ONG cohérentes
                if (detailCommune.NombreOngNationales + detailCommune.NombreOngEtrangeres > detailCommune.NombreOng)
                {
                    return (false, "La somme des ONG nationales et étrangères ne peut pas dépasser le nombre total d'ONG.");
                }

                // Validation : Écoles cohérentes
                var totalEcoles = detailCommune.NombreEcolesPrescolaire +
                                  detailCommune.NombreEcolesPrimaire +
                                  detailCommune.NombreEcolesCollege +
                                  detailCommune.NombreEcolesLycee;

                if (totalEcoles > detailCommune.NombreEcoles)
                {
                    return (false, "La somme des écoles par niveau ne peut pas dépasser le nombre total d'écoles.");
                }

                // Validation : Marchés cohérents
                if (detailCommune.NombreMarchesJournaliers + detailCommune.NombreMarchesHebdomadaires > detailCommune.NombreMarches)
                {
                    return (false, "La somme des marchés journaliers et hebdomadaires ne peut pas dépasser le nombre total de marchés.");
                }

                // Copier les valeurs
                _context.Entry(detailExistant).CurrentValues.SetValues(detailCommune);
                detailExistant.IdCommune = detailCommune.IdCommune;

                await _context.SaveChangesAsync();

                return (true, "Détails de la commune modifiés avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la modification : {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        // SUPPRIMER UN DÉTAIL
        // ══════════════════════════════════════════════════════════
        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var detailCommune = await _context.DetailCommunes.FindAsync(id);

                if (detailCommune == null)
                {
                    return (false, "Détail introuvable.");
                }

                _context.DetailCommunes.Remove(detailCommune);
                await _context.SaveChangesAsync();

                return (true, "Détails de la commune supprimés avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        // STATISTIQUES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtenir la population totale de toutes les communes
        /// </summary>
        public async Task<int> GetPopulationTotaleAsync()
        {
            return await _context.DetailCommunes.SumAsync(d => d.PopulationTotale);
        }

        /// <summary>
        /// Obtenir la superficie totale de toutes les communes
        /// </summary>
        public async Task<double> GetSuperficieTotaleAsync()
        {
            return await _context.DetailCommunes.SumAsync(d => d.Superficie);
        }

        /// <summary>
        /// Obtenir la densité moyenne
        /// </summary>
        public async Task<double> GetDensiteMoyenneAsync()
        {
            var details = await _context.DetailCommunes
                .AsNoTracking()
                .ToListAsync();

            if (details.Count == 0) return 0;

            return Math.Round(details.Average(d => d.Densite), 2);
        }

        /// <summary>
        /// Obtenir le nombre total d'écoles
        /// </summary>
        public async Task<int> GetNombreEcolesTotalAsync()
        {
            return await _context.DetailCommunes.SumAsync(d => d.NombreEcoles);
        }

        /// <summary>
        /// Obtenir le nombre total de centres de santé
        /// </summary>
        public async Task<int> GetNombreCentresSanteTotalAsync()
        {
            return await _context.DetailCommunes.SumAsync(d => d.NombreCentresSante);
        }

        /// <summary>
        /// Obtenir le nombre total d'élèves tous niveaux confondus
        /// </summary>
        public async Task<int> GetNombreElevesTotalAsync()
        {
            var details = await _context.DetailCommunes.AsNoTracking().ToListAsync();
            return details.Sum(d => d.NombreElevesPrescolaire +
                                   d.NombreElevesPrimaire +
                                   d.NombreElevesCollege +
                                   d.NombreElevesLycee);
        }
    }
}