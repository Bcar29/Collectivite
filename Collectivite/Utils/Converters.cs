using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Views.Pages ;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections;
using System.Linq;

namespace Collectivite.Utils
{
    public class StringToVisibilityConverters : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Converter pour le badge de couleur selon le mode de règlement
    /// </summary>
    public class ModeReglementToBadgeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string mode = value?.ToString() ?? "";
            
            return mode.ToLower() switch
            {
                "espèces" or "especes" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Vert #4CAF50
                "virement" => new SolidColorBrush(Color.FromRgb(25, 118, 210)),              // Bleu #1976D2
                "chèque" or "cheque" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),     // Orange #FF9800
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))                        // Gris #9E9E9E
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Converter pour afficher quand la liste est vide (Count = 0)
    /// </summary>
    public class ZeroToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Converter de base pour ModeReglement vers bool
    /// </summary>
    public abstract class ModeReglementToBoolConverterBase : IValueConverter
    {
        protected abstract ModeReglement TargetMode { get; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ModeReglement mode)
            {
                return mode == TargetMode;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return TargetMode;
            }
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converter qui retourne Visible si count = 0, sinon Collapsed
    /// </summary>
    public class CountZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Visible SEULEMENT si count = 0
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Affiche l'élément UNIQUEMENT si la collection est vide (Count = 0)
    /// Utiliser pour l'info box "Aucun élément"
    /// </summary>
    public class EmptyCollectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Visible si count = 0 (liste vide)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Affiche l'élément UNIQUEMENT si la collection contient des éléments (Count > 0)
    /// Utiliser pour le DataGrid
    /// </summary>
    public class NonEmptyCollectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Visible si count > 0 (liste non vide)
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter générique pour ModeReglement avec propriété TargetMode configurable
    /// </summary>
    public class ModeReglementToBoolConverter : IValueConverter
    {
        public ModeReglement TargetMode { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ModeReglement mode)
            {
                return mode == TargetMode;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return TargetMode;
            }
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converter pour le mode Espèces
    /// </summary>
    public class EspeceToBoolConverter : ModeReglementToBoolConverterBase
    {
        protected override ModeReglement TargetMode => ModeReglement.Espece;
    }

    /// <summary>
    /// Converter pour le mode Virement
    /// </summary>
    public class VirementToBoolConverter : ModeReglementToBoolConverterBase
    {
        protected override ModeReglement TargetMode => ModeReglement.Virement;
    }

    /// <summary>
    /// Converter pour le mode Chèque
    /// </summary>
    public class ChequeToBoolConverter : ModeReglementToBoolConverterBase
    {
        protected override ModeReglement TargetMode => ModeReglement.Cheque;
    }

    /// <summary>
    /// Convertit une chaîne de couleur hexadécimale en SolidColorBrush
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
                }
                catch
                {
                    return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une valeur null en Visibility.Collapsed, sinon Visibility.Visible
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse d'un BooleanToVisibilityConverter
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Formate un montant en GNF
    /// </summary>
    public class CurrencyFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return $"{decimalValue:N0} GNF";
            }
            if (value is double doubleValue)
            {
                return $"{doubleValue:N0} GNF";
            }
            if (value is int intValue)
            {
                return $"{intValue:N0} GNF";
            }
            return "0 GNF";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Affiche si la chaîne n'est pas null/empty, sinon Collapsed
    /// </summary>
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit une variation (decimal/double/int) en couleur (vert, rouge, gris)
    /// </summary>
    public class VariationToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var variation = ToDecimal(value);

            if (variation > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#388E3C"));
            if (variation < 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#546E7A"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static decimal ToDecimal(object value)
        {
            return value switch
            {
                decimal d => d,
                double dbl => (decimal)dbl,
                int i => i,
                _ => 0m
            };
        }
    }

    /// <summary>
    /// Convertit une variation en libellé (Augmentation / Diminution / Équilibré)
    /// </summary>
    public class VariationToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var variation = value switch
            {
                decimal d => d,
                double dbl => (decimal)dbl,
                int i => i,
                _ => 0m
            };

            if (variation > 0) return "Augmentation";
            if (variation < 0) return "Diminution";
            return "Équilibré";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    /// <summary>
    /// Convertit un bool en texte configurable ("Oui;Non" par défaut).
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var format = parameter as string ?? "Oui;Non";
            var parts = format.Split(';');
            var trueText = parts.Length > 0 ? parts[0] : "Oui";
            var falseText = parts.Length > 1 ? parts[1] : "Non";

            if (value is bool boolValue && boolValue)
                return trueText;

            return falseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NOUVEAUX CONVERTERS (ajoutés pour la gestion des nomenclatures)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Convertit un enum en boolean pour les RadioButtons
    /// Utilisé pour lier des RadioButtons à des propriétés enum (Nature, Section)
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            if ((bool)value)
                return Enum.Parse(targetType, parameter.ToString());

            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Inverse un boolean (pour les bindings inverses)
    /// Utilisé pour IsSaisieLibreMode = !IsNommenclatureMode
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }
    }

    /// <summary>
    /// Convertit un boolean en couleur (pour le background conditionnel)
    /// Format du paramètre: "ColorIfTrue|ColorIfFalse"
    /// Exemple: "#F5F5F5|White"
    /// Utilisé pour griser le champ intitulé en mode lecture seule
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorArray = colors.Split('|');
                if (colorArray.Length == 2)
                {
                    var colorString = boolValue ? colorArray[0] : colorArray[1];
                    return System.Windows.Media.ColorConverter.ConvertFromString(colorString);
                }
            }

            return System.Windows.Media.Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Convertit un montant décimal en chaîne formatée avec séparateurs de milliers et 2 décimales
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "0,00";

            if (value is decimal decimalValue)
            {
                // Format avec séparateurs d'espaces pour les milliers et 2 décimales
                return decimalValue.ToString("N2", new CultureInfo("fr-FR")).Replace(" ", " ");
            }

            if (value is double doubleValue)
            {
                return doubleValue.ToString("N2", new CultureInfo("fr-FR")).Replace(" ", " ");
            }

            if (value is int intValue)
            {
                return intValue.ToString("N2", new CultureInfo("fr-FR")).Replace(" ", " ");
            }

            if (value is long longValue)
            {
                return longValue.ToString("N2", new CultureInfo("fr-FR")).Replace(" ", " ");
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un pourcentage (double) en chaîne formatée avec signe + ou - et 2 décimales
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "0,00%";

            if (value is double doubleValue)
            {
                // Arrondir à 2 décimales
                var rounded = Math.Round(doubleValue, 2);

                // Ajouter le signe + pour les valeurs positives
                if (rounded > 0)
                    return $"+{rounded:0.00}%";
                else if (rounded < 0)
                    return $"{rounded:0.00}%";
                else
                    return "0,00%";
            }

            if (value is decimal decimalValue)
            {
                var rounded = Math.Round((double)decimalValue, 2);

                if (rounded > 0)
                    return $"+{rounded:0.00}%";
                else if (rounded < 0)
                    return $"{rounded:0.00}%";
                else
                    return "0,00%";
            }

            return value.ToString();
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }

    public class BooleanAndToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return Visibility.Collapsed;

            bool hasChildren = values[0] is bool b1 && b1;
            bool isNotTotalRow = values[1] is bool b2 && b2;

            return hasChildren && isNotTotalRow ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Vrai si le statut du budget primitif a atteint ou dépassé le statut passé en paramètre
    /// (DRAFT &lt; APPROVED &lt; VALIDATED). Utilisé par le stepper de progression
    /// Brouillon → Approuvé → Validé de la page Synthèse.
    /// </summary>
    public class StatusAtLeastConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not BudgetPrimitif.Statusbudget status || parameter == null)
                return false;

            if (!Enum.TryParse<BudgetPrimitif.Statusbudget>(parameter.ToString(), out var target))
                return false;

            return status >= target;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Calcule le solde budgétaire (Recettes - Dépenses) d'un budget primitif et le formate en GNF.
    /// </summary>
    public class RecetteDepenseToSoldeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return "0 GNF";
            var recette = values[0] is decimal r ? r : 0m;
            var depense = values[1] is decimal d ? d : 0m;
            return $"{recette - depense:N0} GNF";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Couleur du solde budgétaire (Recettes - Dépenses) : vert si excédentaire, rouge si déficitaire, gris si équilibré.
    /// </summary>
    public class RecetteDepenseToSoldeBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return new SolidColorBrush(Color.FromRgb(84, 110, 122));
            var recette = values[0] is decimal r ? r : 0m;
            var depense = values[1] is decimal d ? d : 0m;
            var solde = recette - depense;

            if (solde > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#388E3C"));
            if (solde < 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#546E7A"));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertisseur pour afficher les boutons d'action uniquement sur les lignes sans enfants et qui ne sont pas des totaux
    /// </summary>
    public class ShowActionsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return Visibility.Collapsed;

            // Premier binding : la collection Enfants de la nomenclature
            var enfants = values[0] as IEnumerable;
            bool hasChildren = enfants != null && enfants.Cast<object>().Any();

            // Deuxième binding : IsTotalRow
            bool isTotalRow = values[1] is bool b && b;

            // Afficher les boutons SI :
            // - N'a PAS d'enfants (c'est une feuille)
            // - ET ce n'est PAS une ligne de totaux
            return !hasChildren && !isTotalRow ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
