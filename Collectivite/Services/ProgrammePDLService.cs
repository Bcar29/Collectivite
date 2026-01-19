using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public interface IProgrammePDLService
    {
        Task<List<ProgrammePDL>> GetAllAsync();
        Task<ProgrammePDL?> GetByIdAsync(int id);
        Task<ProgrammePDL?> GetByIdWithSecteursAsync(int id);
        Task<ProgrammePDL> CreateAsync(ProgrammePDL programme);
        Task<ProgrammePDL> UpdateAsync(ProgrammePDL programme);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<int> GetSecteursCountAsync(int programmeId);
    }

    public class ProgrammePDLService : IProgrammePDLService
    {
        /// <summary>
        /// Récupère tous les programmes PDL
        /// </summary>
        public async Task<List<ProgrammePDL>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ProgrammePDL>()
                    .Include(p => p.SecteursPDL)
                    .OrderBy(p => p.Libelle)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync ProgrammePDL: {ex.Message}");
                return new List<ProgrammePDL>();
            }
        }

        /// <summary>
        /// Récupère un programme par son ID
        /// </summary>
        public async Task<ProgrammePDL?> GetByIdAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ProgrammePDL>()
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByIdAsync ProgrammePDL: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère un programme avec ses secteurs
        /// </summary>
        public async Task<ProgrammePDL?> GetByIdWithSecteursAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ProgrammePDL>()
                    .Include(p => p.SecteursPDL)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByIdWithSecteursAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crée un nouveau programme
        /// </summary>
        public async Task<ProgrammePDL> CreateAsync(ProgrammePDL programme)
        {
            using var context = new AppDbContext();
            context.Set<ProgrammePDL>().Add(programme);
            await context.SaveChangesAsync();
            return programme;
        }

        /// <summary>
        /// Met à jour un programme existant
        /// </summary>
        public async Task<ProgrammePDL> UpdateAsync(ProgrammePDL programme)
        {
            using var context = new AppDbContext();
            context.Set<ProgrammePDL>().Update(programme);
            await context.SaveChangesAsync();
            return programme;
        }

        /// <summary>
        /// Supprime un programme
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var programme = await context.Set<ProgrammePDL>().FindAsync(id);
                if (programme == null) return false;

                context.Set<ProgrammePDL>().Remove(programme);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync ProgrammePDL: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vérifie si un programme existe
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<ProgrammePDL>().AnyAsync(p => p.Id == id);
        }

        /// <summary>
        /// Compte le nombre de secteurs d'un programme
        /// </summary>
        public async Task<int> GetSecteursCountAsync(int programmeId)
        {
            using var context = new AppDbContext();
            return await context.Set<SecteurPDL>()
                .CountAsync(s => s.ProgrammePDLId == programmeId);
        }
    }
}