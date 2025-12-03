using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;


// Ajoutez cette ligne pour inclure le namespace contenant CommunePage
using Collectivite.Views.Pages;
using Collectivite.Views;
using System.util;

namespace Collectivite
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly AuthService _authService;

        public MainWindow() : this(null)
        {
        }

        // Dans YourWindow.xaml.cs ou YourUserControl.xaml.cs

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ActualiserEtatBoutons();
        }

        private async Task ActualiserEtatBoutons()
        {
            var service = new BudgetPrimitifService();

            // Récupérer l'exerciceId depuis votre TextBlock
            
                bool estActif = await service.EstActif(1);

                // Activer/désactiver les boutons
                BtnSaisie.IsEnabled = !estActif;
               
            
        }

        public MainWindow(AuthService? authService)
        {
            InitializeComponent();

            // Initialiser le service de navigation
            NavigationService.Instance.MainFrame = MainContentFrame;

            // Initialiser le Service d'authentification partagé si fourni
            _authService = authService ?? SessionManager.AuthService;

            // Initialiser le ViewModel
            _viewModel = new MainViewModel(_authService);
            DataContext = _viewModel;

            // Naviguer vers le tableau de bord par défaut
            NavigateToDashboard();
        }
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Dispose();
                //System.Diagnostics.Debug.WriteLine("BudgetLinesViewModel disposed");
            }
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
            _viewModel.UpdatePageTitle("CONFIGURATION - NOMMENCLATURE");
            NavigationService.Instance.NavigateTo(new Views.Pages.NommenclaturePage());
            _viewModel.IsMenuOpen = false;
        }
        // COMPTE COMPTABLE CLICK
        private void CompteComptable_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - COMPTES COMPTABLES");
            NavigationService.Instance.NavigateTo(new Views.Pages.CompteComptablePage());
            _viewModel.IsMenuOpen = false;
        }
        // COMPTE CONTRAT CLICK
        private void Contrat_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("SAISIE - DES CONTRATS");
            NavigationService.Instance.NavigateTo(new Views.Pages.ContratPage());
            _viewModel.IsMenuOpen = false;
        }

        
        private void Sythese_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - SYNTHESE");
             NavigationService.Instance.NavigateTo(new Views.Pages.BudgetPrimitifPage());
            _viewModel.IsMenuOpen = false;
        }

        private void BudgetLineButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - LIGNE BUDGETAIRE");

             NavigationService.Instance.NavigateTo(new Views.Pages.BudgetLinesPage());
            _viewModel.IsMenuOpen = false;
        }
        private void CompteAdministratif_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - COMPTE ADMINISTRATIF");

             NavigationService.Instance.NavigateTo(new Views.Pages.CompteAdministratifPage());
            _viewModel.IsMenuOpen = false;
        }
        private void CompteGestion_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION COMPTABLE - COMPTE DE GESTION");

             NavigationService.Instance.NavigateTo(new Views.Pages.CompteGestionPage());
            _viewModel.IsMenuOpen = false;
        }

        //SAISIES DES PIECES 

        private void BonCommande_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - BON-COMMANDE");
            NavigationService.Instance.NavigateTo(new Views.Pages.BonCommandeListPage());
            _viewModel.IsMenuOpen = false;
        }
        

        private void OrdreRecette_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - ORDRE RECETTE");
            NavigationService.Instance.NavigateTo(new Views.Pages.OrdreRecettePage());
            _viewModel.IsMenuOpen = false;
        }
        private void FicheEngagement_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - FICHE D'ENGAGEMENT");
            NavigationService.Instance.NavigateTo(new Views.Pages.EngagementPage());
            _viewModel.IsMenuOpen = false;
        }

        private void Mandat_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - FICHE DE MANDAT");
            NavigationService.Instance.NavigateTo(new Views.Pages.MandatListPage());
            _viewModel.IsMenuOpen = false;
        }

        private void RemaniementButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - BUDGET REMANIE");
            NavigationService.Instance.NavigateTo(new Views.Pages.RemaniementPage());
            _viewModel.IsMenuOpen = false;
        }

        //AJOUT DES COMPTES BANCAIRES ET DES TIERS

        //private void CompteBancaireButton_Click(object sender, RoutedEventArgs e)
        //{
        //    _viewModel.UpdatePageTitle("CONFIGURATION - COMPTES BANCAIRES");
        //    NavigationService.Instance.NavigateTo(new Views.Pages.CompteBancairePage());
        //    _viewModel.IsMenuOpen = false;
        //}

        private void TiersButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - TIERS");
            NavigationService.Instance.NavigateTo(new Views.Pages.TiersPage());
            _viewModel.IsMenuOpen = false;
        }

        private void FactureButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - FACTURES ET DETAILS");
            NavigationService.Instance.NavigateTo(new Views.Pages.FacturePage());
            _viewModel.IsMenuOpen = false;
        }

        private void RecensementButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("CONFIGURATION - RECENSEMENTS");
            NavigationService.Instance.NavigateTo(new Views.Pages.RecensementPage());
            _viewModel.IsMenuOpen = false;
        }

        private void NavigateToDashboard()
        {
            _viewModel.UpdatePageTitle("TABLEAU DE BORD");
            NavigationService.Instance.NavigateTo(new DashboardPage());
        }

        private void ExerciceButtonDown_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.HorizontalOffset = 0;
                button.ContextMenu.VerticalOffset = 0;
                button.ContextMenu.IsOpen = true;
            }
        }

        // Gestion d'activation/désactivation des boutons Saisie et Budget Primitif en fonction de l'état du budget primitif

        private async void BtnSaisie_Click(object sender, RoutedEventArgs e)
        {
            var service = new BudgetPrimitifService();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                throw new InvalidOperationException("Aucun exercice n'est sélectionné.");
            }

            bool estActif = await service.EstActif(exerciceService.CurrentExercice.Id);

            if (estActif)
            {
                MessageBox.Show("Impossible de saisir une ligne budgétaire!",
                               "Budget déjà validé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            // Continuer avec la page des lignes de budget
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - BUDGET LINE");

            NavigationService.Instance.NavigateTo(new Views.Pages.BudgetLinesPage());
            _viewModel.IsMenuOpen = false;

        }

        private async void BtnBudgetPrimitif_Click(object sender, RoutedEventArgs e)
        {
            var service = new BudgetPrimitifService();
            var exerciceService = ExerciceService.Instance;

            if (exerciceService.CurrentExercice == null)
            {
                throw new InvalidOperationException("Aucun exercice n'est sélectionné.");
            }

            bool estActif = await service.EstActif(exerciceService.CurrentExercice.Id);

            if (!estActif)
            {
                MessageBox.Show("Impossible de naviguer vers cette page !",
                               "Aucun budget validé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            
            // Continuer avec la page de la liste des lignes de budget
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - BUDGET PRIMITIF");

            NavigationService.Instance.NavigateTo(new Views.Pages.ListeBudgetLinePage());
            _viewModel.IsMenuOpen = false;
        }

        // Navigation vers la page Livre Journal
        private void LivreJournalButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("COMPTABILITÉ - LIVRE JOURNAL");
            NavigationService.Instance.NavigateTo(new Views.Pages.LivreJournalPage());
            _viewModel.IsMenuOpen = false;
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
