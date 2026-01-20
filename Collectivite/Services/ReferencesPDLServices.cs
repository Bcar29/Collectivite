using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    // ═══════════════════════════════════════════════════════════════════════════
    // SERVICE BÉNÉFICIAIRES PDL
    // ═══════════════════════════════════════════════════════════════════════════

    public interface IBeneficiairePDLService
    {
        Task<List<BeneficiairePDL>> GetAllAsync();
        Task<BeneficiairePDL?> GetByIdAsync(int id);
        Task<BeneficiairePDL> CreateAsync(BeneficiairePDL beneficiaire);
        Task<BeneficiairePDL> UpdateAsync(BeneficiairePDL beneficiaire);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class BeneficiairePDLService : IBeneficiairePDLService
    {
        public async Task<List<BeneficiairePDL>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<BeneficiairePDL>()
                    .OrderBy(b => b.Nom)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync BeneficiairePDL: {ex.Message}");
                return new List<BeneficiairePDL>();
            }
        }

        public async Task<BeneficiairePDL?> GetByIdAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<BeneficiairePDL>().FindAsync(id);
        }

        public async Task<BeneficiairePDL> CreateAsync(BeneficiairePDL beneficiaire)
        {
            using var context = new AppDbContext();
            context.Set<BeneficiairePDL>().Add(beneficiaire);
            await context.SaveChangesAsync();
            return beneficiaire;
        }

        public async Task<BeneficiairePDL> UpdateAsync(BeneficiairePDL beneficiaire)
        {
            using var context = new AppDbContext();
            context.Set<BeneficiairePDL>().Update(beneficiaire);
            await context.SaveChangesAsync();
            return beneficiaire;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var entity = await context.Set<BeneficiairePDL>().FindAsync(id);
                if (entity == null) return false;
                context.Set<BeneficiairePDL>().Remove(entity);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync BeneficiairePDL: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<BeneficiairePDL>().AnyAsync(b => b.Id == id);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SERVICE ACTEURS PDL
    // ═══════════════════════════════════════════════════════════════════════════

    public interface IActeurPDLService
    {
        Task<List<ActeurPDL>> GetAllAsync();
        Task<ActeurPDL?> GetByIdAsync(int id);
        Task<ActeurPDL> CreateAsync(ActeurPDL acteur);
        Task<ActeurPDL> UpdateAsync(ActeurPDL acteur);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class ActeurPDLService : IActeurPDLService
    {
        public async Task<List<ActeurPDL>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ActeurPDL>()
                    .OrderBy(a => a.Nom)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync ActeurPDL: {ex.Message}");
                return new List<ActeurPDL>();
            }
        }

        public async Task<ActeurPDL?> GetByIdAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<ActeurPDL>().FindAsync(id);
        }

        public async Task<ActeurPDL> CreateAsync(ActeurPDL acteur)
        {
            using var context = new AppDbContext();
            context.Set<ActeurPDL>().Add(acteur);
            await context.SaveChangesAsync();
            return acteur;
        }

        public async Task<ActeurPDL> UpdateAsync(ActeurPDL acteur)
        {
            using var context = new AppDbContext();
            context.Set<ActeurPDL>().Update(acteur);
            await context.SaveChangesAsync();
            return acteur;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var entity = await context.Set<ActeurPDL>().FindAsync(id);
                if (entity == null) return false;
                context.Set<ActeurPDL>().Remove(entity);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync ActeurPDL: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<ActeurPDL>().AnyAsync(a => a.Id == id);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SERVICE STRUCTURES D'EXÉCUTION PDL
    // ═══════════════════════════════════════════════════════════════════════════

    public interface IStructureExecutionPDLService
    {
        Task<List<StructureExecutionPDL>> GetAllAsync();
        Task<StructureExecutionPDL?> GetByIdAsync(int id);
        Task<StructureExecutionPDL> CreateAsync(StructureExecutionPDL structure);
        Task<StructureExecutionPDL> UpdateAsync(StructureExecutionPDL structure);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class StructureExecutionPDLService : IStructureExecutionPDLService
    {
        public async Task<List<StructureExecutionPDL>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<StructureExecutionPDL>()
                    .OrderBy(s => s.Nom)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync StructureExecutionPDL: {ex.Message}");
                return new List<StructureExecutionPDL>();
            }
        }

        public async Task<StructureExecutionPDL?> GetByIdAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<StructureExecutionPDL>().FindAsync(id);
        }

        public async Task<StructureExecutionPDL> CreateAsync(StructureExecutionPDL structure)
        {
            using var context = new AppDbContext();
            context.Set<StructureExecutionPDL>().Add(structure);
            await context.SaveChangesAsync();
            return structure;
        }

        public async Task<StructureExecutionPDL> UpdateAsync(StructureExecutionPDL structure)
        {
            using var context = new AppDbContext();
            context.Set<StructureExecutionPDL>().Update(structure);
            await context.SaveChangesAsync();
            return structure;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var entity = await context.Set<StructureExecutionPDL>().FindAsync(id);
                if (entity == null) return false;
                context.Set<StructureExecutionPDL>().Remove(entity);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync StructureExecutionPDL: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<StructureExecutionPDL>().AnyAsync(s => s.Id == id);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SERVICE ODD (Objectifs de Développement Durable)
    // ═══════════════════════════════════════════════════════════════════════════

    public interface IODDService
    {
        Task<List<ODD>> GetAllAsync();
        Task<ODD?> GetByIdAsync(int id);
        Task<ODD> CreateAsync(ODD odd);
        Task<ODD> UpdateAsync(ODD odd);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class ODDService : IODDService
    {
        public async Task<List<ODD>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ODD>()
                    .OrderBy(o => o.Numero)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync ODD: {ex.Message}");
                return new List<ODD>();
            }
        }

        public async Task<ODD?> GetByIdAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<ODD>().FindAsync(id);
        }

        public async Task<ODD> CreateAsync(ODD odd)
        {
            using var context = new AppDbContext();
            context.Set<ODD>().Add(odd);
            await context.SaveChangesAsync();
            return odd;
        }

        public async Task<ODD> UpdateAsync(ODD odd)
        {
            using var context = new AppDbContext();
            context.Set<ODD>().Update(odd);
            await context.SaveChangesAsync();
            return odd;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var entity = await context.Set<ODD>().FindAsync(id);
                if (entity == null) return false;
                context.Set<ODD>().Remove(entity);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync ODD: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<ODD>().AnyAsync(o => o.Id == id);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SERVICE COMPÉTENCES COLLECTIVITÉ
    // ═══════════════════════════════════════════════════════════════════════════

    public interface ICompetenceCollectiviteService
    {
        Task<List<CompetenceCollectivite>> GetAllAsync();
        Task<CompetenceCollectivite?> GetByIdAsync(int id);
        Task<CompetenceCollectivite> CreateAsync(CompetenceCollectivite competence);
        Task<CompetenceCollectivite> UpdateAsync(CompetenceCollectivite competence);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class CompetenceCollectiviteService : ICompetenceCollectiviteService
    {
        public async Task<List<CompetenceCollectivite>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<CompetenceCollectivite>()
                    .OrderBy(c => c.Numero)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync CompetenceCollectivite: {ex.Message}");
                return new List<CompetenceCollectivite>();
            }
        }

        public async Task<CompetenceCollectivite?> GetByIdAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<CompetenceCollectivite>().FindAsync(id);
        }

        public async Task<CompetenceCollectivite> CreateAsync(CompetenceCollectivite competence)
        {
            using var context = new AppDbContext();
            context.Set<CompetenceCollectivite>().Add(competence);
            await context.SaveChangesAsync();
            return competence;
        }

        public async Task<CompetenceCollectivite> UpdateAsync(CompetenceCollectivite competence)
        {
            using var context = new AppDbContext();
            context.Set<CompetenceCollectivite>().Update(competence);
            await context.SaveChangesAsync();
            return competence;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var entity = await context.Set<CompetenceCollectivite>().FindAsync(id);
                if (entity == null) return false;
                context.Set<CompetenceCollectivite>().Remove(entity);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync CompetenceCollectivite: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<CompetenceCollectivite>().AnyAsync(c => c.Id == id);
        }
    }
}