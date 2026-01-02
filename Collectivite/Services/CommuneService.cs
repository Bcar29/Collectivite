using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class CommuneService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }


        #region READ (sans audit)

        public async Task<Commune?> GetCommuneByIdWithRelationsAsync(int id)
        {
            using var context = CreateContext();

            return await context.Communes
                .AsNoTracking()
                .Include(c => c.DetailCommunes)
                .Include(c => c.Users)
                .Include(c => c.Engagements)
                .Include(c => c.Recensements)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Commune>> GetAllCommuneAsync()
        {
            if (!SessionManager.HasPermission("Commune.View"))
                throw new UnauthorizedAccessException("Permission Commune.View requise.");

            using var context = CreateContext();

            return await context.Communes
                .AsNoTracking()
                .OrderBy(c => c.Nom)
                .ToListAsync();
        }

        #endregion

        #region CREATE

        public async Task<(bool Succes, string Message, Commune? Commune)>
            CreateCommuneAsync(Commune commune)
        {
            if (!SessionManager.HasPermission("Commune.Create"))
                return (false, "Permission Commune.Create requise.", null);

            try
            {
                using var context = CreateContext();

                var existe = await context.Communes
                    .AnyAsync(c => c.Nom == commune.Nom);

                if (existe)
                    return (false, $"{commune.Nom} existe déjà.", null);

                context.Communes.Add(commune);
                await context.SaveChangesAsync();

                await AuditService.Instance.LogAsync(
                    "Création Commune",
                    $"Commune créée | ID: {commune.Id} | Nom: {commune.Nom}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, $"Commune {commune.Nom} ajoutée avec succès.", commune);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur création commune : {ex.Message}", null);
            }
        }

        #endregion

        #region UPDATE

        public async Task<(bool Succes, string Message)> UpdateCommuneAsync(Commune commune)
        {
            if (!SessionManager.HasPermission("Commune.Edit"))
                return (false, "Permission Commune.Edit requise.");

            try
            {
                using var context = CreateContext();

                var existingCommune = await context.Communes
                    .FirstOrDefaultAsync(c => c.Id == commune.Id);

                if (existingCommune == null)
                    return (false, "Commune non trouvée.");

                var existe = await context.Communes
                    .AnyAsync(c => c.Nom == commune.Nom && c.Id != commune.Id);

                if (existe)
                    return (false, $"{commune.Nom} existe déjà.");

                existingCommune.Nom = commune.Nom;
                existingCommune.CommuneType = commune.CommuneType;
                existingCommune.Region = commune.Region;
                existingCommune.Prefecture = commune.Prefecture;
                existingCommune.DateCreation = commune.DateCreation;
                existingCommune.DistanceChefLieuRegion = commune.DistanceChefLieuRegion;
                existingCommune.DistanceChefLieuProvince = commune.DistanceChefLieuProvince;
                existingCommune.DistanceCapitale = commune.DistanceCapitale;

                await context.SaveChangesAsync();

                await AuditService.Instance.LogAsync(
                    "Modification Commune",
                    $"Commune modifiée | ID: {existingCommune.Id} | Nom: {existingCommune.Nom}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, "Commune mise à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur mise à jour commune : {ex.Message}");
            }
        }

        #endregion

        #region DELETE

        public async Task<(bool Succes, string Message)> DeleteCommuneAsync(int communeId)
        {
            if (!SessionManager.HasPermission("Commune.Delete"))
                return (false, "Permission Commune.Delete requise.");

            try
            {
                using var context = CreateContext();

                var commune = await context.Communes
                    .FirstOrDefaultAsync(c => c.Id == communeId);

                if (commune == null)
                    return (false, "Commune non trouvée.");

                context.Communes.Remove(commune);
                await context.SaveChangesAsync();

                await AuditService.Instance.LogAsync(
                    "Suppression Commune",
                    $"Commune supprimée | ID: {commune.Id} | Nom: {commune.Nom}",
                    SessionManager.CurrentUser?.Username ?? "Utilisateur Inconnu");

                return (true, "Commune supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur suppression commune : {ex.Message}");
            }
        }

        #endregion
    }
}
