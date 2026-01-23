using Collectivite.Security;
using System;
using System.Windows;

namespace Collectivite.Security
{
    /// <summary>
    /// EXEMPLE D'UTILISATION - Nettoyage lors de la désinstallation
    /// 
    /// Ce fichier montre comment utiliser UninstallHelper pour nettoyer la configuration
    /// lors de la désinstallation de l'application.
    /// 
    /// ⚠️ Ce fichier est un exemple et peut être supprimé ou adapté selon vos besoins.
    /// </summary>
    public static class UninstallExample
    {
        /// <summary>
        /// Exemple 1 : Nettoyage simple sans confirmation
        /// Utilisez cette méthode dans votre processus de désinstallation automatisé.
        /// </summary>
        public static void Example1_SimpleCleanup()
        {
            // Appel simple du nettoyage
            bool success = UninstallHelper.CleanupConfiguration();

            if (success)
            {
                Console.WriteLine("Configuration supprimée avec succès.");
            }
            else
            {
                Console.WriteLine("Échec de la suppression de la configuration.");
            }
        }

        /// <summary>
        /// Exemple 2 : Nettoyage avec confirmation utilisateur
        /// Utilisez cette méthode dans une interface utilisateur (bouton de nettoyage).
        /// </summary>
        public static void Example2_CleanupWithConfirmation()
        {
            // Cette méthode affiche une boîte de dialogue de confirmation
            // et nettoie uniquement si l'utilisateur confirme
            bool success = UninstallHelper.CleanupConfigurationWithConfirmation();

            if (success)
            {
                // La configuration a été supprimée
                // Vous pouvez rediriger l'utilisateur vers la fenêtre de configuration
                // ou fermer l'application
            }
        }

        /// <summary>
        /// Exemple 3 : Intégration dans un gestionnaire d'événements de bouton
        /// À ajouter dans votre ViewModel ou code-behind.
        /// </summary>
        public static void Example3_ButtonClickHandler()
        {
            // Dans votre ViewModel ou code-behind :
            // 
            // private void OnCleanupButtonClick(object sender, RoutedEventArgs e)
            // {
            //     UninstallHelper.CleanupConfigurationWithConfirmation();
            // }
        }

        /// <summary>
        /// Exemple 4 : Utilisation dans un script de désinstallation (NSIS, WiX, etc.)
        /// 
        /// Pour NSIS (Nullsoft Scriptable Install System) :
        /// 
        /// Section "Uninstall"
        ///     ; Appeler votre application avec un paramètre spécial
        ///     ExecWait '"$INSTDIR\Collectivite.exe" /uninstall'
        /// SectionEnd
        /// 
        /// Dans votre App.xaml.cs, ajoutez :
        /// 
        /// protected override void OnStartup(StartupEventArgs e)
        /// {
        ///     // Vérifier si l'application est lancée en mode désinstallation
        ///     if (e.Args.Length > 0 && e.Args[0] == "/uninstall")
        ///     {
        ///         UninstallHelper.CleanupConfiguration();
        ///         Shutdown(0);
        ///         return;
        ///     }
        ///     
        ///     // ... reste du code de démarrage
        /// }
        /// </summary>
        public static void Example4_UninstallScript()
        {
            // Voir les commentaires dans la méthode pour l'implémentation
        }

        /// <summary>
        /// Exemple 5 : Utilisation dans un Custom Action WiX
        /// 
        /// Dans votre fichier .wxs :
        /// 
        /// <CustomAction Id="CleanupRegistry"
        ///               ExeCommand="[INSTALLDIR]Collectivite.exe /uninstall"
        ///               Directory="INSTALLDIR"
        ///               Execute="deferred"
        ///               Impersonate="no" />
        /// 
        /// <InstallExecuteSequence>
        ///     <Custom Action="CleanupRegistry" Before="RemoveFiles" />
        /// </InstallExecuteSequence>
        /// </summary>
        public static void Example5_WiXCustomAction()
        {
            // Voir les commentaires dans la méthode pour l'implémentation
        }
    }
}
