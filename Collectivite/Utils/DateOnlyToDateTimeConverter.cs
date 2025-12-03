using System;
using System.Globalization;
using System.Windows.Data;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertisseur pour DateOnly <-> DateTime pour les DatePicker WPF
    /// </summary>
    public class DateOnlyToDateTimeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly dateOnly)
            {
                // DateOnly → DateTime
                return dateOnly.ToDateTime(TimeOnly.MinValue);
            }

            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                // DateTime → DateOnly
                return DateOnly.FromDateTime(dateTime);
            }

            return null;
        }
    }
}