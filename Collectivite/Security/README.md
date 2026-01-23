# Module de Sécurité - Gestion Sécurisée de la Configuration

Ce module gère de manière sécurisée la chaîne de connexion à la base de données MySQL en utilisant le chiffrement AES et le stockage dans le registre Windows.

## 📁 Structure du Module

### `CryptoHelper.cs`
Classe statique pour le chiffrement et déchiffrement AES-256-CBC des chaînes de caractères.

**Méthodes principales :**
- `Encrypt(string plainText)` : Chiffre une chaîne de caractères
- `Decrypt(string cipherText)` : Déchiffre une chaîne chiffrée

**Caractéristiques :**
- Utilise AES-256-CBC avec PKCS7 padding
- Génère un salt aléatoire pour chaque chiffrement
- Utilise PBKDF2 avec SHA-256 pour la dérivation de clé (10000 itérations)
- Stocke le salt et l'IV avec les données chiffrées

### `RegistryManager.cs`
Gestionnaire pour les opérations sur le registre Windows.

**Méthodes principales :**
- `ConfigurationExists()` : Vérifie si la configuration existe
- `SaveConnectionString(string encryptedConnectionString)` : Sauvegarde la chaîne chiffrée
- `GetConnectionString()` : Récupère la chaîne chiffrée
- `DeleteConfiguration()` : Supprime complètement la configuration
- `BuildConnectionString(string server, string database, string user, string password)` : Construit une chaîne MySQL

**Emplacement dans le registre :**
- Chemin : `HKEY_CURRENT_USER\SOFTWARE\MonLogiciel`
- Clé : `ConnectionString`

### `UninstallHelper.cs`
Classe utilitaire pour le nettoyage lors de la désinstallation.

**Méthodes principales :**
- `CleanupConfiguration()` : Nettoie sans confirmation
- `CleanupConfigurationWithConfirmation()` : Nettoie avec confirmation utilisateur

## 🔐 Sécurité

### Chiffrement
- **Algorithme** : AES-256-CBC
- **Dérivation de clé** : PBKDF2 avec SHA-256 (10000 itérations)
- **Salt** : 32 bytes aléatoires par chiffrement
- **IV** : 16 bytes aléatoires par chiffrement

### Stockage
- Les données sont stockées dans le registre Windows sous `HKEY_CURRENT_USER`
- Seul l'utilisateur actuel peut accéder à ses propres données
- La chaîne de connexion n'est jamais stockée en clair

## 🚀 Utilisation

### Configuration initiale
La fenêtre de configuration s'affiche automatiquement au premier lancement si aucune configuration n'est trouvée dans le registre.

### Modification de la configuration
Pour modifier la configuration, supprimez la clé du registre ou utilisez `UninstallHelper.CleanupConfiguration()` puis relancez l'application.

### Nettoyage lors de la désinstallation

#### Exemple 1 : Dans un script de désinstallation
```csharp
// Dans votre processus de désinstallation
UninstallHelper.CleanupConfiguration();
```

#### Exemple 2 : Avec confirmation utilisateur
```csharp
// Dans un bouton de votre interface
UninstallHelper.CleanupConfigurationWithConfirmation();
```

#### Exemple 3 : Via ligne de commande
```csharp
// Dans App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    if (e.Args.Length > 0 && e.Args[0] == "/uninstall")
    {
        UninstallHelper.CleanupConfiguration();
        Shutdown(0);
        return;
    }
    // ... reste du code
}
```

Voir `UninstallExample.cs` pour plus d'exemples détaillés.

## ⚠️ Notes importantes

1. **Clé de chiffrement** : La clé secrète est intégrée dans le code compilé. Pour une sécurité renforcée en production, considérez l'utilisation d'une clé dérivée de l'environnement ou d'un module de sécurité matériel.

2. **Compatibilité** : Le système maintient un fallback vers `appsettings.json` pour la compatibilité en développement, mais cette fonctionnalité ne devrait pas être utilisée en production.

3. **Permissions** : L'application nécessite les permissions d'écriture dans le registre utilisateur (`HKEY_CURRENT_USER`).

4. **Migration** : Si vous migrez depuis `appsettings.json`, la configuration sera automatiquement migrée vers le registre au premier lancement après la configuration.

## 🔧 Intégration avec Entity Framework Core

Le `AppDbContext` a été modifié pour :
1. Lire la configuration depuis le registre en priorité
2. Déchiffrer automatiquement la chaîne de connexion
3. Utiliser `appsettings.json` comme fallback uniquement si aucune configuration n'est trouvée

## 📝 Fichiers modifiés

- `Services/AppDbContext.cs` : Lecture depuis le registre
- `App.xaml.cs` : Vérification de la configuration au démarrage
- `ViewModels/ServerConfigurationViewModel.cs` : Gestion de la configuration
- `Views/ServerConfigurationWindow.xaml` : Interface de configuration

## 🎯 Architecture MVVM

Le module respecte l'architecture MVVM existante :
- **View** : `ServerConfigurationWindow.xaml` et `.xaml.cs`
- **ViewModel** : `ServerConfigurationViewModel.cs` (hérite de `ViewModelBase`)
- **Model/Services** : `CryptoHelper`, `RegistryManager` (logique métier isolée)
