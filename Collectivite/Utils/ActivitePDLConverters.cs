using Collectivite.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Collectivite.Utils
{
    /// <summary>
    /// Convertit une collection de Bénéficiaires en chaîne de caractères
    /// </summary>
    public class BeneficiairesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<BeneficiairePDL> beneficiaires && beneficiaires.Any())
            {
                return string.Join(", ", beneficiaires.Select(b => b.Nom));
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une collection de Bénéficiaires en tooltip
    /// </summary>
    public class BeneficiairesToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<BeneficiairePDL> beneficiaires && beneficiaires.Any())
            {
                return string.Join("\n", beneficiaires.Select(b => b.Nom));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une collection d'Acteurs en chaîne de caractères
    /// </summary>
    public class ActeursToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<ActeurPDL> acteurs && acteurs.Any())
            {
                return string.Join(", ", acteurs.Select(a => a.Nom));
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une collection d'Acteurs en tooltip
    /// </summary>
    public class ActeursToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<ActeurPDL> acteurs && acteurs.Any())
            {
                return string.Join("\n", acteurs.Select(a => a.Nom));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une collection de Structures en chaîne de caractères
    /// </summary>
    public class StructuresToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<StructureExecutionPDL> structures && structures.Any())
            {
                return string.Join(", ", structures.Select(s => s.Nom));
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une collection de Structures en tooltip
    /// </summary>
    public class StructuresToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<StructureExecutionPDL> structures && structures.Any())
            {
                return string.Join("\n", structures.Select(s => s.Nom));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit les dates de début et fin en une chaîne contenant toutes les années de période
    /// </summary>
    public class PeriodeCompleteConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && 
                values[0] is DateTime dateDebut && 
                values[1] is DateTime dateFin)
            {
                var annees = new List<string>();
                var anneeDebut = dateDebut.Year;
                var anneeFin = dateFin.Year;
                
                // Générer toutes les années de la période (maximum 5 ans)
                for (int i = 0; i < 5; i++)
                {
                    var annee = anneeDebut + i;
                    if (annee <= anneeFin)
                    {
                        annees.Add(annee.ToString());
                    }
                    else
                    {
                        annees.Add("-");
                    }
                }
                
                // Retourner les années séparées par des espaces
                return string.Join("  ", annees);
            }
            return "-  -  -  -  -";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

