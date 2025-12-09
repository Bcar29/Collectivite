using Collectivite.Models;
using Collectivite.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Collectivite.Utils
{
    public static class SeedRolesPermissions
    {
        private static readonly Permission[] DefaultPermissions =
        {
            new Permission { Name = "Approuver le budget", Code = "Budget.Approve", Description = "Permet d'approuver le budget primitif." },
            new Permission { Name = "Valider le budget", Code = "Budget.Validate", Description = "Permet de valider le budget primitif." },
            new Permission { Name = "Gérer les remaniements", Code = "Remaniement.Manage", Description = "Permet d'ajouter/supprimer des remaniements." },
            new Permission { Name = "Gérer les finances", Code = "Finance.Manage", Description = "Accès aux écrans financiers (mandats, recettes, etc.)." },
            new Permission { Name = "Enregistrer les courriers", Code = "Courrier.Register", Description = "Permet l'enregistrement des courriers entrants/sortants." }
        };

        private static readonly Dictionary<string, string[]> RolePermissions = new()
        {
            { "Maire", new[] { "Budget.Approve", "Budget.Validate", "Remaniement.Manage" } },
            { "Secrétaire Général", new[] { "Courrier.Register", "Remaniement.Manage" } },
            { "Receveur", new[] { "Finance.Manage", "Remaniement.Manage" } }
        };

        public static void Seed(AppDbContext db)
        {
            //db.Database.Migrate();

            // Permissions
            foreach (var permission in DefaultPermissions)
            {
                if (!db.Permissions.Any(p => p.Code == permission.Code))
                {
                    db.Permissions.Add(new Permission
                    {
                        Name = permission.Name,
                        Code = permission.Code,
                        Description = permission.Description
                    });
                }
            }

            db.SaveChanges();

            // Roles + assignments
            foreach (var roleEntry in RolePermissions)
            {
                var role = db.Roles.Include(r => r.RolePermissions)
                                   .FirstOrDefault(r => r.Name == roleEntry.Key);

                if (role == null)
                {
                    role = new Role
                    {
                        Name = roleEntry.Key,
                        Description = $"Rôle par défaut : {roleEntry.Key}",
                        IsActive = true
                    };
                    db.Roles.Add(role);
                    db.SaveChanges();
                    role = db.Roles.Include(r => r.RolePermissions).First(r => r.Name == roleEntry.Key);
                }

                var permissionCodes = roleEntry.Value;
                var permissionIds = db.Permissions
                    .Where(p => permissionCodes.Contains(p.Code))
                    .Select(p => p.Id)
                    .ToList();

                // Nettoyer puis réinsérer
                if (role.RolePermissions.Any())
                {
                    db.RolePermissions.RemoveRange(role.RolePermissions);
                }

                foreach (var pid in permissionIds)
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = pid
                    });
                }

                db.SaveChanges();
            }
        }
    }
}

