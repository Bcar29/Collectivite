using Collectivite.ViewModels;
using Collectivite.Services;
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
            if (!SessionManager.HasPermission("Engagement.Create"))
            {
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de créer des engagements.", "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Navigation vers la page de formulaire
            var formPage = new EngagementFormPage();
            NavigationService?.Navigate(formPage);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int engagementId)
            {
                if (!SessionManager.HasPermission("Engagement.Edit"))
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de modifier les engagements.", "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Navigation vers la page de formulaire en mode édition
                var formPage = new EngagementFormPage(engagementId);
                NavigationService?.Navigate(formPage);
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int engagementId)
            {
                if (!SessionManager.HasPermission("Engagement.View"))
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les engagements.", "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Navigation vers la page de détails
                var detailPage = new EngagementDetailPage(engagementId);
                NavigationService?.Navigate(detailPage);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EngagementViewModel viewModel)
            {
                viewModel.Dispose();
                //System.Diagnostics.Debug.WriteLine("BudgetLinesViewModel disposed");
            }
        }

    }
}