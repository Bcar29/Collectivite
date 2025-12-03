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

    // ═══════════════════════════════════════════════════════════════════════
    // NOUVEAUX CONVERTERS (ajoutés pour la gestion des nomenclatures)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Convertit un enum en boolean pour les RadioButtons
    /// Utilisé pour lier des RadioButtons à des propriétés enum (Nature, Section)
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            if ((bool)value)
                return Enum.Parse(targetType, parameter.ToString());

            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Inverse un boolean (pour les bindings inverses)
    /// Utilisé pour IsSaisieLibreMode = !IsNommenclatureMode
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }
    }

    /// <summary>
    /// Convertit un boolean en couleur (pour le background conditionnel)
    /// Format du paramètre: "ColorIfTrue|ColorIfFalse"
    /// Exemple: "#F5F5F5|White"
    /// Utilisé pour griser le champ intitulé en mode lecture seule
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorArray = colors.Split('|');
                if (colorArray.Length == 2)
                {
                    var colorString = boolValue ? colorArray[0] : colorArray[1];
                    return System.Windows.Media.ColorConverter.ConvertFromString(colorString);
                }
            }

            return System.Windows.Media.Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}