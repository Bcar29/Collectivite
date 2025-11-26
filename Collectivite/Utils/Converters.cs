using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertit une chaîne de couleur hexadécimale en SolidColorBrush
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
                }
                catch
                {
                    return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une valeur null en Visibility.Collapsed, sinon Visibility.Visible
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse d'un BooleanToVisibilityConverter
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Formate un montant en GNF
    /// </summary>
    public class CurrencyFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return $"{decimalValue:N0} GNF";
            }
            if (value is double doubleValue)
            {
                return $"{doubleValue:N0} GNF";
            }
            if (value is int intValue)
            {
                return $"{intValue:N0} GNF";
            }
            return "0 GNF";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Affiche si la chaîne n'est pas null/empty, sinon Collapsed
    /// </summary>
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit une variation (decimal/double/int) en couleur (vert, rouge, gris)
    /// </summary>
    public class VariationToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var variation = ToDecimal(value);

            if (variation > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#388E3C"));
            if (variation < 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#546E7A"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static decimal ToDecimal(object value)
        {
            return value switch
            {
                decimal d => d,
                double dbl => (decimal)dbl,
                int i => i,
                _ => 0m
            };
        }
    }

    /// <summary>
    /// Convertit une variation en libellé (Augmentation / Diminution / Équilibré)
    /// </summary>
    public class VariationToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var variation = value switch
            {
                decimal d => d,
                double dbl => (decimal)dbl,
                int i => i,
                _ => 0m
            };

            if (variation > 0) return "Augmentation";
            if (variation < 0) return "Diminution";
            return "Équilibré";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
