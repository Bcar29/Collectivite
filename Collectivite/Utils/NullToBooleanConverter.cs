using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertit null en Boolean
    /// </summary>
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit le statut de paiement en tooltip
    /// </summary>
    public class PaymentTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? "Marquer comme payé" : "Annuler le paiement";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Sélectionne la commande en fonction du statut de paiement
    /// </summary>
    public class PaymentCommandConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return null;

            var marquerPayeCommand = values[0];
            var annulerPaiementCommand = values[1];
            var datePaiement = values[2];

            return datePaiement == null ? marquerPayeCommand : annulerPaiementCommand;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}