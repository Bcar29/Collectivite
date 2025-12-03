using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collectivite.Models;
using Microsoft.EntityFrameworkCore;

namespace Collectivite.Services
{
    public class NommenclatureService
    {
        private readonly AppDbContext _context;

        public NommenclatureService(AppDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════════════════
        // MÉTHODES EXISTANTES (CRUD de base)
        // ═══════════════════════════════════════════════════════════════════

        // recuperer tous les nommenclatures
        public async Task<List<Nommenclature>> GetAllNommenclatureAsync()
        {
            return await _context.Nommenclatures
                .OrderBy(n => n.Section)
                .ToListAsync();
        }

        // ajouter une nommenclature
        public async Task<(bool Success, string Message, Nommenclature? Nommenclature)> CreateNommenclatureAsync(Nommenclature nommenclature)
        {
            try
            {
                // Validation : Vérifier qu'il n'existe pas déjà une nommenclature avec le même intitulé
                var existe = await _context.Nommenclatures
                    .AnyAsync(n => n.Intitule == nommenclature.Intitule);
                if (existe)
                {
                    return (false, $"{nommenclature.Intitule} existe déjà.", null);
                }
                _context.Nommenclatures.Add(nommenclature);
                await _context.SaveChangesAsync();
                return (true, "Nommenclature créée avec succès.", nommenclature);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création de la nommenclature : {ex.Message}", null);
            }
        }

        // mettre à jour une nommenclature
        public async Task<(bool Success, string Message)> UpdateNommenclatureAsync(Nommenclature nommenclature)
        {
            try
            {
                var existingNommenclature = await _context.Nommenclatures
                    .FirstOrDefaultAsync(n => n.Id == nommenclature.Id);
                if (existingNommenclature == null)
                {
                    return (false, "Nommenclature non trouvée.");
                }
                // Validation : Vérifier qu'il n'existe pas déjà une nommenclature avec le même intitulé
                var existe = await _context.Nommenclatures
                    .AnyAsync(n => n.Intitule == nommenclature.Intitule && n.Id != nommenclature.Id);
                if (existe)
                {
                    return (false, $"{nommenclature.Intitule} existe déjà.");
                }
                existingNommenclature.Chapitre = nommenclature.Chapitre;
                existingNommenclature.Article = nommenclature.Article;
                existingNommenclature.Paragraphe = nommenclature.Paragraphe;
                existingNommenclature.SousParagraphe = nommenclature.SousParagraphe;
                existingNommenclature.Intitule = nommenclature.Intitule;
                existingNommenclature.Nature = nommenclature.Nature;
                existingNommenclature.Section = nommenclature.Section;
                existingNommenclature.ParentId = nommenclature.ParentId;
                await _context.SaveChangesAsync();
                return (true, "Nommenclature mise à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour de la nommenclature : {ex.Message}");
            }
        }

        // supprimer une nommenclature
        public async Task<(bool Success, string Message)> DeleteNommenclatureAsync(int nommenclatureId)
        {
            try
            {
                var existingNommenclature = await _context.Nommenclatures
                    .FirstOrDefaultAsync(n => n.Id == nommenclatureId);
                if (existingNommenclature == null)
                {
                    return (false, "Nommenclature non trouvée.");
                }
                _context.Nommenclatures.Remove(existingNommenclature);
                await _context.SaveChangesAsync();
                return (true, "Nommenclature supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression de la nommenclature : {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // NOUVELLES MÉTHODES - Récupération des nomenclatures terminales (sans enfants)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Récupère uniquement les nomenclatures terminales (sans enfants) 
        /// c'est-à-dire les comptes de dernier niveau dans la hiérarchie
        /// </summary>
        /// <returns>Liste des nomenclatures sans sous-comptes</returns>
        public async Task<List<Nommenclature>> GetNommenclaturesSansEnfantsAsync()
        {
            return await _context.Nommenclatures
                .Include(n => n.Enfants)
                .Include(n => n.Parent)
                .Where(n => n.Enfants == null || n.Enfants.Count == 0)
                .AsNoTracking()
                .OrderBy(n => n.Chapitre)
                .ThenBy(n => n.Article)
                .ThenBy(n => n.Paragraphe)
                .ThenBy(n => n.SousParagraphe)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère uniquement les nomenclatures terminales d'une nature spécifique
        /// </summary>
        /// <param name="nature">Nature (Recette ou Dépense)</param>
        /// <returns>Liste des nomenclatures sans sous-comptes pour la nature spécifiée</returns>
        public async Task<List<Nommenclature>> GetNommenclaturesSansEnfantsByNatureAsync(NatureType nature)
        {
            return await _context.Nommenclatures
                .Include(n => n.Enfants)
                .Include(n => n.Parent)
                .Where(n => n.Nature == nature && (n.Enfants == null || n.Enfants.Count == 0))
                .AsNoTracking()
                .OrderBy(n => n.Chapitre)
                .ThenBy(n => n.Article)
                .ThenBy(n => n.Paragraphe)
                .ThenBy(n => n.SousParagraphe)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère uniquement les nomenclatures terminales d'une section spécifique
        /// </summary>
        /// <param name="section">Section (Fonctionnement ou Investissement)</param>
        /// <returns>Liste des nomenclatures sans sous-comptes pour la section spécifiée</returns>
        public async Task<List<Nommenclature>> GetNommenclaturesSansEnfantsBySectionAsync(SectionType section)
        {
            return await _context.Nommenclatures
                .Include(n => n.Enfants)
                .Include(n => n.Parent)
                .Where(n => n.Section == section && (n.Enfants == null || n.Enfants.Count == 0))
                .AsNoTracking()
                .OrderBy(n => n.Chapitre)
                .ThenBy(n => n.Article)
                .ThenBy(n => n.Paragraphe)
                .ThenBy(n => n.SousParagraphe)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère uniquement les nomenclatures terminales avec des filtres combinés
        /// </summary>
        /// <param name="nature">Nature optionnelle (Recette ou Dépense)</param>
        /// <param name="section">Section optionnelle (Fonctionnement ou Investissement)</param>
        /// <returns>Liste des nomenclatures sans sous-comptes avec les filtres appliqués</returns>
        public async Task<List<Nommenclature>> GetNommenclaturesSansEnfantsAvecFiltresAsync(
            NatureType? nature = null,
            SectionType? section = null)
        {
            var query = _context.Nommenclatures
                .Include(n => n.Enfants)
                .Include(n => n.Parent)
                .Where(n => n.Enfants == null || n.Enfants.Count == 0);

            // Appliquer le filtre Nature si spécifié
            if (nature.HasValue)
            {
                query = query.Where(n => n.Nature == nature.Value);
            }

            // Appliquer le filtre Section si spécifié
            if (section.HasValue)
            {
                query = query.Where(n => n.Section == section.Value);
            }

            return await query
                .AsNoTracking()
                .OrderBy(n => n.Chapitre)
                .ThenBy(n => n.Article)
                .ThenBy(n => n.Paragraphe)
                .ThenBy(n => n.SousParagraphe)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère une nomenclature par son ID avec ses relations
        /// </summary>
        /// <param name="id">ID de la nomenclature</param>
        /// <returns>La nomenclature trouvée ou null</returns>
        public async Task<Nommenclature?> GetNommenclatureByIdAsync(int id)
        {
            return await _context.Nommenclatures
                .Include(n => n.Enfants)
                .Include(n => n.Parent)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        /// <summary>
        /// Vérifie si une nomenclature a des enfants
        /// </summary>
        /// <param name="id">ID de la nomenclature</param>
        /// <returns>True si la nomenclature a des enfants, False sinon</returns>
        public async Task<bool> HasEnfantsAsync(int id)
        {
            return await _context.Nommenclatures
                .AnyAsync(n => n.ParentId == id);
        }

        /// <summary>
        /// Compte le nombre d'enfants d'une nomenclature
        /// </summary>
        /// <param name="id">ID de la nomenclature parent</param>
        /// <returns>Nombre d'enfants</returns>
        public async Task<int> CountEnfantsAsync(int id)
        {
            return await _context.Nommenclatures
                .CountAsync(n => n.ParentId == id);
        }
    }
}