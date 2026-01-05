# Guide Rapide - Création de l'Installeur

## Étapes rapides pour créer l'installeur

### 1. Préparer l'icône ICO (optionnel mais recommandé)

L'installeur nécessite un fichier `.ico`. Si vous n'avez pas encore créé le fichier ICO :

1. Allez sur https://convertio.co/png-ico/
2. Uploadez `Collectivite\Collectivite\app_icon_256.png`
3. Téléchargez le fichier `.ico`
4. Renommez-le en `app_icon_256.ico` et placez-le dans `Collectivite\Collectivite\`
5. Dans `setup.iss`, décommentez la ligne `SetupIconFile=..\Collectivite\app_icon_256.ico`

### 2. Préparer les fichiers de publication

```powershell
cd installer
.\build_installer.ps1
```

Ce script va :
- Publier l'application en mode Release
- Copier tous les fichiers nécessaires dans `installer\publish\`

### 3. Installer Inno Setup Compiler

Si ce n'est pas déjà fait :
1. Téléchargez depuis : https://jrsoftware.org/isdl.php
2. Choisissez **"Current Release"**
3. Installez avec les paramètres par défaut

### 4. Créer l'installeur

**Option A : Via l'interface graphique**
1. Ouvrez Inno Setup Compiler
2. Fichier > Ouvrir
3. Sélectionnez `installer\setup.iss`
4. Compile > Compiler (ou F9)
5. L'installeur sera créé dans `installer\Output\Collectivite-Setup-1.0.0.exe`

**Option B : Via la ligne de commande**
```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
```

### 5. Tester l'installeur

1. Exécutez `installer\Output\Collectivite-Setup-1.0.0.exe`
2. Suivez l'assistant d'installation
3. Testez le lancement de l'application

## Structure des fichiers

```
installer/
├── build_installer.ps1          # Script de publication
├── setup.iss                     # Script Inno Setup
├── check_prerequisites.ps1       # Vérification des prérequis
├── README_INSTALLATION.md        # Guide d'installation détaillé
├── GUIDE_RAPIDE.md              # Ce fichier
├── publish/                      # Fichiers publiés (généré)
└── Output/                       # Installeur généré (généré)
```

## Notes importantes

- **MySQL/MariaDB** : Doit être installé séparément sur chaque machine
- **.NET 8.0 Runtime** : Peut être inclus dans l'installeur ou installé séparément
- **Base de données** : L'application crée automatiquement la base de données au premier lancement
- **Configuration** : Le fichier `appsettings.json` doit être édité pour configurer la connexion MySQL

## Personnalisation

Pour personnaliser l'installeur, éditez `setup.iss` :
- `MyAppPublisher` : Nom de l'éditeur
- `MyAppURL` : URL du site web
- `AppId` : Identifiant unique (ne pas changer après la première publication)
- Autres options dans la section `[Setup]`

