using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace Collectivite.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly ExerciceService _exerciceService;
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
        private string _userRole = "Rôle non défini";

        public MainViewModel(AuthService authService)
        {
            _authService = authService;
            _exerciceService = ExerciceService.Instance;
            _navigationService = NavigationService.Instance;

            // Initialiser les commandes
            LogoutCommand = new RelayCommand(_ => Logout());
            _openProfileCommand = new RelayCommand(_ => ShowProfile(), _ => _authService.CurrentUser != null);
            _openSettingsCommand = new RelayCommand(_ => ShowSettings());
            SelectExerciceCommand = new RelayCommand(param => SelectExercice(param));
            OpenProfileCommand = _openProfileCommand;
            OpenSettingsCommand = _openSettingsCommand;
            OpenMenuCommand = new RelayCommand(_ => IsMenuOpen = true);
            CloseMenuCommand = new RelayCommand(_ => IsMenuOpen = false);

            // S'abonner aux changements d'exercice
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            // Charger les exercices de manière asynchrone
            _ = LoadExercicesAsync();

            // Initialiser les données utilisateur
            InitializeUserData();
        }

        // Collection des exercices
        public ObservableCollection<Exercice> Exercices { get; } = new();

        public ICommand SelectExerciceCommand { get; }

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

        public string UserRole
        {
            get => _userRole;
            set => SetProperty(ref _userRole, value);
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

        /// <summary>
        /// Charge tous les exercices depuis la base de données
        /// </summary>
        private async Task LoadExercicesAsync()
        {
            try
            {
                var exercices = await _exerciceService.GetAllExerciceAsync();

                // Effacer et recharger la collection
                Exercices.Clear();
                foreach (var exercice in exercices)
                {
                    Exercices.Add(exercice);
                }

                // Initialiser l'exercice courant si pas déjà défini
                if (_exerciceService.CurrentExercice == null && exercices.Any())
                {
                    // Prendre l'exercice non clôturé ou le plus récent
                    var activeExercice = exercices.FirstOrDefault(e => !e.EstCloture)
                                        ?? exercices.First();
                    _exerciceService.CurrentExercice = activeExercice;
                }

                // Mettre à jour le texte avec l'exercice actuel
                if (_exerciceService.CurrentExercice != null)
                {
                    ExerciceText = _exerciceService.CurrentExercice.Libelle;
                }

                // Notifier le changement
                OnPropertyChanged(nameof(Exercices));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du chargement des exercices : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Sélectionne un exercice spécifique
        /// </summary>
        private void SelectExercice(object? parameter)
        {
            if (parameter is int exerciceId)
            {
                var selectedExercice = Exercices.FirstOrDefault(e => e.Id == exerciceId);
                if (selectedExercice != null)
                {
                    // Utiliser la méthode du service pour notifier tous les abonnés
                    _exerciceService.SetCurrentExercice(selectedExercice);

                    // Mettre à jour le texte affiché
                    ExerciceText = selectedExercice.Libelle!;

                    // Notifier le changement
                    OnPropertyChanged(nameof(ExerciceText));
                }
            }
        }

        /// <summary>
        /// Gestionnaire d'événement pour les changements d'exercice
        /// </summary>
        private void OnExerciceChanged(object? sender, Exercice exercice)
        {
            // Mettre à jour l'interface utilisateur sur le thread UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                ExerciceText = exercice.Libelle!;
                //Properties.Settings.Default.ExerciceId = exercice.Id;
                //Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(ExerciceText));

                // Vous pouvez ajouter d'autres notifications ici
                // Par exemple, recharger des données dépendantes de l'exercice
            });
        }

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

                    // Rôle de l'utilisateur
                    UserRole = _authService.CurrentRoleName ?? "Rôle non défini";

                // Notifier les changements
                OnPropertyChanged(nameof(CommuneName));
                OnPropertyChanged(nameof(UserIdentifier));
                OnPropertyChanged(nameof(UserEmail));
                OnPropertyChanged(nameof(UserPhone));
                OnPropertyChanged(nameof(UserFullName));
                    OnPropertyChanged(nameof(UserRole));

                _openProfileCommand.RaiseCanExecuteChanged();
            }
            else
            {
                CommuneName = "Commune";
                UserIdentifier = "Non connecté";
                UserEmail = "Email non disponible";
                UserPhone = "Téléphone non disponible";
                    UserRole = "Non connecté";

                // Notifier les changements
                OnPropertyChanged(nameof(CommuneName));
                OnPropertyChanged(nameof(UserIdentifier));
                OnPropertyChanged(nameof(UserEmail));
                OnPropertyChanged(nameof(UserPhone));
                    OnPropertyChanged(nameof(UserRole));

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
                // Se désabonner de l'événement
                _exerciceService.ExerciceChanged -= OnExerciceChanged;

                _authService.Logout();
                _openProfileCommand.RaiseCanExecuteChanged();

                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();

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
                OnPropertyChanged(nameof(UserEmail));
                OnPropertyChanged(nameof(UserPhone));
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
            OnPropertyChanged(nameof(CurrentPageTitle));
        }

        /// <summary>
        /// Rafraîchir la liste des exercices (utile après ajout/modification)
        /// </summary>
        public async Task RefreshExercicesAsync()
        {
            await LoadExercicesAsync();
        }
    }
}