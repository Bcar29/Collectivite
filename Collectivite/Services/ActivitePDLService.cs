using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public interface IActivitePDLService
    {
        Task<List<ActivitePDL>> GetAllAsync();
        Task<List<ActivitePDL>> GetByPDLIdAsync(int pdlId);
        Task<List<ActivitePDL>> GetBySecteurIdAsync(int secteurId);
        Task<ActivitePDL?> GetByIdAsync(int id);
        Task<ActivitePDL?> GetByIdWithDetailsAsync(int id);
        Task<ActivitePDL> CreateAsync(ActivitePDL activite);
        Task<ActivitePDL> UpdateAsync(ActivitePDL activite);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Gestion des relations N-N
        Task<bool> AddBeneficiaireAsync(int activiteId, int beneficiaireId);
        Task<bool> RemoveBeneficiaireAsync(int activiteId, int beneficiaireId);
        Task<bool> AddActeurAsync(int activiteId, int acteurId);
        Task<bool> RemoveActeurAsync(int activiteId, int acteurId);
        Task<bool> AddStructureExecutionAsync(int activiteId, int structureId);
        Task<bool> RemoveStructureExecutionAsync(int activiteId, int structureId);

        // Mise à jour complète des relations N-N
        Task<bool> UpdateBeneficiairesAsync(int activiteId, List<int> beneficiaireIds);
        Task<bool> UpdateActeursAsync(int activiteId, List<int> acteurIds);
        Task<bool> UpdateStructuresAsync(int activiteId, List<int> structureIds);
    }

    public class ActivitePDLService : IActivitePDLService
    {
        /// <summary>
        /// Récupère toutes les activités PDL
        /// </summary>
        public async Task<List<ActivitePDL>> GetAllAsync()
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ActivitePDL>()
                    .Include(a => a.PDL)
                    .Include(a => a.SecteurPDL)
                        .ThenInclude(s => s!.ProgrammePDL)
                    .Include(a => a.CompetenceCollectivite)
                    .Include(a => a.ODD)
                    .Include(a => a.Beneficiaires)
                    .Include(a => a.Acteurs)
                    .Include(a => a.StructureExecutions)
                    .OrderBy(a => a.DateDebut)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetAllAsync ActivitePDL: {ex.Message}");
                return new List<ActivitePDL>();
            }
        }

        /// <summary>
        /// Récupère les activités d'un PDL spécifique
        /// </summary>
        public async Task<List<ActivitePDL>> GetByPDLIdAsync(int pdlId)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ActivitePDL>()
                    .Where(a => a.PDLId == pdlId)
                    .Include(a => a.SecteurPDL)
                        .ThenInclude(s => s!.ProgrammePDL)
                    .Include(a => a.CompetenceCollectivite)
                    .Include(a => a.ODD)
                    .Include(a => a.Beneficiaires)
                    .Include(a => a.Acteurs)
                    .Include(a => a.StructureExecutions)
                    .OrderBy(a => a.DateDebut)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByPDLIdAsync: {ex.Message}");
                return new List<ActivitePDL>();
            }
        }

        /// <summary>
        /// Récupère les activités d'un secteur spécifique
        /// </summary>
        public async Task<List<ActivitePDL>> GetBySecteurIdAsync(int secteurId)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ActivitePDL>()
                    .Where(a => a.SecteurPDLId == secteurId)
                    .Include(a => a.PDL)
                    .Include(a => a.SecteurPDL)
                        .ThenInclude(s => s!.ProgrammePDL)
                    .Include(a => a.CompetenceCollectivite)
                    .Include(a => a.ODD)
                    .Include(a => a.Beneficiaires)
                    .Include(a => a.Acteurs)
                    .Include(a => a.StructureExecutions)
                    .OrderBy(a => a.DateDebut)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetBySecteurIdAsync: {ex.Message}");
                return new List<ActivitePDL>();
            }
        }

        /// <summary>
        /// Récupère une activité par son ID
        /// </summary>
        public async Task<ActivitePDL?> GetByIdAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ActivitePDL>()
                    .Include(a => a.PDL)
                    .Include(a => a.SecteurPDL)
                        .ThenInclude(s => s!.ProgrammePDL)
                    .Include(a => a.CompetenceCollectivite)
                    .Include(a => a.ODD)
                    .FirstOrDefaultAsync(a => a.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByIdAsync ActivitePDL: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère une activité avec toutes ses relations (y compris N-N)
        /// </summary>
        public async Task<ActivitePDL?> GetByIdWithDetailsAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                return await context.Set<ActivitePDL>()
                    .Include(a => a.PDL)
                    .Include(a => a.SecteurPDL)
                        .ThenInclude(s => s!.ProgrammePDL)
                    .Include(a => a.CompetenceCollectivite)
                    .Include(a => a.ODD)
                    .Include(a => a.Beneficiaires)
                    .Include(a => a.Acteurs)
                    .Include(a => a.StructureExecutions)
                    .FirstOrDefaultAsync(a => a.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetByIdWithDetailsAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crée une nouvelle activité
        /// </summary>
        public async Task<ActivitePDL> CreateAsync(ActivitePDL activite)
        {
            using var context = new AppDbContext();
            context.Set<ActivitePDL>().Add(activite);
            await context.SaveChangesAsync();
            return activite;
        }

        /// <summary>
        /// Met à jour une activité existante
        /// </summary>
        public async Task<ActivitePDL> UpdateAsync(ActivitePDL activite)
        {
            using var context = new AppDbContext();
            context.Set<ActivitePDL>().Update(activite);
            await context.SaveChangesAsync();
            return activite;
        }

        /// <summary>
        /// Supprime une activité
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>().FindAsync(id);
                if (activite == null) return false;

                context.Set<ActivitePDL>().Remove(activite);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAsync ActivitePDL: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vérifie si une activité existe
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            using var context = new AppDbContext();
            return await context.Set<ActivitePDL>().AnyAsync(a => a.Id == id);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // GESTION DES RELATIONS N-N : BÉNÉFICIAIRES
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<bool> AddBeneficiaireAsync(int activiteId, int beneficiaireId)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.Beneficiaires)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite == null) return false;

                var beneficiaire = await context.Set<BeneficiairePDL>().FindAsync(beneficiaireId);
                if (beneficiaire == null) return false;

                if (activite.Beneficiaires == null) activite.Beneficiaires = new List<BeneficiairePDL>();
                if (!activite.Beneficiaires.Any(b => b.Id == beneficiaireId))
                {
                    activite.Beneficiaires.Add(beneficiaire);
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur AddBeneficiaireAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveBeneficiaireAsync(int activiteId, int beneficiaireId)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.Beneficiaires)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite?.Beneficiaires == null) return false;

                var beneficiaire = activite.Beneficiaires.FirstOrDefault(b => b.Id == beneficiaireId);
                if (beneficiaire != null)
                {
                    activite.Beneficiaires.Remove(beneficiaire);
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur RemoveBeneficiaireAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateBeneficiairesAsync(int activiteId, List<int> beneficiaireIds)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.Beneficiaires)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite == null) return false;

                // Charger les bénéficiaires sélectionnés
                var beneficiaires = await context.Set<BeneficiairePDL>()
                    .Where(b => beneficiaireIds.Contains(b.Id))
                    .ToListAsync();

                // Remplacer la collection
                activite.Beneficiaires = beneficiaires;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur UpdateBeneficiairesAsync: {ex.Message}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // GESTION DES RELATIONS N-N : ACTEURS
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<bool> AddActeurAsync(int activiteId, int acteurId)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.Acteurs)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite == null) return false;

                var acteur = await context.Set<ActeurPDL>().FindAsync(acteurId);
                if (acteur == null) return false;

                if (activite.Acteurs == null) activite.Acteurs = new List<ActeurPDL>();
                if (!activite.Acteurs.Any(a => a.Id == acteurId))
                {
                    activite.Acteurs.Add(acteur);
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur AddActeurAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveActeurAsync(int activiteId, int acteurId)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.Acteurs)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite?.Acteurs == null) return false;

                var acteur = activite.Acteurs.FirstOrDefault(a => a.Id == acteurId);
                if (acteur != null)
                {
                    activite.Acteurs.Remove(acteur);
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur RemoveActeurAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateActeursAsync(int activiteId, List<int> acteurIds)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.Acteurs)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite == null) return false;

                var acteurs = await context.Set<ActeurPDL>()
                    .Where(a => acteurIds.Contains(a.Id))
                    .ToListAsync();

                activite.Acteurs = acteurs;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur UpdateActeursAsync: {ex.Message}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // GESTION DES RELATIONS N-N : STRUCTURES D'EXÉCUTION
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<bool> AddStructureExecutionAsync(int activiteId, int structureId)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.StructureExecutions)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite == null) return false;

                var structure = await context.Set<StructureExecutionPDL>().FindAsync(structureId);
                if (structure == null) return false;

                if (activite.StructureExecutions == null) activite.StructureExecutions = new List<StructureExecutionPDL>();
                if (!activite.StructureExecutions.Any(s => s.Id == structureId))
                {
                    activite.StructureExecutions.Add(structure);
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur AddStructureExecutionAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveStructureExecutionAsync(int activiteId, int structureId)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.StructureExecutions)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite?.StructureExecutions == null) return false;

                var structure = activite.StructureExecutions.FirstOrDefault(s => s.Id == structureId);
                if (structure != null)
                {
                    activite.StructureExecutions.Remove(structure);
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur RemoveStructureExecutionAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateStructuresAsync(int activiteId, List<int> structureIds)
        {
            try
            {
                using var context = new AppDbContext();
                var activite = await context.Set<ActivitePDL>()
                    .Include(a => a.StructureExecutions)
                    .FirstOrDefaultAsync(a => a.Id == activiteId);

                if (activite == null) return false;

                var structures = await context.Set<StructureExecutionPDL>()
                    .Where(s => structureIds.Contains(s.Id))
                    .ToListAsync();

                activite.StructureExecutions = structures;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur UpdateStructuresAsync: {ex.Message}");
                return false;
            }
        }
    }
}