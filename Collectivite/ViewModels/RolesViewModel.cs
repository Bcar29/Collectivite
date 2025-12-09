using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class RolesViewModel : ViewModelBase
    {
        private readonly RoleService _roleService = new();
        private readonly PermissionService _permissionService = new();

        private bool _isLoading;
        private bool _isDialogOpen;
        private bool _isPermissionsDialogOpen; // NOUVEAU : Dialog d'affichage des permissions
        private bool _isEditMode;
        private Role _dialogRole = CreateEmptyRole();
        private Role? _selectedRole;
        private string _permissionFilterText = string.Empty;

        // Stocke toutes les permissions chargées pour pouvoir les filtrer dans le modal
        private readonly ObservableCollection<PermissionSelectionViewModel> _allPermissionSelections = new();

        public RolesViewModel()
        {
            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<Role>(OpenEditDialog, role => role != null);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            DeleteCommand = new RelayCommand<Role>(async role => await DeleteAsync(role));
            CloseDialogCommand = new RelayCommand(_ => CloseDialog());
            
            // NOUVELLES COMMANDES : Affichage des permissions
            ShowPermissionsCommand = new RelayCommand<Role>(ShowPermissions, role => role != null);
            ClosePermissionsDialogCommand = new RelayCommand(_ => ClosePermissionsDialog());

            LoadCommand.Execute(null);
        }

        public ObservableCollection<Role> Roles { get; } = new();
        public ObservableCollection<PermissionSelectionViewModel> PermissionSelections { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        /// <summary>
        /// NOUVEAU : Contrôle l'ouverture du dialog d'affichage des permissions
        /// </summary>
        public bool IsPermissionsDialogOpen
        {
            get => _isPermissionsDialogOpen;
            set => SetProperty(ref _isPermissionsDialogOpen, value);
        }

        /// <summary>
        /// Texte de filtre pour la liste des permissions dans le modal (par nom, code ou description).
        /// </summary>
        public string PermissionFilterText
        {
            get => _permissionFilterText;
            set
            {
                if (SetProperty(ref _permissionFilterText, value))
                {
                    ApplyPermissionFilter();
                }
            }
        }

        public Role? SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public Role DialogRole
        {
            get => _dialogRole;
            set => SetProperty(ref _dialogRole, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier le rôle" : "Nouveau rôle";

        public ICommand LoadCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CloseDialogCommand { get; }
        
        // NOUVELLES COMMANDES
        public ICommand ShowPermissionsCommand { get; }
        public ICommand ClosePermissionsDialogCommand { get; }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            IsLoading = true;

            try
            {
                Roles.Clear();
                var roles = await _roleService.GetAllAsync();

                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des rôles : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static Role CreateEmptyRole() => new Role
        {
            Name = string.Empty,
            Description = string.Empty,
            IsActive = true
        };

        private void OpenAddDialog()
        {
            DialogRole = CreateEmptyRole();

            IsEditMode = false;
            LoadPermissionSelections(Array.Empty<string>());
            IsDialogOpen = true;
        }

        private void OpenEditDialog(Role? role)
        {
            if (role == null) return;

            DialogRole = new Role
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive
            };

            IsEditMode = true;

            var currentCodes = role.RolePermissions?
                .Select(rp => rp.Permission?.Code)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!)
                .ToArray() ?? Array.Empty<string>();

            LoadPermissionSelections(currentCodes);
            IsDialogOpen = true;
        }

        private async void LoadPermissionSelections(string[] checkedCodes)
        {
            PermissionSelections.Clear();
            _allPermissionSelections.Clear();
            PermissionFilterText = string.Empty;

            var permissions = await _permissionService.GetAllAsync();
            foreach (var permission in permissions)
            {
                var vm = new PermissionSelectionViewModel
                {
                    PermissionId = permission.Id,
                    Code = permission.Code,
                    Name = permission.Name,
                    Description = permission.Description,
                    IsSelected = checkedCodes.Contains(permission.Code ?? "", StringComparer.OrdinalIgnoreCase)
                };

                _allPermissionSelections.Add(vm);
            }

            ApplyPermissionFilter();
        }

        /// <summary>
        /// Applique le filtre sur les permissions visibles dans le modal à partir de _allPermissionSelections.
        /// </summary>
        private void ApplyPermissionFilter()
        {
            PermissionSelections.Clear();

            var filter = _permissionFilterText?.Trim();
            var source = _allPermissionSelections.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                source = source.Where(p =>
                    (!string.IsNullOrEmpty(p.Name) && p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(p.Code) && p.Code.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)));
            }

            foreach (var vm in source)
            {
                PermissionSelections.Add(vm);
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(DialogRole.Name);
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                // FIX IMPORTANT : Utiliser _allPermissionSelections au lieu de PermissionSelections
                // car PermissionSelections peut être filtré et ne contenir que les permissions visibles
                var selectedPermissionIds = _allPermissionSelections
                    .Where(p => p.IsSelected)
                    .Select(p => p.PermissionId)
                    .ToList();

                if (IsEditMode)
                {
                    var (success, message) = await _roleService.UpdateAsync(DialogRole);
                    if (!success)
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await _roleService.UpdateRolePermissionsAsync(DialogRole.Id, selectedPermissionIds);
                }
                else
                {
                    var (success, message, created) = await _roleService.CreateAsync(DialogRole);
                    if (!success || created == null)
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await _roleService.UpdateRolePermissionsAsync(created.Id, selectedPermissionIds);
                }

                MessageBox.Show("Rôle sauvegardé avec succès.",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                IsDialogOpen = false;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task DeleteAsync(Role? role)
        {
            if (role == null) return;

            var confirm = MessageBox.Show(
                $"Supprimer le rôle '{role.Name}' ?\n\n" +
                "Attention : Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            IsLoading = true;

            try
            {
                var (success, message) = await _roleService.DeleteAsync(role.Id);
                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CloseDialog()
        {
            IsDialogOpen = false;
        }

        // ========================================
        // NOUVELLES MÉTHODES : Affichage des permissions
        // ========================================

        /// <summary>
        /// Affiche le dialog avec le détail des permissions d'un rôle
        /// </summary>
        private void ShowPermissions(Role? role)
        {
            if (role == null) return;

            // Définir le rôle sélectionné pour l'affichage
            SelectedRole = role;

            // Ouvrir le dialog des permissions
            IsPermissionsDialogOpen = true;
        }

        /// <summary>
        /// Ferme le dialog des permissions
        /// </summary>
        private void ClosePermissionsDialog()
        {
            IsPermissionsDialogOpen = false;
        }
    }

    public class PermissionSelectionViewModel : ViewModelBase
    {
        private bool _isSelected;

        public int PermissionId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}