using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;

// Ajoutez cette ligne pour inclure le namespace contenant CommunePage
using Collectivite.Views.Pages;
using Collectivite.Views;

namespace Collectivite
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Initialiser le service de navigation
            NavigationService.Instance.MainFrame = MainContentFrame;

            // Initialiser le ViewModel
            var context = new AppDbContext();
            var authService = new AuthService(context);
            _viewModel = new MainViewModel(authService);
            DataContext = _viewModel;

            // Naviguer vers le tableau de bord par défaut
            NavigateToDashboard();
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDashboard();
            _viewModel.IsMenuOpen = false;
        }

        private void CommuneButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - COMMUNE");
            // Utilisez le type CommunePage directement grâce à l'import
            NavigationService.Instance.NavigateTo(new Views.Pages.CommunePage());
            _viewModel.IsMenuOpen = false;
        }

        private void ExerciceButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - EXERCICE");
             NavigationService.Instance.NavigateTo(new Views.Pages.ExercicePage());
            _viewModel.IsMenuOpen = false;
        }

        private void NommenclatureButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - EXERCICE");
            NavigationService.Instance.NavigateTo(new Views.Pages.NommenclaturePage());
            _viewModel.IsMenuOpen = false;
        }

        private void BudgetPrimitifButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - BUDGET PRIMITIF");
             NavigationService.Instance.NavigateTo(new Views.Pages.BudgetPrimitifPage());
            _viewModel.IsMenuOpen = false;
        }

        private void BudgetLineButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - BUDGET LINE");
             NavigationService.Instance.NavigateTo(new Views.Pages.BudgetLinesPage());
            _viewModel.IsMenuOpen = false;
        }

        private void NavigateToDashboard()
        {
            _viewModel.UpdatePageTitle("TABLEAU DE BORD");
            NavigationService.Instance.NavigateTo(new DashboardPage());
        }

        // Méthode temporaire pour afficher un placeholder
        private static FrameworkElement CreatePlaceholderContent(string pageName)
        {
            var grid = new System.Windows.Controls.Grid();
            grid.Background = System.Windows.Media.Brushes.White;

            var stack = new System.Windows.Controls.StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var icon = new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircle,
                Width = 80,
                Height = 80,
                Foreground = System.Windows.Media.Brushes.Green,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var title = new System.Windows.Controls.TextBlock
            {
                Text = pageName,
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 10)
            };

            var subtitle = new System.Windows.Controls.TextBlock
            {
                Text = "La structure est prête ! Cette page sera développée dans les prochaines étapes.",
                FontSize = 16,
                Foreground = System.Windows.Media.Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500
            };

            stack.Children.Add(icon);
            stack.Children.Add(title);
            stack.Children.Add(subtitle);
            grid.Children.Add(stack);

            return grid;
        }
    }
}
