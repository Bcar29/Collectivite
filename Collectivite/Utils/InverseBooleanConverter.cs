using System;
using System.Globalization;
using System.Windows.Data;

namespace Collectivite.Utils
{
    /// <summary>
    /// Converter pour inverser une valeur booléenne
    /// Utilisé pour IsEnabled avec EstCloture
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}