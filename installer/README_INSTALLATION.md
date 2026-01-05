# Guide d'Installation - Collectivite v1.0.0

## Prérequis

Avant d'installer l'application, assurez-vous d'avoir :

1. **Windows 10/11 (64-bit)** ou Windows Server 2016+
2. **.NET 8.0 Runtime** (téléchargement automatique proposé si absent)
   - Lien: https://dotnet.microsoft.com/download/dotnet/8.0
3. **MySQL Server 8.0+** ou **MariaDB 10.5+** (doit être installé séparément)
   - MySQL: https://dev.mysql.com/downloads/mysql/
   - MariaDB: https://mariadb.org/download/

## Installation de MySQL/MariaDB

### Option 1: Installation standard
1. Téléchargez MySQL ou MariaDB depuis les liens ci-dessus
2. Installez avec les paramètres par défaut
3. Notez le mot de passe root (vous en aurez besoin pour configurer l'application)

### Option 2: Installation silencieuse (pour déploiement)
```powershell
# MySQL
mysql-installer-community-8.0.xx.x.msi /quiet /norestart

# Ou utilisez Chocolatey
choco install mysql -y
```

## Installation de l'application

### Méthode 1: Utiliser l'installeur (Recommandé)

1. **Créer l'installeur** :
   ```powershell
   cd installer
   .\build_installer.ps1
   ```

2. **Compiler avec Inno Setup** :
   - Installer Inno Setup Compiler: https://jrsoftware.org/isdl.php
   - Ouvrir `setup.iss` dans Inno Setup Compiler
   - Cliquer sur "Compile" (F9)
   - L'installeur sera créé dans `installer\Output\Collectivite-Setup-1.0.0.exe`

3. **Exécuter l'installeur** :
   - Double-cliquer sur `Collectivite-Setup-1.0.0.exe`
   - Suivre l'assistant d'installation
   - L'application sera installée dans `C:\Program Files\Collectivite\`

### Méthode 2: Installation manuelle

1. **Publier l'application** :
   ```powershell
   cd Collectivite\Collectivite
   dotnet publish -c Release -r win-x64 --self-contained true -o ..\..\installer\publish
   ```

2. **Copier les fichiers** :
   - Copier le contenu du dossier `installer\publish` vers le dossier d'installation souhaité
   - Par exemple: `C:\Program Files\Collectivite\`

## Configuration de la base de données

### 1. Créer la base de données

Connectez-vous à MySQL avec un client (MySQL Workbench, phpMyAdmin, ou ligne de commande) :

```sql
CREATE DATABASE collectivite CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 2. Configurer la chaîne de connexion

Éditez le fichier `appsettings.json` dans le dossier d'installation :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=collectivite;User=root;Password=VOTRE_MOT_DE_PASSE;"
  }
}
```

**Important** : Remplacez `VOTRE_MOT_DE_PASSE` par le mot de passe MySQL root.

### 3. Initialisation automatique

L'application créera automatiquement les tables et les données de base lors du premier lancement via `EnsureCreatedAsync()`.

## Premier lancement

1. Lancez l'application depuis le menu Démarrer ou le raccourci sur le bureau
2. La base de données sera automatiquement initialisée
3. Les données de base (nomenclatures, rôles, plan comptable) seront créées automatiquement

## Désinstallation

### Via l'installeur
- Panneau de configuration > Programmes > Désinstaller un programme
- Sélectionner "Collectivite" et cliquer sur Désinstaller

### Manuellement
- Supprimer le dossier d'installation
- Supprimer la base de données MySQL (optionnel) :
  ```sql
  DROP DATABASE collectivite;
  ```

## Dépannage

### L'application ne démarre pas
- Vérifiez que .NET 8.0 Runtime est installé
- Vérifiez les logs dans le dossier d'installation

### Erreur de connexion à la base de données
- Vérifiez que MySQL/MariaDB est démarré
- Vérifiez la chaîne de connexion dans `appsettings.json`
- Vérifiez que la base de données `collectivite` existe
- Vérifiez les identifiants MySQL (user/password)

### Réinitialiser la base de données
1. Supprimez la base de données :
   ```sql
   DROP DATABASE collectivite;
   CREATE DATABASE collectivite CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```
2. Relancez l'application (elle recréera les tables automatiquement)

## Support

Pour toute question ou problème, contactez le support technique.

