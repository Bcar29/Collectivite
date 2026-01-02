using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class BonCommandeFormPage : Page
    {
        public BonCommandeFormPage(AuthService authService, int? bonCommandeId = null)
        {
            InitializeComponent();
            var auditService = new AuditService();
            DataContext = new BonCommandeFormViewModel(authService,  bonCommandeId);
        }
    }
}