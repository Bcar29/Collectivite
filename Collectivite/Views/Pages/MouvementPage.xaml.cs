
using Collectivite.Services;
using Collectivite.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Page de gestion des mouvements (paiements et encaissements)
    /// </summary>
    public partial class MouvementPage : UserControl
    {
        private MouvementViewModel? _viewModel;

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public MouvementPage()
        {
            try
            {
                //MessageBox.Show("ETAPE 1: Début constructeur MouvementPage", "Debug");

                InitializeComponent();

                //MessageBox.Show("ETAPE 2: InitializeComponent OK", "Debug");

                // Créer le contexte
                var context = new AppDbContext();
                //MessageBox.Show("ETAPE 3: AppDbContext créé", "Debug");

                // Créer le service
                var service = new MouvementService(context);
                //MessageBox.Show("ETAPE 4: MouvementService créé", "Debug");

                // Créer le ViewModel
                _viewModel = new MouvementViewModel(service);
                //MessageBox.Show("ETAPE 5: ViewModel créé", "Debug");

                DataContext = _viewModel;
                //MessageBox.Show("ETAPE 6: DataContext assigné", "Debug");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERREUR dans constructeur:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur Constructeur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Constructeur avec injection de dépendances
        /// </summary>
        public MouvementPage(IMouvementService mouvementService)
        {
            InitializeComponent();
            _viewModel = new MouvementViewModel(mouvementService);
            DataContext = _viewModel;
        }

        /// <summary>
        /// Événement de chargement de la page
        /// </summary>
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
               

                if (_viewModel != null)
                {
                    

                    await _viewModel.InitialiserAsync();

                    
                }
                else
                {
                    MessageBox.Show("ERREUR: _viewModel est null!", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERREUR dans UserControl_Loaded:\n{ex.Message}\n\nInner:\n{ex.InnerException?.Message}",
                    "Erreur Loaded", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Appeler Cleanup quand la page se ferme
            if (DataContext is MouvementViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }

    

   
}