
using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Page d'affichage de la Balance Annuelle
    /// </summary>
    public partial class BalanceAnnuellePage : UserControl
    {
        private readonly BalanceAnnuelleViewModel _viewModel;

        /// <summary>
        /// Constructeur par défaut (pour le designer et l'utilisation simple)
        /// </summary>
        public BalanceAnnuellePage()
        {
            InitializeComponent();

            // Créer le contexte et le service
            var context = new AppDbContext();
            var service = new BalanceAnnuelleService(context);
            _viewModel = new BalanceAnnuelleViewModel(service);

            DataContext = _viewModel;
        }

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        public BalanceAnnuellePage(IBalanceAnnuelleService balanceAnnuelleService)
        {
            InitializeComponent();
            _viewModel = new BalanceAnnuelleViewModel(balanceAnnuelleService);
            DataContext = _viewModel;
        }

        /// <summary>
        /// Événement de chargement de la page
        /// </summary>
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Appeler Cleanup quand la page se ferme
            if (DataContext is BalanceAnnuelleViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }
}