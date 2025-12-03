using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class UserService
    {
        private AppDbContext CreateContext() => new AppDbContext();

        public async Task<List<User>> GetAllAsync()
        {
            using var context = CreateContext();
            return await context.Users
                .Include(u => u.Commune)
                .Include(u => u.Role)
                .OrderBy(u => u.Username)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, User? User)> CreateAsync(User user)
        {
            using var context = CreateContext();

            if (await context.Users.AnyAsync(u => u.Username == user.Username))
            {
                return (false, "Ce nom d'utilisateur est déjà utilisé.", null);
            }

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var created = await context.Users
                .Include(u => u.Commune)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            return (true, "Utilisateur créé avec succès.", created);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(User user)
        {
            using var context = CreateContext();
            var existing = await context.Users.FindAsync(user.Id);

            if (existing == null)
            {
                return (false, "Utilisateur introuvable.");
            }

            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.Tel = user.Tel;
            existing.Password = user.Password;
            existing.CommuneId = user.CommuneId;
            existing.RoleId = user.RoleId;

            await context.SaveChangesAsync();
            return (true, "Utilisateur mis à jour avec succès.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int userId)
        {
            using var context = CreateContext();
            var user = await context.Users.FindAsync(userId);

            if (user == null)
            {
                return (false, "Utilisateur introuvable.");
            }

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            return (true, "Utilisateur supprimé avec succès.");
        }

        public async Task<(bool Success, string Message)> UpdateRoleAsync(int userId, int roleId)
        {
            using var context = CreateContext();
            var user = await context.Users.FindAsync(userId);

            if (user == null)
            {
                return (false, "Utilisateur introuvable.");
            }

            var roleExists = await context.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                return (false, "Rôle sélectionné introuvable.");
            }

            user.RoleId = roleId;
            await context.SaveChangesAsync();

            return (true, "Rôle mis à jour avec succès.");
        }
    }
}

