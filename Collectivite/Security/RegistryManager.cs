using Microsoft.Win32;
using System;
using System.Security;

namespace Collectivite.Security
{
    /// <summary>
    /// Gestionnaire pour les opérations sur le registre Windows.
    /// Stocke et récupère la chaîne de connexion chiffrée dans HKEY_CURRENT_USER\SOFTWARE\MonLogiciel
    /// </summary>
    public static class RegistryManager
    {
        private const string RegistryPath = @"SOFTWARE\MonLogiciel";
        private const string ConnectionStringKey = "ConnectionString";

        /// <summary>
        /// Vérifie si la configuration existe dans le registre.
        /// </summary>
        /// <returns>True si la clé et la valeur existent, False sinon</returns>
        public static bool ConfigurationExists()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key == null)
                        return false;

                    var value = key.GetValue(ConnectionStringKey);
                    return value != null && !string.IsNullOrWhiteSpace(value.ToString());
                }
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Sauvegarde la chaîne de connexion chiffrée dans le registre.
        /// </summary>
        /// <param name="encryptedConnectionString">La chaîne de connexion déjà chiffrée</param>
        /// <exception cref="ArgumentNullException">Si encryptedConnectionString est null ou vide</exception>
        /// <exception cref="UnauthorizedAccessException">Si l'accès au registre est refusé</exception>
        public static void SaveConnectionString(string encryptedConnectionString)
        {
            if (string.IsNullOrWhiteSpace(encryptedConnectionString))
                throw new ArgumentNullException(nameof(encryptedConnectionString), "La chaîne chiffrée ne peut pas être vide.");

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (key == null)
                        throw new InvalidOperationException($"Impossible de créer ou d'ouvrir la clé de registre : {RegistryPath}");

                    key.SetValue(ConnectionStringKey, encryptedConnectionString, RegistryValueKind.String);
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException("Accès refusé au registre. Vérifiez les permissions de l'application.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de la sauvegarde dans le registre : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Récupère la chaîne de connexion chiffrée depuis le registre.
        /// </summary>
        /// <returns>La chaîne chiffrée, ou null si elle n'existe pas</returns>
        /// <exception cref="InvalidOperationException">Si la configuration n'existe pas</exception>
        public static string? GetConnectionString()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key == null)
                        return null;

                    var value = key.GetValue(ConnectionStringKey);
                    return value?.ToString();
                }
            }
            catch (SecurityException ex)
            {
                throw new SecurityException("Accès refusé au registre.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de la lecture du registre : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Supprime complètement la clé MonLogiciel du registre.
        /// Utilisé lors de la désinstallation ou du nettoyage.
        /// </summary>
        /// <returns>True si la suppression a réussi, False sinon</returns>
        public static bool DeleteConfiguration()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true))
                {
                    if (key != null)
                    {
                        // Supprimer la valeur
                        try
                        {
                            key.DeleteValue(ConnectionStringKey, throwOnMissingValue: false);
                        }
                        catch
                        {
                            // Ignorer si la valeur n'existe pas
                        }
                    }
                }

                // Supprimer la clé complète
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, throwOnMissingSubKey: false);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Construit une chaîne de connexion MySQL à partir des paramètres individuels.
        /// </summary>
        /// <param name="server">Nom du serveur ou adresse IP</param>
        /// <param name="database">Nom de la base de données</param>
        /// <param name="user">Nom d'utilisateur</param>
        /// <param name="password">Mot de passe</param>
        /// <param name="sslMode">Mode SSL (None, Preferred, Required, VerifyCA, VerifyFull). Par défaut: Preferred</param>
        /// <param name="connectTimeout">Timeout de connexion en secondes. Par défaut: 30</param>
        /// <returns>Chaîne de connexion MySQL formatée</returns>
        public static string BuildConnectionString(string server, string database, string user, string password, string sslMode = "Preferred", int connectTimeout = 30)
        {
            if (string.IsNullOrWhiteSpace(server))
                throw new ArgumentException("Le serveur ne peut pas être vide.", nameof(server));
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("La base de données ne peut pas être vide.", nameof(database));
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentException("L'utilisateur ne peut pas être vide.", nameof(user));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Le mot de passe ne peut pas être vide.", nameof(password));

            // Validation du SslMode
            var validSslModes = new[] { "None", "Preferred", "Required", "VerifyCA", "VerifyFull" };
            if (!string.IsNullOrWhiteSpace(sslMode) && !validSslModes.Contains(sslMode, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"SslMode invalide. Valeurs acceptées : {string.Join(", ", validSslModes)}", nameof(sslMode));
            }

            // Validation du ConnectTimeout
            if (connectTimeout < 1 || connectTimeout > 300)
            {
                throw new ArgumentException("Le timeout de connexion doit être entre 1 et 300 secondes.", nameof(connectTimeout));
            }

            // Échapper les caractères spéciaux dans le mot de passe si nécessaire
            var escapedPassword = password.Replace(";", "\\;").Replace("=", "\\=");

            // Construire la chaîne de connexion avec tous les paramètres
            var connectionString = $"Server={server};Database={database};User={user};Password={escapedPassword};";
            
            // Ajouter SslMode si spécifié
            if (!string.IsNullOrWhiteSpace(sslMode))
            {
                connectionString += $"SslMode={sslMode};";
            }
            
            // Ajouter ConnectTimeout
            connectionString += $"Connection Timeout={connectTimeout};";

            return connectionString;
        }
    }
}
