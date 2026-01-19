using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class SecteurPDLPage : UserControl
    {
        private readonly SecteurPDLViewModel _viewModel;

        public SecteurPDLPage()
        {
            InitializeComponent();
            _viewModel = new SecteurPDLViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }
}