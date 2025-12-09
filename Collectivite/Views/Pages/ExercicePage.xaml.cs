using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class ExercicePage : Page
    {
        public ExercicePage(AuthService authService)
        {
            InitializeComponent();

            var exerciceService = new ExerciceService();
            var auditService = new AuditService();

            DataContext = new ExerciceViewModel(exerciceService, auditService, authService);
        }
    }
}