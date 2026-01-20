using Collectivite.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Collectivite.Views.Pages
{
    public partial class ActivitePDLPage : UserControl
    {
        private readonly ActivitePDLViewModel _viewModel;
        private bool _scrollViewerAttached = false;

        public ActivitePDLPage()
        {
            InitializeComponent();
            _viewModel = new ActivitePDLViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.InitialiserAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors du chargement de la page : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ActivitiesDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sender is DataGrid dataGrid && e.Row.Item != null)
            {
                var index = dataGrid.Items.IndexOf(e.Row.Item);
                e.Row.Header = (index + 1).ToString();
                
                // Permettre l'ajustement automatique de la hauteur de la ligne
                e.Row.Height = double.NaN; // NaN permet l'ajustement automatique
            }
        }

        private void ActivitiesDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // Synchroniser la largeur du Grid d'en-têtes avec le DataGrid
                SynchronizeHeadersWidth();
            }
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Synchroniser le scroll horizontal du Grid d'en-têtes avec le ScrollViewer principal
            if (HeaderScrollViewer != null && e.HorizontalChange != 0)
            {
                HeaderScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void HeaderScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Synchroniser le scroll horizontal du ScrollViewer principal avec le Grid d'en-têtes
            if (MainScrollViewer != null && e.HorizontalChange != 0)
            {
                MainScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void SynchronizeHeadersWidth()
        {
            if (CustomHeadersGrid != null && ActivitiesDataGrid != null)
            {
                // Calculer la largeur totale du DataGrid
                double totalWidth = 0;
                foreach (var column in ActivitiesDataGrid.Columns)
                {
                    totalWidth += column.Width.IsAbsolute ? column.Width.Value : 
                                 (column.Width.IsStar ? 100 : column.Width.DisplayValue);
                }
                
                // Ajuster la largeur minimale du Grid d'en-têtes
                CustomHeadersGrid.MinWidth = totalWidth;
            }
        }

    }
}