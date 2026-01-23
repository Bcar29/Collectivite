using System;
using System.Security.Cryptography;
using System.Text;

namespace Collectivite.Security
{
    /// <summary>
    /// Classe utilitaire pour le chiffrement et déchiffrement AES des chaînes de caractères.
    /// Utilise AES-256-CBC avec une clé dérivée d'un secret fixe.
    /// </summary>
    public static class CryptoHelper
    {
        // Clé secrète de base (en production, cette clé devrait être plus complexe)
        // Note: Cette clé est intégrée dans le code compilé, ce qui offre une protection basique
        // Pour une sécurité renforcée, considérez l'utilisation d'une clé dérivée de l'environnement
        private const string SecretKey = "Collectivite2025SecureKey!@#$%^&*()_+";
        
        private const int KeySize = 256; // AES-256
        private const int IvSize = 128; // 16 bytes pour l'IV
        private const int SaltSize = 32; // 32 bytes pour le salt

        /// <summary>
        /// Chiffre une chaîne de caractères en utilisant AES-256-CBC.
        /// </summary>
        /// <param name="plainText">Le texte à chiffrer</param>
        /// <returns>Le texte chiffré encodé en Base64</returns>
        /// <exception cref="ArgumentNullException">Si plainText est null ou vide</exception>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new ArgumentNullException(nameof(plainText), "Le texte à chiffrer ne peut pas être vide.");

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Génération d'un salt aléatoire
                    var salt = new byte[SaltSize];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(salt);
                    }

                    // Dérivation de la clé à partir du secret et du salt
                    var key = DeriveKey(SecretKey, salt, KeySize / 8);

                    // Génération d'un IV aléatoire
                    aes.GenerateIV();
                    var iv = aes.IV;

                    // Chiffrement
                    using (var encryptor = aes.CreateEncryptor(key, iv))
                    using (var msEncrypt = new System.IO.MemoryStream())
                    {
                        // Écriture du salt, puis de l'IV, puis des données chiffrées
                        msEncrypt.Write(salt, 0, salt.Length);
                        msEncrypt.Write(iv, 0, iv.Length);

                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Erreur lors du chiffrement.", ex);
            }
        }

        /// <summary>
        /// Déchiffre une chaîne de caractères chiffrée avec AES-256-CBC.
        /// </summary>
        /// <param name="cipherText">Le texte chiffré encodé en Base64</param>
        /// <returns>Le texte déchiffré</returns>
        /// <exception cref="ArgumentNullException">Si cipherText est null ou vide</exception>
        /// <exception cref="CryptographicException">Si le déchiffrement échoue</exception>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                throw new ArgumentNullException(nameof(cipherText), "Le texte chiffré ne peut pas être vide.");

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                // Extraction du salt (32 premiers bytes)
                var salt = new byte[SaltSize];
                Array.Copy(fullCipher, 0, salt, 0, SaltSize);

                // Extraction de l'IV (16 bytes suivants)
                var iv = new byte[IvSize / 8];
                Array.Copy(fullCipher, SaltSize, iv, 0, IvSize / 8);

                // Extraction des données chiffrées (reste)
                var cipher = new byte[fullCipher.Length - SaltSize - (IvSize / 8)];
                Array.Copy(fullCipher, SaltSize + (IvSize / 8), cipher, 0, cipher.Length);

                // Dérivation de la clé
                var key = DeriveKey(SecretKey, salt, KeySize / 8);

                // Déchiffrement
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor(key, iv))
                    using (var msDecrypt = new System.IO.MemoryStream(cipher))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Format de texte chiffré invalide.", ex);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Erreur lors du déchiffrement.", ex);
            }
        }

        /// <summary>
        /// Dérive une clé de la taille spécifiée à partir d'un secret et d'un salt en utilisant PBKDF2.
        /// </summary>
        private static byte[] DeriveKey(string secret, byte[] salt, int keyLength)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(secret, salt, 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keyLength);
            }
        }
    }
}
