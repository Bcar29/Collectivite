using Collectivite.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertit EtatMandat ou EtatOdre en texte lisible
    /// </summary>
    public class EtatOrdreToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Gestion de EtatMandat
            if (value is Mandat.EtatMandat etatMandat)
            {
                return etatMandat switch
                {
                    Mandat.EtatMandat.Non_Validé => "Non Validé",
                    Mandat.EtatMandat.Validé => "Validé",
                    _ => ""
                };
            }

            // Gestion de EtatOdre (OrdreRecette)
            if (value is OrdreRecette.EtatOdre etatOrdre)
            {
                return etatOrdre switch
                {
                    OrdreRecette.EtatOdre.Non_Validé => "Non Validé",
                    OrdreRecette.EtatOdre.Validé => "Validé",
                    _ => ""
                };
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit EtatMandat ou EtatOdre en couleur de texte
    /// </summary>
    public class EtatOrdreToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool estValide = false;

            // Gestion de EtatMandat
            if (value is Mandat.EtatMandat etatMandat)
            {
                estValide = etatMandat == Mandat.EtatMandat.Validé;
            }
            // Gestion de EtatOdre (OrdreRecette)
            else if (value is OrdreRecette.EtatOdre etatOrdre)
            {
                estValide = etatOrdre == OrdreRecette.EtatOdre.Validé;
            }

            // Validé = Vert, Non Validé = Orange
            return new SolidColorBrush(estValide
                ? (Color)ColorConverter.ConvertFromString("#059669")  // Vert
                : (Color)ColorConverter.ConvertFromString("#D97706")); // Orange
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit EtatMandat ou EtatOdre en couleur de fond
    /// </summary>
    public class EtatOrdreToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool estValide = false;

            // Gestion de EtatMandat
            if (value is Mandat.EtatMandat etatMandat)
            {
                estValide = etatMandat == Mandat.EtatMandat.Validé;
            }
            // Gestion de EtatOdre (OrdreRecette)
            else if (value is OrdreRecette.EtatOdre etatOrdre)
            {
                estValide = etatOrdre == OrdreRecette.EtatOdre.Validé;
            }

            // Validé = Fond vert clair, Non Validé = Fond orange clair
            return new SolidColorBrush(estValide
                ? (Color)ColorConverter.ConvertFromString("#D1FAE5")  // Vert clair
                : (Color)ColorConverter.ConvertFromString("#FEF3C7")); // Orange clair
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}