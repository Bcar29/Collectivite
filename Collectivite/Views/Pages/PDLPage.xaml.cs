using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class PDLPage : UserControl
    {
        private readonly PDLViewModel _viewModel;

        public PDLPage()
        {
            InitializeComponent();
            _viewModel = new PDLViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }
}