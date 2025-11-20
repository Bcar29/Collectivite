using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour EngagementPage.xaml
    /// </summary>
    public partial class EngagementPage : Page
    {
        public EngagementPage()
        {
            InitializeComponent();

            // Initialiser le ViewModel
            var viewModel = new EngagementViewModel();
            DataContext = viewModel;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Navigation vers la page de formulaire
            var formPage = new EngagementFormPage();
            NavigationService?.Navigate(formPage);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int engagementId)
            {
                // Navigation vers la page de formulaire en mode édition
                var formPage = new EngagementFormPage(engagementId);
                NavigationService?.Navigate(formPage);
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int engagementId)
            {
                // Navigation vers la page de détails
                var detailPage = new EngagementDetailPage(engagementId);
                NavigationService?.Navigate(detailPage);
            }
        }
    }
}