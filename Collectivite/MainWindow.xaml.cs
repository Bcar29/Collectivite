using Collectivite.Services;
using Collectivite.Tests;
using Collectivite.ViewModels;
using Collectivite.Views;
// Ajoutez cette ligne pour inclure le namespace contenant CommunePage
using Collectivite.Views.Pages;
using System.util;
using System.Windows;
using System.Windows.Controls;

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

            // 🆕 Charger l'exercice AVANT de naviguer
            Loaded += async (s, e) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // Charger les exercices d'abord
            await _viewModel.LoadExercicesAsync();

            // Ensuite naviguer vers le tableau de bord
            NavigateToDashboard();
        }
        public AuthService AuthService => _authService;

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
            if (!SessionManager.HasPermission("Commune.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("CONFIGURATION - COMMUNE");
            NavigationService.Instance.NavigateTo(new Views.Pages.CommunePage());
            _viewModel.IsMenuOpen = false;
        }

        private void ExerciceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("Exercice.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }
            _viewModel.UpdatePageTitle("CONFIGURATION - EXERCICE");
             NavigationService.Instance.NavigateTo(new Views.Pages.ExercicePage(_authService));
            _viewModel.IsMenuOpen = false;
        }

        private void NommenclatureButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("Nommenclature.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("CONFIGURATION - NOMMENCLATURE");
            NavigationService.Instance.NavigateTo(new Views.Pages.NommenclaturePage());
            _viewModel.IsMenuOpen = false;
        }
        // COMPTE COMPTABLE CLICK
        private void CompteComptable_Click(object sender, RoutedEventArgs e)
        {

            _viewModel.UpdatePageTitle("CONFIGURATION - PLAN COMPTABLES");

            if (!SessionManager.HasPermission("CompteComptable.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }


            NavigationService.Instance.NavigateTo(new Views.Pages.CompteComptablePage());
            _viewModel.IsMenuOpen = false;
        }
        // COMPTE CONTRAT CLICK
        private void Contrat_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("Contrats.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("SAISIE - DES CONTRATS");
            NavigationService.Instance.NavigateTo(new Views.Pages.ContratPage());
            _viewModel.IsMenuOpen = false;
        }

        
        private void Sythese_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGETAIRE - SYNTHESE");
             NavigationService.Instance.NavigateTo(new Views.Pages.BudgetPrimitifPage());
            _viewModel.IsMenuOpen = false;
        }

        private void BudgetLineButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - LIGNE BUDGETAIRE");

             NavigationService.Instance.NavigateTo(new Views.Pages.BudgetLinesPage(_authService));
            _viewModel.IsMenuOpen = false;
        }
        private void CompteAdministratif_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - COMPTE ADMINISTRATIF");

             NavigationService.Instance.NavigateTo(new Views.Pages.CompteAdministratifPage(_authService));
            _viewModel.IsMenuOpen = false;
        }
        private void CompteGestion_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("GestionComptable.Access"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("GESTION COMPTABLE - COMPTE DE GESTION");

            NavigationService.Instance.NavigateTo(new Views.Pages.CompteGestionPage(_authService));
            _viewModel.IsMenuOpen = false;
        }

        //SAISIES DES PIECES 

        private void BonCommande_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("SAISIE DES PIECES - BON-COMMANDE");

            if (!SessionManager.HasPermission("BonCommande.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            NavigationService.Instance.NavigateTo(new Views.Pages.BonCommandeListPage());
            _viewModel.IsMenuOpen = false;
        }
        private void ExpressionBesoin_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("SAISIE DES PIECES - EXPRESSION DE BESOIN");
            if (!SessionManager.HasPermission("ExpressionBesoin.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }
            NavigationService.Instance.NavigateTo(new Views.Pages.ExpressionBesoinListPage());
            _viewModel.IsMenuOpen = false;
        }
        

        private void OrdreRecette_Click(object sender, RoutedEventArgs e)
        {

            _viewModel.UpdatePageTitle("SAISIE DES PIECES - ORDRE RECETTE");
            if (!SessionManager.HasPermission("OrdreRecette.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            NavigationService.Instance.NavigateTo(new Views.Pages.OrdreRecettePage());
            _viewModel.IsMenuOpen = false;
        }
        private void FicheEngagement_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePageTitle("SAISIE DES PIECES - FICHE D'ENGAGEMENT");

            if (!SessionManager.HasPermission("Engagement.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }


            NavigationService.Instance.NavigateTo(new Views.Pages.EngagementPage());
            _viewModel.IsMenuOpen = false;
        }

        private void Mandat_Click(object sender, RoutedEventArgs e)
        {

            _viewModel.UpdatePageTitle("SAISIE DES PIECES - FICHE DE MANDAT");

            if (!SessionManager.HasPermission("Mandat.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            NavigationService.Instance.NavigateTo(new Views.Pages.MandatListPage());
            _viewModel.IsMenuOpen = false;
        }

        private void RemaniementButton_Click(object sender, RoutedEventArgs e)
        {

            _viewModel.UpdatePageTitle("GESTION BUDGETAIRE - BUDGET REMANIE");
            if (!SessionManager.HasPermission("Remaniement.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }


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
            if (!SessionManager.HasPermission("Tiers.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("CONFIGURATION - TIERS");
            NavigationService.Instance.NavigateTo(new Views.Pages.TiersPage());
            _viewModel.IsMenuOpen = false;
        }

        private void FactureButton_Click(object sender, RoutedEventArgs e)
        {

            _viewModel.UpdatePageTitle("SAISIE DES PIECES - FACTURES ET DETAILS");
            if (!SessionManager.HasPermission("Facture.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            NavigationService.Instance.NavigateTo(new Views.Pages.FacturePage());
            _viewModel.IsMenuOpen = false;
        }

        private void RecensementButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("Recensement.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("CONFIGURATION - RECENSEMENTS");
            NavigationService.Instance.NavigateTo(new Views.Pages.RecensementPage());
            _viewModel.IsMenuOpen = false;
        }

        private void RolesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("Administration.Access"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("ADMINISTRATION - RÔLES");
            NavigationService.Instance.NavigateTo(new RolesPage());
            _viewModel.IsMenuOpen = false;
        }

        private void PermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("Administration.Access"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("ADMINISTRATION - PERMISSIONS");
            NavigationService.Instance.NavigateTo(new PermissionsPage());
            _viewModel.IsMenuOpen = false;
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("Administration.Access"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("ADMINISTRATION - UTILISATEURS");
            NavigationService.Instance.NavigateTo(new UsersPage());
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
            _viewModel.UpdatePageTitle("GESTION BUDGÉTAIRE - SAISIE PREVISIONS");

            NavigationService.Instance.NavigateTo(new Views.Pages.BudgetLinesPage(_authService));
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
            NavigationService.Instance.NavigateTo(new Views.Pages.BudgetLinesPage(_authService));
            _viewModel.IsMenuOpen = false;
        }

        // Navigation vers la page Livre Journal
        private void LivreJournalButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("LivreJournal.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
                
            }

            _viewModel.UpdatePageTitle("COMPTABILITÉ - LIVRE JOURNAL");
            NavigationService.Instance.NavigateTo(new Views.Pages.LivreJournalPage());
            _viewModel.IsMenuOpen = false;
        }

        // Navigation vers la page Grand Livre 
        private void GrandLivreButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("GrandLivre.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }

            _viewModel.UpdatePageTitle("COMPTABILITÉ - GRAND LIVRE");
            MainContentFrame.Navigate(new Views.GrandLivrePage());
            _viewModel.IsMenuOpen = false;
        }

        // Navigation vers la page Balance Mensuelle  
        private void BalanceButton_Click(object sender, RoutedEventArgs e)
        {
            if(!SessionManager.HasPermission("BalanceMensuelle.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }
            _viewModel.UpdatePageTitle("COMPTABILITÉ - BALANCE MENSUELLE");
            MainContentFrame.Navigate(new Views.BalancePage());
            _viewModel.IsMenuOpen = false;
        }
        // Navigation vers la page Balance Annuelle  
        private void BalanceAnnuelleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("BalanceAnnuelle.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }
            _viewModel.UpdatePageTitle("COMPTABILITÉ - BALANCE ANNUELLE");
            MainContentFrame.Navigate(new Views.Pages.BalanceAnnuellePage());
            _viewModel.IsMenuOpen = false;
        }

        // Navigation vers la page Mouvement 
        private void MouvementButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("DroitConstate.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }
            _viewModel.UpdatePageTitle("COMPTABILITÉ - DROITS CONSTATES");
            MainContentFrame.Navigate(new Views.Pages.MouvementPage());
            _viewModel.IsMenuOpen = false;
        }

        // Navigation vers la page des Droits au comptant
        private void DroitsAuComptantButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.HasPermission("DroitAuComptant.View"))
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour accéder à cette section. Contactez l'administrateur",
                               "Accès refusé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                _viewModel.IsMenuOpen = false;
                return;
            }
            _viewModel.UpdatePageTitle("COMPTABILITÉ - DROITS AU COMPTANT");
            MainContentFrame.Navigate(new Views.Pages.DroitAuComptantPage());
            _viewModel.IsMenuOpen = false;
        }

        // BOUTON DIAGNOSTIC
        private async void BoutonDiagnostic_Click(object sender, RoutedEventArgs e)
        {
            await TestRapideMouvement.AfficherDiagnosticAsync();
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
