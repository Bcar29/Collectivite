using Collectivite.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Collectivite.Utils
{
    /// <summary>
    /// Retourne Visible si Non_Payé, sinon Collapsed
    /// </summary>
    public class StatutOrdreCanEditConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrdreRecette.StatutOrdre statut)
            {
                // Visible uniquement si Non_Encaissé
                return statut == OrdreRecette.StatutOrdre.Non_Encaissé ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit StatutOrdre en texte
    /// </summary>
    public class StatutOrdreRecetteToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrdreRecette.StatutOrdre statut)
            {
                return statut switch
                {
                    OrdreRecette.StatutOrdre.Non_Encaissé => "Non payé",
                    OrdreRecette.StatutOrdre.Partiel => "Partiel",
                    OrdreRecette.StatutOrdre.Enciassé => "Payé",
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
    public class StatutOrdreRecetteToColorConverter : IValueConverter
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
    public class StatutOrdreRecetteToBackgroundConverter : IValueConverter
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