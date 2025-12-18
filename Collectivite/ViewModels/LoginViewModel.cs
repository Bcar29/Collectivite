using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanLogin());
        }

        public string Username
        {
            get => _username; 
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ErrorMessage = string.Empty;
                    ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ErrorMessage = string.Empty;
                    ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand LoginCommand { get; }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !IsLoading;
        }
       


        private async Task LoginAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var (success, message, user) = await _authService.AuthenticateAsync(Username, Password);

            IsLoading = false;

            if (success && user != null)
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };
                var json = JsonSerializer.Serialize(user, options);
                Properties.Settings.Default.UserJson = json;
                Properties.Settings.Default.CommuneId = user.CommuneId ?? 0;
                Properties.Settings.Default.Save();
                // Fermer la fenêtre de connexion et ouvrir la fenêtre principale
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = new Collectivite.MainWindow(_authService);
                    mainWindow.Show();

                    // Fermer la fenêtre de connexion
                    Application.Current.Windows.OfType<Window>()
                        .FirstOrDefault(w => w is Collectivite.Views.LoginWindow)?.Close();
                });
            }
            else
            {
                ErrorMessage = message;
                MessageBox.Show(message);
            }
        }
    }
}
