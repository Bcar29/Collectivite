using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class FacturePage : Page
    {
        public FacturePage(AuthService authService)
        {
            InitializeComponent();
            var auditService = new AuditService();
            DataContext = new FactureViewModel(authService, auditService);
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int factureId)
            {
                if (!SessionManager.HasPermission("Facture.View"))
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les factures.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var detailPage = new FactureDetailsPage(factureId);
                NavigationService?.Navigate(detailPage);
            }
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is FactureViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}