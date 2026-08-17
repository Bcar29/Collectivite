using Collectivite.Models;
using Collectivite.Services;
using Collectivite.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour BudgetLinesPage.xaml
    /// </summary>
    public partial class BudgetLinesPage : Page
    {
        public BudgetLinesPage(AuthService authService)
        {
            InitializeComponent();

            // On attend que la Page soit entièrement chargée
            var auditService = new AuditService();
            DataContext = new BudgetLinesViewModel(new BudgetLineService(), authService, auditService);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BudgetLinesViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }

        /// <summary>
        /// Mode Tableau : seules les lignes-feuilles (sans enfant) et non-totaux sont
        /// éditables directement dans la grille - identique à la règle du mode Formulaire.
        /// </summary>
        private void BudgetGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is not BudgetLineHierarchyViewModel line)
                return;

            if (line.HasChildren || line.IsTotalRow || DataContext is not BudgetLinesViewModel vm || !vm.CanModifyBudget)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Mode Tableau : à la validation d'une cellule "Montant prévu", enregistre le
        /// nouveau montant et déclenche le recalcul automatique des ancêtres/totaux.
        /// </summary>
        private void BudgetGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not BudgetLineHierarchyViewModel line)
                return;

            // Pour une DataGridTemplateColumn, e.EditingElement est le ContentPresenter qui
            // héberge le CellEditingTemplate - jamais directement le contrôle qu'il contient -
            // il faut donc descendre dans l'arbre visuel pour retrouver le TextBox.
            var textBox = FindVisualChild<TextBox>(e.EditingElement);
            if (textBox == null)
                return;

            if (DataContext is not BudgetLinesViewModel vm)
                return;

            if (!TryParseMontant(textBox.Text, out var newMontant) || newMontant < 0)
            {
                e.Cancel = true;
                NotificationService.ShowWarning($"« {textBox.Text} » n'est pas un montant valide (nombre positif attendu).");
                return;
            }

            _ = vm.CommitLeafMontantAsync(line, newMontant);
        }

        /// <summary>
        /// Parse un montant tapé dans la grille en tenant compte du format d'affichage
        /// français (StringFormat=N2 hérité du Language="fr-FR" de la Page : virgule
        /// décimale, espace insécable pour les milliers), tout en acceptant aussi la
        /// saisie "internationale" avec un point décimal.
        /// </summary>
        private static bool TryParseMontant(string text, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var cleaned = text.Replace(" ", "").Replace(" ", "").Trim();

            if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.GetCultureInfo("fr-FR"), out value))
                return true;

            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Recherche récursive dans l'arbre visuel : nécessaire car
        /// DataGridCellEditEndingEventArgs.EditingElement expose, pour une
        /// DataGridTemplateColumn, le ContentPresenter généré par WPF, jamais directement
        /// le contrôle défini dans CellEditingTemplate.
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;
            if (parent is T typed) return typed;

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }

            return null;
        }
    }

}
