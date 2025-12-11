using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Collectivite.Models;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertisseur pour déterminer la visibilité du bouton de modification
    /// Le bouton est visible uniquement si le mandat n'est pas complètement payé
    /// </summary>
    public class CanEditMandatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Mandat.StatutMandat statut)
            {
                // Le bouton de modification est visible si le mandat n'est pas complètement payé
                return statut != Mandat.StatutMandat.Payé ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter combiné pour le bouton de modification
    /// Prend en compte la permission ET le statut du mandat
    /// Le bouton est visible si : CanEditMandat == true ET status == Non_Payé
    /// Le bouton est invisible si le statut est Payé ou Partiel
    /// </summary>
    public class EditButtonVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Visibility.Collapsed;

            // values[0] = CanEditMandat (bool)
            // values[1] = status (StatutMandat)
            bool canEdit = values[0] is bool b && b;

            if (!canEdit)
                return Visibility.Collapsed;

            if (values[1] is Collectivite.Models.Mandat.StatutMandat statut)
            {
                // Le bouton est visible uniquement si le mandat n'est pas payé (ni totalement, ni partiellement)
                return statut == Collectivite.Models.Mandat.StatutMandat.Non_Payé
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}