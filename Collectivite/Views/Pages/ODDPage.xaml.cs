using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class ODDPage : UserControl
    {
        private readonly ODDViewModel _viewModel;

        public ODDPage()
        {
            InitializeComponent();
            _viewModel = new ODDViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }
}