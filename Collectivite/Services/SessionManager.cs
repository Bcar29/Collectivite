using Collectivite.Models;

namespace Collectivite.Services
{
    public static class SessionManager
    {
        private static AuthService? _authService;

        public static AuthService AuthService
        {
            get
            {
                if (_authService == null)
                {
                    var context = new AppDbContext();
                    _authService = new AuthService(context);
                }

                return _authService;
            }
        }

        public static User? CurrentUser => AuthService.CurrentUser;

        public static void Reset()
        {
            _authService?.Logout();
        }
    }
}

