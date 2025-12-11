using Collectivite.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertit StatutMandat en texte
    /// </summary>
    public class StatutMandatToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Mandat.StatutMandat statut)
            {
                return statut switch
                {
                    Mandat.StatutMandat.Non_Payé => "Non payé",
                    Mandat.StatutMandat.Partiel => "Partiel",
                    Mandat.StatutMandat.Payé => "Payé",
                    _ => "Inconnu"
                };
            }
            return "Inconnu";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit StatutMandat en couleur de texte
    /// </summary>
    public class StatutMandatToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Mandat.StatutMandat statut)
            {
                return statut switch
                {
                    Mandat.StatutMandat.Non_Payé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    Mandat.StatutMandat.Partiel => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    Mandat.StatutMandat.Payé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit StatutMandat en couleur de fond
    /// </summary>
    public class StatutMandatToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Mandat.StatutMandat statut)
            {
                return statut switch
                {
                    Mandat.StatutMandat.Non_Payé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE")),
                    Mandat.StatutMandat.Partiel => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")),
                    Mandat.StatutMandat.Payé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
                };
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}