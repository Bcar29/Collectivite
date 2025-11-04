using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{

    /// <param name="context">Contexte de base de données</param>
    public class DetailCommuneService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        // ══════════════════════════════════════════════════════════
        // RÉCUPÉRER TOUS LES DÉTAILS
        // ══════════════════════════════════════════════════════════
        // ✅ CORRECTION : Ajouter AsNoTracking() pour éviter les conflits de threading
        public async Task<List<DetailCommune>> GetAllAsync()
        {
            // Utiliser AsNoTracking pour éviter de laisser EF Core suivre ces instances
            // (on n'a pas besoin de suivi pour l'affichage en lecture seule)
            return await _context.DetailCommunes
                .Include(d => d.Commune)
                .AsNoTracking()
                .OrderBy(d => d.Commune.Nom)
                .ToListAsync();
        }


        // ══════════════════════════════════════════════════════════
        // RÉCUPÉRER LES DÉTAILS D'UNE COMMUNE SPÉCIFIQUE
        // ══════════════════════════════════════════════════════════
        public async Task<List<DetailCommune>> GetByCommuneAsync(int communeId)
        {
            // Lecture seule — éviter le tracking pour réduire les risques de conflits
            return await _context.DetailCommunes
                .Include(d => d.Commune)
                .AsNoTracking()
                .Where(d => d.IdCommune == communeId)
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════
        // RÉCUPÉRER UN DÉTAIL PAR ID
        // ══════════════════════════════════════════════════════════
        public async Task<DetailCommune?> GetByIdAsync(int id)
        {
            // Lecture seule — utiliser AsNoTracking pour retourner une copie sans suivi
            return await _context.DetailCommunes
                .Include(d => d.Commune)
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

                // Validation : Cohérence des données
                if (detailCommune.PopulationHommes + detailCommune.PopulationFemmes != detailCommune.PopulationTotale)
                {
                    detailCommune.PopulationTotale = detailCommune.PopulationHommes + detailCommune.PopulationFemmes;
                }

                // Calcul automatique de la densité
                if (detailCommune.Superficie > 0)
                {
                    detailCommune.Densite = Math.Round(detailCommune.PopulationTotale / detailCommune.Superficie, 2);
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

                // Validation : Cohérence des données
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
                if (detailCommune.NombreEcolesPrimaires + detailCommune.NombreEcolesSecondaires > detailCommune.NombreEcoles)
                {
                    return (false, "La somme des écoles primaires et secondaires ne peut pas dépasser le nombre total d'écoles.");
                }

                // Validation : Marchés cohérents
                if (detailCommune.NombreMarchesJournaliers + detailCommune.NombreMarchesHebdomadaires > detailCommune.NombreMarches)
                {
                    return (false, "La somme des marchés journaliers et hebdomadaires ne peut pas dépasser le nombre total de marchés.");
                }

                // Copier les valeurs du DTO / instance entrante dans l'entité suivie par le contexte
                // Cela évite l'exception "cannot be tracked because another instance with the same key value is already being tracked"
                _context.Entry(detailExistant).CurrentValues.SetValues(detailCommune);

                // Si l'entité entrante contient une navigation 'Commune' distincte, ne pas essayer
                // d'attacher automatiquement une autre instance. Mettre à jour l'IdCommune seulement.
                detailExistant.IdCommune = detailCommune.IdCommune;

                await _context.SaveChangesAsync();

                return (true, "Détails de la commune modifiés avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la modification ser: {ex.Message}");
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
            // Récupérer en lecture seule pour éviter le suivi inutile
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
    }
}