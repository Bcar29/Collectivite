using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class CompetenceCollectivitePage : UserControl
    {
        private readonly CompetenceCollectiviteViewModel _viewModel;

        public CompetenceCollectivitePage()
        {
            InitializeComponent();
            _viewModel = new CompetenceCollectiviteViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }
}