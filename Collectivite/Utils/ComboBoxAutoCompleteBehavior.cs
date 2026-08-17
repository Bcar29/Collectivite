using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Collectivite.Utils
{
    /// <summary>
    /// Comportement générique qui transforme n'importe quel ComboBox en champ "autocomplete"
    /// avec filtrage live de la liste déroulante (façon moteur de recherche), sans nécessiter
    /// de propriété de filtre dédiée dans le ViewModel.
    ///
    /// Utilisation minimale (ComboBox avec DisplayMemberPath ou éléments de type string) :
    ///   local:ComboBoxAutoCompleteBehavior.IsEnabled="True"
    ///
    /// Pour un ComboBox utilisant un ItemTemplate (pas de DisplayMemberPath), préciser la
    /// propriété à utiliser pour la comparaison texte :
    ///   local:ComboBoxAutoCompleteBehavior.IsEnabled="True"
    ///   local:ComboBoxAutoCompleteBehavior.FilterMemberPath="Intitule"
    /// </summary>
    public static class ComboBoxAutoCompleteBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ComboBoxAutoCompleteBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static readonly DependencyProperty FilterMemberPathProperty =
            DependencyProperty.RegisterAttached(
                "FilterMemberPath",
                typeof(string),
                typeof(ComboBoxAutoCompleteBehavior),
                new PropertyMetadata(null));

        public static void SetFilterMemberPath(DependencyObject element, string value) => element.SetValue(FilterMemberPathProperty, value);
        public static string GetFilterMemberPath(DependencyObject element) => (string)element.GetValue(FilterMemberPathProperty);

        private static readonly DependencyPropertyDescriptor ItemsSourceDescriptor =
            DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ComboBox));

        [ThreadStatic]
        private static bool _suppressTextChanged;

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ComboBox comboBox) return;

            if ((bool)e.NewValue)
            {
                comboBox.IsEditable = true;
                comboBox.IsTextSearchEnabled = false;
                comboBox.StaysOpenOnEdit = true;
                // Empêche le filtrage (Refresh de la CollectionView) de déplacer le "current item"
                // et de forcer automatiquement une sélection pendant la saisie.
                comboBox.IsSynchronizedWithCurrentItem = false;

                comboBox.Loaded += ComboBox_Loaded;
                comboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(ComboBox_TextChanged));
                comboBox.DropDownClosed += ComboBox_DropDownClosed;
                comboBox.LostKeyboardFocus += ComboBox_LostKeyboardFocus;
                ItemsSourceDescriptor.AddValueChanged(comboBox, ComboBox_ItemsSourceChanged);
            }
            else
            {
                comboBox.Loaded -= ComboBox_Loaded;
                comboBox.RemoveHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(ComboBox_TextChanged));
                comboBox.DropDownClosed -= ComboBox_DropDownClosed;
                comboBox.LostKeyboardFocus -= ComboBox_LostKeyboardFocus;
                ItemsSourceDescriptor.RemoveValueChanged(comboBox, ComboBox_ItemsSourceChanged);
            }
        }

        private static void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                ApplyFilter(comboBox, string.Empty);
                ResyncText(comboBox);
            }
        }

        private static void ComboBox_ItemsSourceChanged(object? sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
                ApplyFilter(comboBox, string.Empty);
        }

        private static void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextChanged) return;
            if (sender is not ComboBox comboBox) return;

            ApplyFilter(comboBox, comboBox.Text);

            if (!string.IsNullOrEmpty(comboBox.Text) && !comboBox.IsDropDownOpen)
                comboBox.IsDropDownOpen = true;
        }

        private static void ComboBox_DropDownClosed(object? sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
                ApplyFilter(comboBox, string.Empty);
        }

        private static void ComboBox_LostKeyboardFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                ApplyFilter(comboBox, string.Empty);
                ResyncText(comboBox);
            }
        }

        private static void ApplyFilter(ComboBox comboBox, string filterText)
        {
            if (comboBox.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(comboBox.ItemsSource);
            if (view == null) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                if (view.Filter != null)
                    view.Filter = null;
                return;
            }

            var displayPath = !string.IsNullOrEmpty(comboBox.DisplayMemberPath)
                ? comboBox.DisplayMemberPath
                : GetFilterMemberPath(comboBox);

            view.Filter = item =>
            {
                if (item == null) return false;

                // Ne jamais masquer l'élément déjà sélectionné, pour ne pas perdre la sélection en cours
                if (Equals(item, comboBox.SelectedItem)) return true;

                var text = GetDisplayText(item, displayPath);
                return text != null && text.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
            };
        }

        private static void ResyncText(ComboBox comboBox)
        {
            if (comboBox.SelectedItem == null) return;

            var displayPath = !string.IsNullOrEmpty(comboBox.DisplayMemberPath)
                ? comboBox.DisplayMemberPath
                : GetFilterMemberPath(comboBox);

            var text = GetDisplayText(comboBox.SelectedItem, displayPath) ?? string.Empty;

            if (comboBox.Text == text) return;

            _suppressTextChanged = true;
            try
            {
                comboBox.Text = text;
            }
            finally
            {
                _suppressTextChanged = false;
            }
        }

        private static string? GetDisplayText(object item, string? path)
        {
            if (string.IsNullOrEmpty(path))
                return item.ToString();

            object? current = item;
            foreach (var segment in path.Split('.'))
            {
                if (current == null) return null;
                var prop = current.GetType().GetProperty(segment);
                if (prop == null) return current.ToString();
                current = prop.GetValue(current);
            }
            return current?.ToString();
        }
    }
}
