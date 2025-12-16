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
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is FactureViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}