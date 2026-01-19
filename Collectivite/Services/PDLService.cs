using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public interface IPDLService
    {
        Task<List<PDL>> GetAllAsync();
        Task<PDL?> GetByIdAsync(int id);
        Task<PDL?> GetByIdWithDetailsAsync(int id);
        Task<PDL?> GetCurrentPDLAsync();
        Task<PDL> CreateAsync(PDL pdl);
        Task<PDL> UpdateAsync(PDL pdl);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<int> GetActivitesCountAsync(int pdlId);
    }

    public class PDLService : IPDLService
    {
        /// <summary>
        /// Récupère tous les PDL
        /// </summary>
        public async Task<List<PDL>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<PDL>()
                    .OrderByDescending(p => p.DateDebut)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync PDL: {ex.Message}");
                return new List<PDL>();
            }
        }

        /// <summary>
        /// Récupère un PDL par son ID
        /// </summary>
        public async Task<PDL?> GetByIdAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<PDL>()
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByIdAsync PDL: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère un PDL avec toutes ses relations
        /// </summary>
        public async Task<PDL?> GetByIdWithDetailsAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<PDL>()
                    .Include(p => p.Activites)
                        .ThenInclude(a => a.SecteurPDL)
                            .ThenInclude(s => s!.ProgrammePDL)
                    .Include(p => p.Activites)
                        .ThenInclude(a => a.CompetenceCollectivite)
                    .Include(p => p.Activites)
                        .ThenInclude(a => a.ODD)
                    .Include(p => p.Exercices)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByIdWithDetailsAsync PDL: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère le PDL en cours (date actuelle entre DateDebut et DateFin)
        /// </summary>
        public async Task<PDL?> GetCurrentPDLAsync()
        {
            try
            {
                using var context = new AppDbContext();
                var today = DateTime.Today;
                return await context.Set<PDL>()
                    .Where(p => p.DateDebut <= today && p.DateFin >= today)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetCurrentPDLAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crée un nouveau PDL
        /// </summary>
        public async Task<PDL> CreateAsync(PDL pdl)
        {
            using var context = new AppDbContext();
            context.Set<PDL>().Add(pdl);
            await context.SaveChangesAsync();
            return pdl;
        }

        /// <summary>
        /// Met à jour un PDL existant
        /// </summary>
        public async Task<PDL> UpdateAsync(PDL pdl)
        {
            using var context = new AppDbContext();
            context.Set<PDL>().Update(pdl);
            await context.SaveChangesAsync();
            return pdl;
        }

        /// <summary>
        /// Supprime un PDL
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var pdl = await context.Set<PDL>().FindAsync(id);
                if (pdl == null) return false;

                context.Set<PDL>().Remove(pdl);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync PDL: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vérifie si un PDL existe
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<PDL>().AnyAsync(p => p.Id == id);
        }

        /// <summary>
        /// Compte le nombre d'activités d'un PDL
        /// </summary>
        public async Task<int> GetActivitesCountAsync(int pdlId)
        {
            using var context = new AppDbContext();
            return await context.Set<ActivitePDL>()
                .CountAsync(a => a.PDLId == pdlId);
        }
    }
}