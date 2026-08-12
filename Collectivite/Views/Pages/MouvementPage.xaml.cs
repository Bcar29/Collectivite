
using Collectivite.Services;
using Collectivite.ViewModels;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        private readonly string _logPath = Path.Combine(Path.GetTempPath(), "collectivite_mouvement.log");

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public MouvementPage()
        {
            try
            {
                Log("MouvementPage ctor - début");
                //MessageBox.Show("ETAPE 1: Début constructeur MouvementPage", "Debug");

                InitializeComponent();
                Log("MouvementPage ctor - InitializeComponent OK");

                //MessageBox.Show("ETAPE 2: InitializeComponent OK", "Debug");

                // Créer le contexte
                var context = new AppDbContext();
                Log("MouvementPage ctor - AppDbContext créé");
                //MessageBox.Show("ETAPE 3: AppDbContext créé", "Debug");

                // Créer le service
                var service = new MouvementService(context);
                Log("MouvementPage ctor - MouvementService créé");
                //MessageBox.Show("ETAPE 4: MouvementService créé", "Debug");

                // Créer le ViewModel
                _viewModel = new MouvementViewModel(service);
                Log("MouvementPage ctor - ViewModel créé");
                //MessageBox.Show("ETAPE 5: ViewModel créé", "Debug");

                DataContext = _viewModel;
                Log("MouvementPage ctor - DataContext assigné");
                //MessageBox.Show("ETAPE 6: DataContext assigné", "Debug");
            }
            catch (Exception ex)
            {
                Log($"ERREUR ctor: {ex.Message}\n{ex.StackTrace}");
                NotificationService.ShowError($"ERREUR dans constructeur:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}");
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
            Log("MouvementPage ctor DI - DataContext assigné");
        }

        /// <summary>
        /// Événement de chargement de la page
        /// </summary>
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("MouvementPage Loaded");

                if (_viewModel != null)
                {
                    Log("MouvementPage Loaded - InitialiserAsync start");
                    await _viewModel.InitialiserAsync();
                    Log("MouvementPage Loaded - InitialiserAsync done");

                }
                else
                {
                    Log("MouvementPage Loaded - _viewModel est null");
                    NotificationService.ShowError("ERREUR: _viewModel est null!");
                }
            }
            catch (Exception ex)
            {
                Log($"ERREUR Loaded: {ex.Message}\nInner: {ex.InnerException?.Message}\n{ex.StackTrace}");
                NotificationService.ShowError($"ERREUR dans UserControl_Loaded:\n{ex.Message}\n\nInner:\n{ex.InnerException?.Message}");
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Log("MouvementPage Unloaded");
            // Appeler Cleanup quand la page se ferme
            if (DataContext is MouvementViewModel vm)
            {
                vm.Cleanup();
            }
        }

        private void Log(string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}";
            Debug.WriteLine(line);
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch
            {
                // ignorer les erreurs d'écriture de log
            }
        }
    }

    

   
}