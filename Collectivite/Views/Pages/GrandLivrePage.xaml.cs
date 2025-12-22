using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views
{
    /// <summary>
    /// Logique d'interaction pour GrandLivrePage.xaml
    /// </summary>
    public partial class GrandLivrePage : UserControl
    {
        private readonly GrandLivreViewModel _viewModel;

        public GrandLivrePage()
        {
            InitializeComponent();

            // Créer le service et le ViewModel
            // En production, utiliser l'injection de dépendances
            var context = new AppDbContext();
            var service = new GrandLivreService(context);
            _viewModel = new GrandLivreViewModel(service);

            DataContext = _viewModel;
        }

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        public GrandLivrePage(IGrandLivreService grandLivreService)
        {
            InitializeComponent();

            _viewModel = new GrandLivreViewModel(grandLivreService);
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialiser le ViewModel au chargement de la page
            await _viewModel.InitialiserAsync();
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Appeler Cleanup quand la page se ferme
            if (DataContext is GrandLivreViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }
}