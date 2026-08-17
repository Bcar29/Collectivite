using Collectivite.Security;
using Collectivite.Services;
using Collectivite.Utils;
using Collectivite.Views;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Collectivite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly string CrashLogPath = Path.Combine(Path.GetTempPath(), "collectivite_crash.log");

        private static void LogCrash(string source, Exception ex)
        {
            try
            {
                File.AppendAllText(CrashLogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {source}{Environment.NewLine}{ex}{Environment.NewLine}{new string('=', 80)}{Environment.NewLine}");
            }
            catch
            {
                // ignorer les erreurs d'écriture de log
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, args) =>
            {
                LogCrash("DispatcherUnhandledException", args.Exception);
                MessageBox.Show(
                    $"Une erreur inattendue est survenue :\n\n{args.Exception.Message}\n\nDétails enregistrés dans :\n{CrashLogPath}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    LogCrash("AppDomain.UnhandledException", ex);
            };

            base.OnStartup(e);

            // Vérification de la configuration du serveur de base de données
            if (!RegistryManager.ConfigurationExists())
            {
                // Affichage de la fenêtre de configuration au premier lancement
                var configWindow = new ServerConfigurationWindow();
                var result = configWindow.ShowDialog();

                // Si l'utilisateur annule ou ferme sans configurer, on arrête l'application
                if (result != true)
                {
                    MessageBox.Show(
                        "La configuration du serveur est requise pour utiliser l'application.",
                        "Configuration requise",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Shutdown(0);
                    return;
                }
            }

            try
            {
                // Test de connexion avec la configuration
                using var db = new AppDbContext();
                
                // Vérification de la connexion
                if (!await db.Database.CanConnectAsync())
                {
                    MessageBox.Show(
                        "Impossible de se connecter à la base de données. " +
                        "Veuillez vérifier votre configuration.",
                        "Erreur de connexion",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }

                // Initialisation de la base de données
                await db.Database.EnsureCreatedAsync();

                // Seeds dans l'ordre
                SeedNomenclature.Seed(db);
                SeedRolesPermissions.Seed(db);

                // Seed du plan comptable
                var seedPlanComptable = new SeedPlanComptable(db);
                await seedPlanComptable.SeedCompteComptablesAsync();
            }
            catch (InvalidOperationException ex)
            {
                // Erreur de configuration - proposer de reconfigurer
                var response = MessageBox.Show(
                    $"Erreur de configuration : {ex.Message}\n\n" +
                    "Souhaitez-vous reconfigurer le serveur de base de données ?",
                    "Erreur de configuration",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (response == MessageBoxResult.Yes)
                {
                    // Supprimer l'ancienne configuration
                    RegistryManager.DeleteConfiguration();
                    
                    // Afficher la fenêtre de configuration
                    var configWindow = new ServerConfigurationWindow();
                    var result = configWindow.ShowDialog();

                    if (result == true)
                    {
                        // Relancer l'initialisation après reconfiguration
                        try
                        {
                            using var db = new AppDbContext();
                            
                            if (!await db.Database.CanConnectAsync())
                            {
                                MessageBox.Show(
                                    "Impossible de se connecter à la base de données avec la nouvelle configuration.",
                                    "Erreur de connexion",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                Shutdown(1);
                                return;
                            }

                            await db.Database.EnsureCreatedAsync();
                            SeedNomenclature.Seed(db);
                            Utils.SeedRolesPermissions.Seed(db);
                            var seedPlanComptable = new SeedPlanComptable(db);
                            await seedPlanComptable.SeedCompteComptablesAsync();
                        }
                        catch (Exception ex2)
                        {
                            MessageBox.Show(
                                $"Erreur après reconfiguration : {ex2.Message}",
                                "Erreur",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            Shutdown(1);
                        }
                    }
                    else
                    {
                        Shutdown(0);
                    }
                }
                else
                {
                    Shutdown(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur d'initialisation : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}