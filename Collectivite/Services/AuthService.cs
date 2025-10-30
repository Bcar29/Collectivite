using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private User? _currentUser;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public User? CurrentUser => _currentUser;

        public async Task<(bool Success, string Message, User? User)> AuthenticateAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Commune)
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
        }
    }
}
