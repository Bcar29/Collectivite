using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour TiersPage.xaml
    /// </summary>
    public partial class TiersPage : Page
    {
        public TiersPage()
        {
            InitializeComponent();

            // ✅ CORRECTION : Créer simplement le ViewModel
            // Plus besoin de passer les services
            var viewModel = new TiersViewModel();
            DataContext = viewModel;
        }

        private void ApplyFilter()
        {
            // Le filtre est géré par les onglets dans le XAML
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Appeler Cleanup quand la page se ferme
            if (DataContext is TiersViewModel vm)
            {
                vm.Cleanup();
            }
        }

    }

}