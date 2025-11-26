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
    /// Logique d'interaction pour MandatListPage.xaml
    /// </summary>
    public partial class MandatListPage : Page
    {
        public MandatListPage()
        {
            InitializeComponent();
            DataContext = new MandatListViewModel();
        }
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Navigation vers la page de formulaire
            var formPage = new MandatFormPage();
            NavigationService?.Navigate(formPage);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int mandatId)
            {
                // Navigation vers la page de formulaire en mode édition
                var formPage = new MandatFormPage(mandatId);
                NavigationService?.Navigate(formPage);
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int mandatId)
            {
                // Navigation vers la page de détails
                var detailPage = new MandatDetailPage(mandatId);
                NavigationService?.Navigate(detailPage);
            }
        }
    }
}
