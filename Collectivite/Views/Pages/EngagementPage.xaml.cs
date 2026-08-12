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
                NotificationService.ShowWarning("Accès refusé : vous n'avez pas la permission de créer des engagements.");
                return;
            }

            // Navigation vers la page de formulaire
            var formPage = new EngagementFormPage();
            NavigationService?.Navigate(formPage);
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