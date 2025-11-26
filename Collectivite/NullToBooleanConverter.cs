using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Collectivite
{
    public class NullToBooleanConverter : IValueConverter
    {
        // If true, the boolean result is inverted
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = value != null;
            if (Invert)
            {
                result = !result;
            }

            // If target is Visibility, return Visible/Collapsed
            if (targetType == typeof(Visibility))
            {
                return result ? Visibility.Visible : Visibility.Collapsed;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
