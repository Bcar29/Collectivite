using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class DebiteurCrediteurPage : UserControl
    {
        private readonly TiersGestionViewModel _viewModel;

        public DebiteurCrediteurPage()
        {
            InitializeComponent();

            _viewModel = new TiersGestionViewModel(new TiersGestionService());
            DataContext = _viewModel;

            // S'abonner à l'événement Unloaded
            Unloaded += UserControl_Unloaded;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }

        /// <summary>
        /// Nettoyage lors du déchargement de la page
        /// </summary>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Cleanup();
        }
    }
}