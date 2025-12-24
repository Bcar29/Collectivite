using Collectivite.Services;
using Collectivite.ViewModels;
using System;
using System.Collections.Generic;
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
    /// Logique d'interaction pour ContratPage.xaml
    /// </summary>
    public partial class ContratPage : Page
    {
        public ContratPage()
        {
            InitializeComponent();
            var context = new AppDbContext();
            var contratService = new ContratService(context);
            var viewModel = new ContratViewModel(contratService);

            // ⚠️ IMPORTANT : Définir le DataContext pour le binding
            DataContext = viewModel;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Appeler Cleanup quand la page se ferme
            if (DataContext is ContratViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }
}
