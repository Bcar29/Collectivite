using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private User? _currentUser;
        private readonly HashSet<string> _currentPermissions = new(StringComparer.OrdinalIgnoreCase);

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public User? CurrentUser => _currentUser;
        public IReadOnlyCollection<string> CurrentPermissions => _currentPermissions;
        public string? CurrentRoleName => _currentUser?.Role?.Name;

        public async Task<(bool Success, string Message, User? User)> AuthenticateAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Commune)
                    .Include(u => u.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return (false, "Nom d'utilisateur ou mot de passe incorrect.", null);
                }

                // Note: En production, utilisez un hash (BCrypt, PBKDF2, etc.)
                if (user.Password != password)
                {
                    return (false, "Nom d'utilisateur ou mot de passe incorrect.", null);
                }

                _currentUser = user;
                HydratePermissions(user);

                return (true, "Connexion réussie!", user);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur de connexion: {ex.Message}", null);
            }
        }

        public void Logout()
        {
            _currentUser = null;
            _currentPermissions.Clear();
        }

        public bool HasPermission(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return _currentPermissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
        }

        private void HydratePermissions(User user)
        {
            _currentPermissions.Clear();

            if (user.Role?.RolePermissions == null)
                return;

            foreach (var permission in user.Role.RolePermissions
                         .Select(rp => rp.Permission?.Code)
                         .Where(code => !string.IsNullOrWhiteSpace(code)))
            {
                _currentPermissions.Add(permission!);
            }
        }
    }
}
