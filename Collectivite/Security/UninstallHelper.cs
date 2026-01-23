using System;
using System.Windows;

namespace Collectivite.Security
{
    /// <summary>
    /// Classe utilitaire pour le nettoyage lors de la désinstallation de l'application.
    /// Supprime toutes les données de configuration stockées dans le registre Windows.
    /// </summary>
    public static class UninstallHelper
    {
        /// <summary>
        /// Nettoie complètement la configuration de l'application depuis le registre.
        /// À appeler lors de la désinstallation ou via un bouton de nettoyage.
        /// </summary>
        /// <returns>True si le nettoyage a réussi, False sinon</returns>
        public static bool CleanupConfiguration()
        {
            try
            {
                return RegistryManager.DeleteConfiguration();
            }
            catch (Exception ex)
            {
                // Log l'erreur si nécessaire
                System.Diagnostics.Debug.WriteLine($"Erreur lors du nettoyage : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Nettoie la configuration avec confirmation utilisateur (pour utilisation via UI).
        /// </summary>
        /// <returns>True si l'utilisateur a confirmé et le nettoyage a réussi</returns>
        public static bool CleanupConfigurationWithConfirmation()
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir supprimer toutes les configurations de connexion ?\n\n" +
                "Cette action est irréversible et vous devrez reconfigurer le serveur au prochain lancement.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var success = CleanupConfiguration();
                
                if (success)
                {
                    MessageBox.Show(
                        "La configuration a été supprimée avec succès.",
                        "Suppression réussie",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        "Une erreur s'est produite lors de la suppression de la configuration.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }
    }
}
