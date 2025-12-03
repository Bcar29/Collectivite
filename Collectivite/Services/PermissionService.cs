using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class PermissionService
    {
        private AppDbContext CreateContext() => new AppDbContext();

        public async Task<List<Permission>> GetAllAsync()
        {
            using var context = CreateContext();
            return await context.Permissions
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, Permission? Permission)> CreateAsync(Permission permission)
        {
            using var context = CreateContext();

            if (await context.Permissions.AnyAsync(p => p.Code == permission.Code))
            {
                return (false, "Une permission avec ce code existe déjà.", null);
            }

            context.Permissions.Add(permission);
            await context.SaveChangesAsync();
            return (true, "Permission créée avec succès.", permission);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Permission permission)
        {
            using var context = CreateContext();
            var existing = await context.Permissions.FindAsync(permission.Id);

            if (existing == null)
            {
                return (false, "Permission introuvable.");
            }

            if (await context.Permissions.AnyAsync(p => p.Code == permission.Code && p.Id != permission.Id))
            {
                return (false, "Un autre enregistrement possède déjà ce code.");
            }

            existing.Name = permission.Name;
            existing.Code = permission.Code;
            existing.Description = permission.Description;

            await context.SaveChangesAsync();
            return (true, "Permission mise à jour avec succès.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int permissionId)
        {
            using var context = CreateContext();
            var permission = await context.Permissions
                .Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.Id == permissionId);

            if (permission == null)
            {
                return (false, "Permission introuvable.");
            }

            if (permission.RolePermissions.Any())
            {
                return (false, "Impossible de supprimer une permission utilisée par un rôle.");
            }

            context.Permissions.Remove(permission);
            await context.SaveChangesAsync();

            return (true, "Permission supprimée avec succès.");
        }
    }
}

