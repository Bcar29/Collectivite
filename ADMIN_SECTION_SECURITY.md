# Sécurisation de la Section d'Administration

## Objectif
Rendre la section d'administration (Rôles, Permissions, Utilisateurs) du sidebar invisible pour tous les rôles sauf le **Maire**, à moins qu'il n'accorde explicitement la permission `Administration.Access`.

## Changes apportés

### 1. **Ajouter la permission `Administration.Access`** 
   - **Fichier**: `Utils/SeedRolesPermissions.cs`
   - **Description**: Ajout d'une nouvelle permission dans le tableau `DefaultPermissions` qui contrôle l'accès à la section d'administration.
   - **Code permission**: `Administration.Access`
   - **Description**: "Permet d'accéder à la section d'administration (Rôles, Permissions, Utilisateurs)."
  
### 2. **Créer un convertisseur de permissions en visibilité**
   - **Fichier**: `Utils/PermissionToVisibilityConverter.cs` (Nouveau fichier)
   - **Classe**: `PermissionToVisibilityConverter`
   - **Fonctionnement**: 
     - Convertit une permission en `Visibility` (Visible ou Collapsed)
     - Utilise `SessionManager.HasPermission()` pour vérifier si l'utilisateur a la permission
     - Paramètre: Code de la permission à vérifier (ex: `Administration.Access`)

### 3. **Modifier le MainWindow.xaml**
   - **Ajouter l'import du namespace**: `xmlns:local="clr-namespace:Collectivite.Utils"`
   - **Ajouter le converter dans les ressources**: 
     ```xml
     <local:PermissionToVisibilityConverter x:Key="PermissionToVisibilityConverter"/>
     ```
   - **Envelopper la section Administration dans un StackPanel** avec la visibilité conditionnelle:
     ```xml
     <StackPanel Visibility="{Binding Converter={StaticResource PermissionToVisibilityConverter}, 
                                     ConverterParameter=Administration.Access}">
         <!-- TextBlock "Administration" + Expander avec Rôles/Permissions/Utilisateurs -->
     </StackPanel>
     ```

### 4. **Ajouter des contrôles au code-behind**
   - **Fichier**: `MainWindow.xaml.cs`
   - **Méthodes modifiées**:
     - `RolesButton_Click()`
     - `PermissionsButton_Click()`
     - `UsersButton_Click()`
   - **Ajout**: Vérification de la permission `Administration.Access` avant de naviguer
   - **Comportement**: Affiche un message d'erreur si l'utilisateur n'a pas la permission

## Fonctionnement

### Comportement par défaut:
1. **Le Maire** reçoit automatiquement TOUTES les permissions (y compris `Administration.Access`)
2. **Les autres rôles** (Secrétaire Général, Receveur) ne reçoivent pas `Administration.Access`

### Personnalisation:
Le Maire peut ensuite:
- Accéder à la page **ADMINISTRATION - PERMISSIONS**
- Accorder la permission `Administration.Access` aux autres rôles s'il le souhaite
- La section d'administration sera alors visible dans le sidebar pour ces rôles

## Structure de la section d'administration
```
Administration
├── Sécurité (Expander)
    ├── Rôles
    ├── Permissions
    └── Utilisateurs
```

## Protections mises en place

### 1. **Visibilité UI**
   - La section est masquée du sidebar si l'utilisateur n'a pas `Administration.Access`

### 2. **Protection du code-behind**
   - Même si quelqu'un force la visite d'une page d'administration, une vérification dans les handlers de clic empêche la navigation

### 3. **Flexibilité**
   - Le Maire peut contrôler qui a accès à la section à tout moment via l'interface d'administration
   - Les permissions peuvent être mises à jour sans modification du code

## Fichiers modifiés

- ✅ `Utils/SeedRolesPermissions.cs` - Ajout de la permission
- ✅ `Utils/PermissionToVisibilityConverter.cs` - Nouveau fichier (converter)
- ✅ `MainWindow.xaml` - Ajout du converter et application de la visibilité conditionnelle
- ✅ `MainWindow.xaml.cs` - Ajout de vérifications de permissions aux trois méthodes
