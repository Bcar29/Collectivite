using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class ProgrammePDLPage : UserControl
    {
        private readonly ProgrammePDLViewModel _viewModel;

        public ProgrammePDLPage()
        {
            InitializeComponent();
            _viewModel = new ProgrammePDLViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }
}