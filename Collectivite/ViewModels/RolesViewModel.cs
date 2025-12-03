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
        private bool _isEditMode;
        private Role _dialogRole = CreateEmptyRole();
        private Role? _selectedRole;

        public RolesViewModel()
        {
            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<Role>(OpenEditDialog, role => role != null);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            DeleteCommand = new RelayCommand<Role>(async role => await DeleteAsync(role));
            CloseDialogCommand = new RelayCommand(_ => CloseDialog());

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

            var permissions = await _permissionService.GetAllAsync();
            foreach (var permission in permissions)
            {
                PermissionSelections.Add(new PermissionSelectionViewModel
                {
                    PermissionId = permission.Id,
                    Code = permission.Code,
                    Name = permission.Name,
                    Description = permission.Description,
                    IsSelected = checkedCodes.Contains(permission.Code ?? "", StringComparer.OrdinalIgnoreCase)
                });
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
                var selectedPermissionIds = PermissionSelections
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
                $"Supprimer le rôle '{role.Name}' ?",
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

