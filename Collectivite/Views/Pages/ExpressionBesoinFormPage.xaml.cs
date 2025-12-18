using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows.Controls;


namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour ExpressionBesoinFormPage.xaml
    /// </summary>
    public partial class ExpressionBesoinFormPage : Page
    {
        public ExpressionBesoinFormPage(AuthService authService,int? expressionBesoinId = null)
        {
            InitializeComponent();
            var auditService = new AuditService();
            DataContext = new ExpressionBesoinFormViewModel( authService,auditService,expressionBesoinId);
        }
    }
}
