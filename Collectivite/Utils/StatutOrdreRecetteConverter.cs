using Collectivite.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertit StatutOrdre en texte
    /// </summary>
    public class StatutOrdreToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrdreRecette.StatutOrdre statut)
            {
                return statut switch
                {
                    OrdreRecette.StatutOrdre.Non_Encaissé => "Non Encaissé",
                    OrdreRecette.StatutOrdre.Partiel => "Partiel",
                    OrdreRecette.StatutOrdre.Enciassé => "Encaissé",
                    _ => "Inconnu"
                };
            }
            return "Inconnu";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit StatutOrdre en couleur de texte
    /// </summary>
    public class StatutOrdreToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrdreRecette.StatutOrdre statut)
            {
                return statut switch
                {
                    OrdreRecette.StatutOrdre.Non_Encaissé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    OrdreRecette.StatutOrdre.Partiel => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    OrdreRecette.StatutOrdre.Enciassé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit StatutOrdre en couleur de fond
    /// </summary>
    public class StatutOrdreToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrdreRecette.StatutOrdre statut)
            {
                return statut switch
                {
                    OrdreRecette.StatutOrdre.Non_Encaissé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE")),
                    OrdreRecette.StatutOrdre.Partiel => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")),
                    OrdreRecette.StatutOrdre.Enciassé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
                };
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}