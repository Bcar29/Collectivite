using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Collectivite.Services;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertisseur qui convertit une permission en visibilité.
    /// Retourne Visible si l'utilisateur a la permission, sinon Collapsed.
    /// </summary>
    public class PermissionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Le paramètre doit être le code de permission à vérifier
            if (parameter is not string permissionCode || string.IsNullOrWhiteSpace(permissionCode))
            {
                return Visibility.Collapsed;
            }

            // Utiliser SessionManager pour vérifier la permission
            bool hasPermission = SessionManager.HasPermission(permissionCode);

            return hasPermission ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
