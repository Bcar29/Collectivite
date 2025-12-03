using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class PermissionsViewModel : ViewModelBase
    {
        private readonly PermissionService _permissionService = new();

        private bool _isLoading;
        private bool _isDialogOpen;
        private bool _isEditMode;
        private static Permission CreateEmptyPermission() => new Permission
        {
            Name = string.Empty,
            Code = string.Empty,
            Description = string.Empty
        };

        private Permission _dialogPermission = CreateEmptyPermission();
        private Permission? _selectedPermission;

        public PermissionsViewModel()
        {
            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<Permission>(OpenEditDialog, p => p != null);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            DeleteCommand = new RelayCommand<Permission>(async p => await DeleteAsync(p));
            CloseDialogCommand = new RelayCommand(_ => CloseDialog());

            LoadCommand.Execute(null);
        }

        public ObservableCollection<Permission> Permissions { get; } = new();

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

        public Permission DialogPermission
        {
            get => _dialogPermission;
            set => SetProperty(ref _dialogPermission, value);
        }

        public Permission? SelectedPermission
        {
            get => _selectedPermission;
            set => SetProperty(ref _selectedPermission, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier la permission" : "Nouvelle permission";

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
                Permissions.Clear();
                var permissions = await _permissionService.GetAllAsync();
                foreach (var permission in permissions)
                {
                    Permissions.Add(permission);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenAddDialog()
        {
            DialogPermission = CreateEmptyPermission();
            IsEditMode = false;
            IsDialogOpen = true;
        }

        private void OpenEditDialog(Permission? permission)
        {
            if (permission == null) return;

            DialogPermission = new Permission
            {
                Id = permission.Id,
                Name = permission.Name,
                Code = permission.Code,
                Description = permission.Description
            };

            IsEditMode = true;
            IsDialogOpen = true;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(DialogPermission.Name)
                && !string.IsNullOrWhiteSpace(DialogPermission.Code);
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _permissionService.UpdateAsync(DialogPermission);
                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (!success) return;
                }
                else
                {
                    var (success, message, _) = await _permissionService.CreateAsync(DialogPermission);
                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (!success) return;
                }

                IsDialogOpen = false;
                await LoadAsync();
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

        private async System.Threading.Tasks.Task DeleteAsync(Permission? permission)
        {
            if (permission == null) return;

            var confirm = MessageBox.Show(
                $"Supprimer la permission '{permission.Name}' ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            IsLoading = true;

            try
            {
                var (success, message) = await _permissionService.DeleteAsync(permission.Id);

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
}

