using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour EngagementDetailPage.xaml
    /// </summary>
    public partial class EngagementDetailPage : Page
    {
        public EngagementDetailPage(int engagementId)
        {
            InitializeComponent();

            // Initialiser le ViewModel
            var viewModel = new EngagementDetailViewModel(engagementId);
            DataContext = viewModel;
        }
    }
}