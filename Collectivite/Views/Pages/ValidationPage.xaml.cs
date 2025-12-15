using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Page de validation des engagements, mandats et ordres de recette
    /// </summary>
    public partial class ValidationPage : UserControl
    {
        private readonly ValidationViewModel _viewModel;

        public ValidationPage()
        {
            InitializeComponent();
            _viewModel = new ValidationViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }
}