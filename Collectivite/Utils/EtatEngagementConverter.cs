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
    public class EtatEngagementToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Engagement.EtatEngagement etat)
            {
                return etat switch
                {
                    Engagement.EtatEngagement.Non_Validé => "Non Validé",
                    Engagement.EtatEngagement.Validé => "Validé",
                    
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
    public class EtatEngagementToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Engagement.EtatEngagement etat)
            {
                return etat switch
                {
                    Engagement.EtatEngagement.Non_Validé  => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    Engagement.EtatEngagement.Validé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    
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
    public class EtatEngagmentToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Engagement.EtatEngagement etat)
            {
                return etat switch
                {
                    Engagement.EtatEngagement.Non_Validé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE")),
                    Engagement.EtatEngagement.Validé => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")),
                    
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
                };
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}