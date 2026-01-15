using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Collectivite.Services
{
    /// <summary>
    /// Système de protection offline basé sur :
    /// - Date d'installation (InstallDateUtc)
    /// - Durée d'expiration (InstallDateUtc + N jours)
    /// - Détection de manipulation de l'horloge (LastRunDateUtc)
    /// - Empreinte machine (CPU + disque système)
    /// - Stockage chiffré (AES + PBKDF2) dans Registre + fichier AppData
    /// </summary>
    public class LicenseManager
    {
        private const string RegistryPath = @"Software\Collectivite\LicenseGuard";
        private const string RegistryValueName = "LicenseData";

        private const string AppFolderName = "Collectivite";
        private const string LicenseFileName = "license.dat";

        // Durée de validité (en jours)
        private const int ExpirationDays = 180;

        // Petite marge autorisée sur l'heure pour éviter des faux positifs
        private static readonly TimeSpan ClockDriftTolerance = TimeSpan.FromMinutes(5);

        // Secrets intégrés (légèrement obfusqués pour éviter le texte en clair)
        // NOTE : pour une vraie appli commerciale, ces valeurs devraient être encore plus protégées
        private static readonly string Passphrase = string.Concat("C0ll", "ecti", "vite_2026", "#Lic");
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("C0ll3ct1vit3_S@lt_v1");

        /// <summary>
        /// Point d'entrée principal.
        /// Retourne false si la licence est invalide / expirée / soupçon de triche.
        /// </summary>
        public bool CheckLicense(out string blockReason)
        {
            blockReason = string.Empty;

            try
            {
                var nowUtc = DateTime.UtcNow;

                // 1) Empreinte machine actuelle
                string machineFingerprint = GetMachineFingerprint();
                if (string.IsNullOrWhiteSpace(machineFingerprint))
                {
                    blockReason = "Impossible de calculer l'empreinte de la machine.";
                    return false;
                }

                // 2) Lecture des deux stockages
                var registryData = LoadFromRegistry();
                var fileData = LoadFromFile();

                bool hasRegistry = registryData != null;
                bool hasFile = fileData != null;

                // 3) Première exécution : rien de stocké
                if (!hasRegistry && !hasFile)
                {
                    var initialData = new LicenseData
                    {
                        InstallDateUtc = nowUtc,
                        LastRunDateUtc = nowUtc,
                        MachineFingerprint = machineFingerprint
                    };

                    SaveAll(initialData);
                    return true;
                }

                LicenseData data;

                // 4) Cas où seul le registre est disponible : on le considère comme source principale
                if (hasRegistry && !hasFile)
                {
                    data = registryData!;
                }
                // 5) Cas où seul le fichier est disponible : on restaure le registre à partir du fichier
                else if (!hasRegistry && hasFile)
                {
                    data = fileData!;
                    SaveToRegistry(Encrypt(data.ToJson()));
                }
                else
                {
                    // 6) Les deux présents : vérification et réconciliation si nécessaire
                    data = registryData!;

                    if (!data.IsEquivalentTo(fileData!))
                    {
                        // Tentative de réconciliation : si seule LastRunDateUtc diffère
                        // (par exemple après manipulation de date), on prend la plus récente
                        if (data.InstallDateUtc == fileData!.InstallDateUtc
                            && string.Equals(data.MachineFingerprint, fileData.MachineFingerprint, StringComparison.OrdinalIgnoreCase)
                            && data.LastRunDateUtc != fileData.LastRunDateUtc)
                        {
                            // Seule LastRunDateUtc diffère → prendre la plus récente
                            data.LastRunDateUtc = data.LastRunDateUtc > fileData.LastRunDateUtc
                                ? data.LastRunDateUtc
                                : fileData.LastRunDateUtc;

                            // Réécrire les deux stockages pour les resynchroniser
                            SaveAll(data);
                        }
                        else
                        {
                            // Différences critiques (InstallDate ou Fingerprint) → blocage
                            blockReason = "Données de licence altérées ou corrompues.";
                            return false;
                        }
                    }
                }

                // 7) Vérification empreinte machine (anti-copie)
                if (!string.Equals(data.MachineFingerprint, machineFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    blockReason = "La licence n'est pas valide pour cette machine.";
                    return false;
                }

                // 8) Calcul de la date d'expiration (toujours InstallDate + N jours)
                var expirationDateUtc = data.InstallDateUtc.AddDays(ExpirationDays);

                // 9) Vérification de la manipulation de l'horloge
                if (nowUtc + ClockDriftTolerance < data.LastRunDateUtc)
                {
                    // Cas particulier : l'utilisateur a remis une date *plus basse* que LastRun,
                    // mais toujours dans la fenêtre de validité de la licence.
                    // On considère ici que c'est une correction manuelle de l'horloge
                    // et non une tentative de fraude durable.
                    if (nowUtc >= data.InstallDateUtc && nowUtc <= expirationDateUtc)
                    {
                        // On ramène LastRunDate à la date "corrigée" plutôt que de bloquer définitivement.
                        data.LastRunDateUtc = nowUtc;
                    }
                    else
                    {
                        blockReason = "Horloge système modifiée (retour arrière détecté).";
                        return false;
                    }
                }

                // 10) Vérification de l'expiration
                if (nowUtc > expirationDateUtc)
                {
                    blockReason = "Durée de licence expirée.";
                    return false;
                }

                // 11) Tout est OK → mise à jour LastRunDate
                data.LastRunDateUtc = nowUtc;
                SaveAll(data);

                return true;
            }
            catch (Exception ex)
            {
                // En cas d'erreur inattendue, on préfère bloquer plutôt que laisser passer,
                // mais on log l'erreur pour faciliter le diagnostic.
                LogInternalError(ex);
                blockReason = $"Erreur interne du système de licence : {ex.Message}";
                return false;
            }
        }

        #region Stockage chiffré

        private static void SaveAll(LicenseData data)
        {
            var json = data.ToJson();
            var encrypted = Encrypt(json);

            SaveToRegistry(encrypted);
            SaveToFile(encrypted);
        }

        private static LicenseData? LoadFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
                if (key == null)
                    return null;

                var base64 = key.GetValue(RegistryValueName) as string;
                if (string.IsNullOrWhiteSpace(base64))
                    return null;

                var json = Decrypt(base64);
                return LicenseData.FromJson(json);
            }
            catch
            {
                return null;
            }
        }

        private static LicenseData? LoadFromFile()
        {
            try
            {
                var path = GetLicenseFilePath();
                if (!File.Exists(path))
                    return null;

                var base64 = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(base64))
                    return null;

                var json = Decrypt(base64);
                return LicenseData.FromJson(json);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveToRegistry(string base64Data)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
            key.SetValue(RegistryValueName, base64Data, RegistryValueKind.String);
        }

        private static void SaveToFile(string base64Data)
        {
            try
            {
                var path = GetLicenseFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(path, base64Data, Encoding.UTF8);

                // Rendre le fichier caché pour le rendre moins visible
                File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.NotContentIndexed);
            }
            catch
            {
                // Ignorer les erreurs d'attributs : le contenu chiffré reste la vraie protection
            }
        }

        private static string GetLicenseFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppFolderName, LicenseFileName);
        }

        #endregion

        #region Chiffrement / Dérivation de clé

        private static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Dérivation de la clé avec PBKDF2
            using var pbkdf2 = new Rfc2898DeriveBytes(Passphrase, Salt, 100_000, HashAlgorithmName.SHA256);
            aes.Key = pbkdf2.GetBytes(aes.KeySize / 8);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Stocker IV + ciphertext en Base64
            var combined = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        private static string Decrypt(string base64Data)
        {
            var combined = Convert.FromBase64String(base64Data);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var pbkdf2 = new Rfc2898DeriveBytes(Passphrase, Salt, 100_000, HashAlgorithmName.SHA256);
            aes.Key = pbkdf2.GetBytes(aes.KeySize / 8);

            var ivSize = aes.BlockSize / 8;
            var iv = new byte[ivSize];
            var cipherBytes = new byte[combined.Length - ivSize];

            Buffer.BlockCopy(combined, 0, iv, 0, ivSize);
            Buffer.BlockCopy(combined, ivSize, cipherBytes, 0, cipherBytes.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        #endregion

        #region Journalisation interne

        private static void LogInternalError(Exception ex)
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dir = Path.Combine(appData, AppFolderName);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var logPath = Path.Combine(dir, "license_error.log");
                var lines = new[]
                {
                    "======================",
                    DateTime.UtcNow.ToString("O"),
                    ex.ToString()
                };

                File.AppendAllLines(logPath, lines);
            }
            catch
            {
                // Ne jamais lancer d'exception depuis le logger
            }
        }

        #endregion

        #region Empreinte machine

        private static string GetMachineFingerprint()
        {
            try
            {
                var cpuId = GetCpuId();
                var diskSerial = GetSystemDiskSerial();

                if (string.IsNullOrWhiteSpace(cpuId) || string.IsNullOrWhiteSpace(diskSerial))
                    return string.Empty;

                var composite = $"{cpuId}|{diskSerial}";

                // Hacher l'empreinte pour éviter de stocker les valeurs brutes
                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(composite);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string? GetCpuId()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select ProcessorId from Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    var id = obj["ProcessorId"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(id))
                        return id;
                }
            }
            catch
            {
                // Ignorer et retourner null
            }
            return null;
        }

        private static string? GetSystemDiskSerial()
        {
            try
            {
                // Récupérer le lecteur système (ex : "C:")
                var systemDrive = Path.GetPathRoot(Environment.SystemDirectory);
                if (string.IsNullOrWhiteSpace(systemDrive))
                    return null;

                systemDrive = systemDrive.TrimEnd('\\');

                using var searcher = new ManagementObjectSearcher("SELECT DeviceID, VolumeSerialNumber FROM Win32_LogicalDisk WHERE DriveType=3");
                foreach (ManagementObject disk in searcher.Get())
                {
                    var deviceId = disk["DeviceID"]?.ToString();
                    if (string.Equals(deviceId, systemDrive, StringComparison.OrdinalIgnoreCase))
                    {
                        var serial = disk["VolumeSerialNumber"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(serial))
                            return serial;
                    }
                }
            }
            catch
            {
                // Ignorer et retourner null
            }
            return null;
        }

        #endregion

        #region Modèle interne

        private sealed class LicenseData
        {
            public DateTime InstallDateUtc { get; set; }
            public DateTime LastRunDateUtc { get; set; }
            public string MachineFingerprint { get; set; } = string.Empty;

            public string ToJson()
            {
                // Format JSON minimaliste, sans dépendance externe
                // Dates en ISO 8601 (UTC)
                var install = InstallDateUtc.ToString("O");
                var lastRun = LastRunDateUtc.ToString("O");
                var escapedFingerprint = MachineFingerprint.Replace("\\", "\\\\").Replace("\"", "\\\"");

                return $"{{\"install\":\"{install}\",\"last\":\"{lastRun}\",\"fp\":\"{escapedFingerprint}\"}}";
            }

            public static LicenseData? FromJson(string json)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                try
                {
                    // Parsing manuel très simple, suffisant pour la structure ci-dessus
                    string GetValue(string key)
                    {
                        var token = $"\"{key}\":\"";
                        var start = json.IndexOf(token, StringComparison.Ordinal);
                        if (start < 0) return string.Empty;
                        start += token.Length;
                        var end = json.IndexOf('"', start);
                        if (end < 0) return string.Empty;
                        return json.Substring(start, end - start)
                                   .Replace("\\\"", "\"")
                                   .Replace("\\\\", "\\");
                    }

                    var installStr = GetValue("install");
                    var lastStr = GetValue("last");
                    var fpStr = GetValue("fp");

                    if (!DateTime.TryParse(installStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var install))
                        return null;
                    if (!DateTime.TryParse(lastStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var last))
                        return null;

                    if (string.IsNullOrWhiteSpace(fpStr))
                        return null;

                    return new LicenseData
                    {
                        InstallDateUtc = install,
                        LastRunDateUtc = last,
                        MachineFingerprint = fpStr
                    };
                }
                catch
                {
                    return null;
                }
            }

            public bool IsEquivalentTo(LicenseData other)
            {
                if (other == null) return false;

                // On exige une égalité stricte sur l'empreinte et la date d'installation.
                // Pour LastRunDate, une petite différence peut exister si un enregistrement a échoué,
                // mais ici on garde la comparaison stricte pour simplifier et renforcer la détection.
                return InstallDateUtc == other.InstallDateUtc
                       && LastRunDateUtc == other.LastRunDateUtc
                       && string.Equals(MachineFingerprint, other.MachineFingerprint, StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion
    }
}


