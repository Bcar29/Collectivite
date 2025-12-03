using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class RoleService
    {
        private AppDbContext CreateContext() => new AppDbContext();

        public async Task<List<Role>> GetAllAsync()
        {
            using var context = CreateContext();
            return await context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, Role? Role)> CreateAsync(Role role)
        {
            using var context = CreateContext();

            if (await context.Roles.AnyAsync(r => r.Name == role.Name))
            {
                return (false, "Ce rôle existe déjà.", null);
            }

            context.Roles.Add(role);
            await context.SaveChangesAsync();
            return (true, "Rôle créé avec succès.", role);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Role role)
        {
            using var context = CreateContext();
            var existing = await context.Roles.FindAsync(role.Id);

            if (existing == null)
            {
                return (false, "Rôle introuvable.");
            }

            if (await context.Roles.AnyAsync(r => r.Name == role.Name && r.Id != role.Id))
            {
                return (false, "Un autre rôle porte déjà ce nom.");
            }

            existing.Name = role.Name;
            existing.Description = role.Description;
            existing.IsActive = role.IsActive;
            existing.UpdatedAt = System.DateTime.UtcNow;

            await context.SaveChangesAsync();
            return (true, "Rôle mis à jour avec succès.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int roleId)
        {
            using var context = CreateContext();
            var role = await context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return (false, "Rôle introuvable.");
            }

            if (role.Users.Any())
            {
                return (false, "Impossible de supprimer un rôle assigné à des utilisateurs.");
            }

            context.Roles.Remove(role);
            await context.SaveChangesAsync();

            return (true, "Rôle supprimé avec succès.");
        }

        public async Task<(bool Success, string Message, Role? Role)> UpdateRolePermissionsAsync(
            int roleId,
            IEnumerable<int> permissionIds)
        {
            using var context = CreateContext();

            var role = await context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return (false, "Rôle introuvable.", null);
            }

            var distinctPermissionIds = permissionIds.Distinct().ToList();
            var existingPermissions = await context.Permissions
                .Where(p => distinctPermissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            var rolePermissions = role.RolePermissions?.ToList() ?? new List<RolePermission>();
            context.RolePermissions.RemoveRange(rolePermissions);

            foreach (var pid in existingPermissions)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = pid
                });
            }

            await context.SaveChangesAsync();

            var reloadedRole = await context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            return (true, "Permissions mises à jour.", reloadedRole);
        }
    }
}

