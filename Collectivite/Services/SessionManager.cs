using Collectivite.Models;
using System.Collections.Generic;

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
                    _authService = new AuthService();
                }

                return _authService;
            }
        }

        public static User? CurrentUser => AuthService.CurrentUser;
        public static IReadOnlyCollection<string> CurrentPermissions => AuthService.CurrentPermissions;
        public static string? CurrentRoleName => AuthService.CurrentRoleName;

        public static void Reset()
        {
            _authService?.Logout();
        }

        public static bool HasPermission(string permissionCode) => AuthService.HasPermission(permissionCode);
    }
}

