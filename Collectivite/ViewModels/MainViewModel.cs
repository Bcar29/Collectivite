using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;
        private string _currentPageTitle = "TABLEAU DE BORD";
        private string _exerciceText = "Exercice 2025";
        private string _communeName = string.Empty;
        private bool _isMenuOpen;

        public MainViewModel(AuthService authService)
        {
            _authService = authService;
            _navigationService = NavigationService.Instance;

            // Initialiser les données utilisateur
            InitializeUserData();

            // Commandes
            LogoutCommand = new RelayCommand(_ => Logout());
            OpenMenuCommand = new RelayCommand(_ => IsMenuOpen = true);
            CloseMenuCommand = new RelayCommand(_ => IsMenuOpen = false);
        }

        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set => SetProperty(ref _currentPageTitle, value);
        }

        public string ExerciceText
        {
            get => _exerciceText;
            set => SetProperty(ref _exerciceText, value);
        }

        public string CommuneName
        {
            get => _communeName;
            set => SetProperty(ref _communeName, value);
        }

        public string UserFullName => _authService.CurrentUser?.Username ?? "Utilisateur";

        public string UserInitials
        {
            get
            {
                var name = UserFullName;
                if (string.IsNullOrWhiteSpace(name))
                    return "U";

                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                
                return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name[0].ToString().ToUpper();
            }
        }

        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }

        public ICommand LogoutCommand { get; }
        public ICommand OpenMenuCommand { get; }
        public ICommand CloseMenuCommand { get; }

        private void InitializeUserData()
        {
            if (_authService.CurrentUser != null)
            {
                CommuneName = _authService.CurrentUser.Commune?.Nom ?? "Commune";
                // Vous pouvez charger l'exercice actif depuis la base de données
                ExerciceText = "Exercice 2025";
            }
        }

        private void Logout()
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir vous déconnecter ?",
                "Déconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();
                
                // Ouvrir la fenêtre de connexion
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();

                // Fermer la fenêtre principale
                Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w is MainWindow)?.Close();
            }
        }

        public void UpdatePageTitle(string title)
        {
            CurrentPageTitle = title;
        }
    }
}
