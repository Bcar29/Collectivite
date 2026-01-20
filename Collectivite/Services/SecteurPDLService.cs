using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public interface ISecteurPDLService
    {
        Task<List<SecteurPDL>> GetAllAsync();
        Task<List<SecteurPDL>> GetByProgrammeIdAsync(int programmeId);
        Task<SecteurPDL?> GetByIdAsync(int id);
        Task<SecteurPDL?> GetByIdWithActivitesAsync(int id);
        Task<SecteurPDL> CreateAsync(SecteurPDL secteur);
        Task<SecteurPDL> UpdateAsync(SecteurPDL secteur);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<int> GetActivitesCountAsync(int secteurId);
    }

    public class SecteurPDLService : ISecteurPDLService
    {
        /// <summary>
        /// Récupère tous les secteurs PDL
        /// </summary>
        public async Task<List<SecteurPDL>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<SecteurPDL>()
                    .Include(s => s.ProgrammePDL)
                    .OrderBy(s => s.ProgrammePDL!.Libelle)
                    .ThenBy(s => s.Libelle)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync SecteurPDL: {ex.Message}");
                return new List<SecteurPDL>();
            }
        }

        /// <summary>
        /// Récupère les secteurs d'un programme spécifique
        /// </summary>
        public async Task<List<SecteurPDL>> GetByProgrammeIdAsync(int programmeId)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<SecteurPDL>()
                    .Where(s => s.ProgrammePDLId == programmeId)
                    .Include(s => s.ProgrammePDL)
                    .OrderBy(s => s.Libelle)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByProgrammeIdAsync: {ex.Message}");
                return new List<SecteurPDL>();
            }
        }

        /// <summary>
        /// Récupère un secteur par son ID
        /// </summary>
        public async Task<SecteurPDL?> GetByIdAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<SecteurPDL>()
                    .Include(s => s.ProgrammePDL)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByIdAsync SecteurPDL: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère un secteur avec ses activités
        /// </summary>
        public async Task<SecteurPDL?> GetByIdWithActivitesAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<SecteurPDL>()
                    .Include(s => s.ProgrammePDL)
                    .Include(s => s.Activites)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByIdWithActivitesAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crée un nouveau secteur
        /// </summary>
        public async Task<SecteurPDL> CreateAsync(SecteurPDL secteur)
        {
            using var context = new AppDbContext();
            context.Set<SecteurPDL>().Add(secteur);
            await context.SaveChangesAsync();
            return secteur;
        }

        /// <summary>
        /// Met à jour un secteur existant
        /// </summary>
        public async Task<SecteurPDL> UpdateAsync(SecteurPDL secteur)
        {
            using var context = new AppDbContext();
            context.Set<SecteurPDL>().Update(secteur);
            await context.SaveChangesAsync();
            return secteur;
        }

        /// <summary>
        /// Supprime un secteur
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var secteur = await context.Set<SecteurPDL>().FindAsync(id);
                if (secteur == null) return false;

                context.Set<SecteurPDL>().Remove(secteur);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync SecteurPDL: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vérifie si un secteur existe
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<SecteurPDL>().AnyAsync(s => s.Id == id);
        }

        /// <summary>
        /// Compte le nombre d'activités d'un secteur
        /// </summary>
        public async Task<int> GetActivitesCountAsync(int secteurId)
        {
            using var context = new AppDbContext();
            return await context.Set<ActivitePDL>()
                .CountAsync(a => a.SecteurPDLId == secteurId);
        }
    }
}