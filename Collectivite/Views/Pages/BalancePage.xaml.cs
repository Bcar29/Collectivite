using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views
{
    /// <summary>
    /// Logique d'interaction pour BalancePage.xaml
    /// </summary>
    public partial class BalancePage : UserControl
    {
        private readonly BalanceViewModel _viewModel;

        public BalancePage()
        {
            InitializeComponent();

            // Créer le service et le ViewModel
            var context = new AppDbContext();
            var service = new BalanceService(context);
            _viewModel = new BalanceViewModel(service);

            DataContext = _viewModel;
        }

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        public BalancePage(IBalanceService balanceService)
        {
            InitializeComponent();

            _viewModel = new BalanceViewModel(balanceService);
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Appeler Cleanup quand la page se ferme
            if (DataContext is BalanceViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }
}