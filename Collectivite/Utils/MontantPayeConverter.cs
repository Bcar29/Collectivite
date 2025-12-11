using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Collectivite.Utils
{
    /// <summary>
    /// Converter pour afficher le montant payé d'un mandat à partir du dictionnaire MontantsPayes
    /// </summary>
    public class MontantPayeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return "0 GNF";

            // values[0] = Id du mandat
            // values[1] = Dictionnaire MontantsPayes
            if (values[0] is int mandatId && values[1] is Dictionary<int, decimal> montantsPayes)
            {
                if (montantsPayes.TryGetValue(mandatId, out var montant))
                {
                    return $"{montant:N0} GNF";
                }
            }

            return "0 GNF";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter pour déterminer si le bouton de modification doit être visible
    /// Visible uniquement si le mandat n'est pas entièrement payé (status != Payé)
    /// </summary>
    public class StatutMandatToEditVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Collectivite.Models.Mandat.StatutMandat statut)
            {
                // Le bouton est visible si le mandat n'est pas entièrement payé
                return statut != Collectivite.Models.Mandat.StatutMandat.Payé
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    
}