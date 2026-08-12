using Collectivite.Security;
using Collectivite.Utils;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour la fenêtre de configuration du serveur de base de données.
    /// Gère la saisie et la sauvegarde sécurisée des paramètres de connexion.
    /// </summary>
    public class ServerConfigurationViewModel : ViewModelBase
    {
        private string _server = string.Empty;
        private string _database = string.Empty;
        private string _user = string.Empty;
        private string _password = string.Empty;
        private string _sslMode = "Required";
        private int _connectTimeout = 30;
        private string _connectTimeoutText = "30";
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        public ServerConfigurationViewModel()
        {
            SaveCommand = new RelayCommand(async _ => 
            {
                await SaveConfigurationAsync();
            }, _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        #region Propriétés

        public string Server
        {
            get => _server;
            set
            {
                if (SetProperty(ref _server, value))
                {
                    ErrorMessage = string.Empty;
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string Database
        {
            get => _database;
            set
            {
                if (SetProperty(ref _database, value))
                {
                    ErrorMessage = string.Empty;
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string User
        {
            get => _user;
            set
            {
                if (SetProperty(ref _user, value))
                {
                    ErrorMessage = string.Empty;
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
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
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string SslMode
        {
            get => _sslMode;
            set
            {
                if (SetProperty(ref _sslMode, value))
                {
                    ErrorMessage = string.Empty;
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public int ConnectTimeout
        {
            get => _connectTimeout;
            private set
            {
                if (SetProperty(ref _connectTimeout, value))
                {
                    ConnectTimeoutText = value.ToString();
                    ErrorMessage = string.Empty;
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string ConnectTimeoutText
        {
            get => _connectTimeoutText;
            set
            {
                if (SetProperty(ref _connectTimeoutText, value))
                {
                    ErrorMessage = string.Empty;
                    
                    // Tenter de convertir en int
                    if (int.TryParse(value, out int timeout))
                    {
                        if (timeout >= 1 && timeout <= 300)
                        {
                            _connectTimeout = timeout;
                        }
                        else
                        {
                            ErrorMessage = "Le timeout doit être entre 1 et 300 secondes.";
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(value))
                    {
                        ErrorMessage = "Le timeout doit être un nombre valide.";
                    }
                    
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
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
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Liste des modes SSL disponibles pour le ComboBox
        /// </summary>
        public List<string> SslModes { get; } = new List<string>
        {
            "None",
            "Preferred",
            "Required",
            "VerifyCA",
            "VerifyFull"
        };

        #endregion

        #region Commandes

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Méthodes privées

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Server) &&
                   !string.IsNullOrWhiteSpace(Database) &&
                   !string.IsNullOrWhiteSpace(User) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(SslMode) &&
                   ConnectTimeout > 0 &&
                   ConnectTimeout <= 300 &&
                   !IsLoading;
        }

        private async Task SaveConfigurationAsync()
        {
            if (IsLoading)
                return;

            try
            {
                // Validation des champs
                if (!CanSave())
                {
                    ErrorMessage = "Veuillez remplir tous les champs obligatoires.";
                    return;
                }

                IsLoading = true;
                ErrorMessage = string.Empty;

                // Construction de la chaîne de connexion
                var connectionString = RegistryManager.BuildConnectionString(Server, Database, User, Password, SslMode, ConnectTimeout);

                // Chiffrement de la chaîne de connexion
                var encryptedConnectionString = CryptoHelper.Encrypt(connectionString);

                // Sauvegarde dans le registre
                RegistryManager.SaveConnectionString(encryptedConnectionString);

                // Test de connexion avec timeout
                ErrorMessage = "Test de connexion en cours...";
                await TestConnectionAsync(connectionString);

                // Fermeture de la fenêtre avec succès
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = Application.Current.Windows.OfType<Views.ServerConfigurationWindow>()
                        .FirstOrDefault();
                    if (window != null)
                    {
                        window.DialogResult = true;
                        window.Close();
                    }
                });
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorMessage = $"Erreur d'accès au registre : {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de la sauvegarde : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task TestConnectionAsync(string connectionString)
        {
            // Utiliser un CancellationTokenSource avec timeout
            using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                try
                {
                    // Test de connexion avec EF Core en utilisant un DbContextOptionsBuilder temporaire
                    var optionsBuilder = new DbContextOptionsBuilder<Services.AppDbContext>();
                    optionsBuilder.UseMySql(
                        connectionString,
                        new MySqlServerVersion(new Version(8, 0, 0)),
                        mySqlOptions =>
                        {
                            mySqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 1,
                                maxRetryDelay: TimeSpan.FromSeconds(2),
                                errorNumbersToAdd: null
                            );
                        });

                    using (var db = new Services.AppDbContext(optionsBuilder.Options))
                    {
                        // Tenter une opération simple pour vérifier la connexion avec timeout
                        var canConnect = await db.Database.CanConnectAsync(cts.Token);
                        if (!canConnect)
                        {
                            throw new InvalidOperationException("La connexion à la base de données a échoué.");
                        }
                    }
                }
                catch (System.Threading.Tasks.TaskCanceledException)
                {
                    throw new InvalidOperationException("Le test de connexion a expiré (timeout de 10 secondes). Vérifiez vos paramètres de connexion.");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Impossible de se connecter à la base de données : {ex.Message}", ex);
                }
            }
        }

        private void Cancel()
        {
            bool hasUnsavedInput = !string.IsNullOrWhiteSpace(Server) ||
                                    !string.IsNullOrWhiteSpace(Database) ||
                                    !string.IsNullOrWhiteSpace(User) ||
                                    !string.IsNullOrWhiteSpace(Password);

            if (hasUnsavedInput &&
                MessageBox.Show(
                    "Des informations ont été saisies et seront perdues. Voulez-vous vraiment annuler ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.Windows.OfType<Views.ServerConfigurationWindow>()
                    .FirstOrDefault();
                if (window != null)
                {
                    window.DialogResult = false;
                    window.Close();
                }
            });
        }

        #endregion
    }
}
