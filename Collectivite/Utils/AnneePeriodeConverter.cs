using System;
using System.Globalization;
using System.Windows.Data;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertit une date de début et un index de colonne en année de période
    /// </summary>
    public class AnneePeriodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateDebut && parameter is string indexStr && int.TryParse(indexStr, out int index))
            {
                // Calculer l'année pour cette colonne (index 0 = année 1, index 1 = année 2, etc.)
                return dateDebut.AddYears(index).Year.ToString();
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une date de début et de fin pour déterminer si une colonne doit être visible
    /// </summary>
    public class AnneePeriodeVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && 
                values[0] is DateTime dateDebut && 
                values[1] is DateTime dateFin && 
                parameter is string indexStr && 
                int.TryParse(indexStr, out int index))
            {
                // Calculer l'année pour cette colonne
                var annee = dateDebut.AddYears(index).Year;
                var anneeFin = dateFin.Year;
                
                // La colonne est visible si l'année est <= année de fin
                return annee <= anneeFin ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une date de début et un index pour déterminer si une cellule doit afficher du contenu
    /// </summary>
    public class AnneePeriodeCellConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && 
                values[0] is DateTime dateDebut && 
                values[1] is DateTime dateFin && 
                parameter is string indexStr && 
                int.TryParse(indexStr, out int index))
            {
                // Calculer l'année pour cette colonne
                var annee = dateDebut.AddYears(index).Year;
                var anneeFin = dateFin.Year;
                
                // Retourner l'année si elle est dans la plage, sinon "-"
                return annee <= anneeFin ? annee.ToString() : "-";
            }
            return "-";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

