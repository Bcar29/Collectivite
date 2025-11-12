using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour EngagementFormPage.xaml
    /// </summary>
    public partial class EngagementFormPage : Page
    {
        public EngagementFormPage(int? engagementId = null)
        {
            InitializeComponent();

            // Initialiser le ViewModel
            var viewModel = new EngagementFormViewModel(engagementId);
            DataContext = viewModel;
        }
    }
}