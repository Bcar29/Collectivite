using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace Collectivite.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;
        private readonly RelayCommand _openProfileCommand;
        private readonly RelayCommand _openSettingsCommand;
        private string _currentPageTitle = "TABLEAU DE BORD";
        private string _exerciceText = "Exercice 2025";
        private string _communeName = string.Empty;
        private bool _isMenuOpen;
        private string _userIdentifier = "Utilisateur";
        private string _userEmail = "Email non défini";
        private string _userPhone = "Téléphone non défini";

        public MainViewModel(AuthService authService)
        {
            _authService = authService;
            _navigationService = NavigationService.Instance;

            // Initialiser les commandes en premier
            LogoutCommand = new RelayCommand(_ => Logout());
            _openProfileCommand = new RelayCommand(_ => ShowProfile(), _ => _authService.CurrentUser != null);
            _openSettingsCommand = new RelayCommand(_ => ShowSettings());
            OpenProfileCommand = _openProfileCommand;
            OpenSettingsCommand = _openSettingsCommand;
            OpenMenuCommand = new RelayCommand(_ => IsMenuOpen = true);
            CloseMenuCommand = new RelayCommand(_ => IsMenuOpen = false);

            // Initialiser les données utilisateur après les commandes
            InitializeUserData();
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

        public string UserIdentifier
        {
            get => _userIdentifier;
            set => SetProperty(ref _userIdentifier, value);
        }

        public string UserEmail
        {
            get => _userEmail;
            set => SetProperty(ref _userEmail, value);
        }

        public string UserPhone
        {
            get => _userPhone;
            set => SetProperty(ref _userPhone, value);
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
        public ICommand OpenProfileCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenMenuCommand { get; }
        public ICommand CloseMenuCommand { get; }

        private void InitializeUserData()
        {
            if (_authService.CurrentUser != null)
            {
                CommuneName = _authService.CurrentUser.Commune?.Nom ?? "Commune";
                UserIdentifier = _authService.CurrentUser.Username;
                UserEmail = _authService.CurrentUser.Email;
                UserPhone = string.IsNullOrWhiteSpace(_authService.CurrentUser.Tel)
                    ? "Téléphone non renseigné"
                    : _authService.CurrentUser.Tel;
                ExerciceText = "Exercice 2025";

                OnPropertyChanged(nameof(UserFullName));
                OnPropertyChanged(nameof(UserInitials));
                _openProfileCommand.RaiseCanExecuteChanged();
            }
            else
            {
                CommuneName = "Commune";
                UserIdentifier = "Non connecté";
                UserEmail = "Email non disponible";
                UserPhone = "Téléphone non disponible";
                _openProfileCommand.RaiseCanExecuteChanged();
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
                _openProfileCommand.RaiseCanExecuteChanged();

                // Ouvrir la fenêtre de connexion
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();

                // Fermer la fenêtre principale
                Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w is MainWindow)?.Close();
            }
        }

        private void ShowProfile()
        {
            if (_authService.CurrentUser == null)
                return;

            var profileWindow = new Views.ProfileWindow(_authService.CurrentUser);
            profileWindow.Owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w is MainWindow);

            var result = profileWindow.ShowDialog();

            // Rafraîchir les données si sauvegardées
            if (result == true)
            {
                OnPropertyChanged(nameof(UserFullName));
                OnPropertyChanged(nameof(UserInitials));
                OnPropertyChanged(nameof(UserEmail));
            }
        }
        private void ShowSettings()
        {
            var settingsWindow = new Views.SettingsWindow();
            settingsWindow.Owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w is MainWindow);
            settingsWindow.ShowDialog();
        }

        public void UpdatePageTitle(string title)
        {
            CurrentPageTitle = title;
        }
    }
}
