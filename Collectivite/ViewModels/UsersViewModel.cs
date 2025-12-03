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
    public class UsersViewModel : ViewModelBase
    {
        private readonly UserService _userService = new();
        private readonly RoleService _roleService = new();
        private readonly CommuneService _communeService = new();

        private bool _isLoading;
        private bool _isDialogOpen;
        private bool _isEditMode;
        private static User CreateEmptyUser() => new User
        {
            Username = string.Empty,
            Email = string.Empty,
            Tel = string.Empty,
            Password = string.Empty,
            CommuneId = 0,
            RoleId = 0
        };

        private User _dialogUser = CreateEmptyUser();
        private User? _selectedUser;

        public UsersViewModel()
        {
            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<User>(OpenEditDialog, u => u != null);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            DeleteCommand = new RelayCommand<User>(async u => await DeleteAsync(u));
            CloseDialogCommand = new RelayCommand(_ => CloseDialog());
            ChangeRoleCommand = new RelayCommand<User>(async u => await ChangeRoleAsync(u));

            LoadCommand.Execute(null);
        }

        public ObservableCollection<User> Users { get; } = new();
        public ObservableCollection<Role> Roles { get; } = new();
        public ObservableCollection<Commune> Communes { get; } = new();

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

        public User DialogUser
        {
            get => _dialogUser;
            set => SetProperty(ref _dialogUser, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CloseDialogCommand { get; }
        public ICommand ChangeRoleCommand { get; }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            IsLoading = true;

            try
            {
                Users.Clear();
                Roles.Clear();
                Communes.Clear();

                var users = await _userService.GetAllAsync();
                foreach (var user in users)
                    Users.Add(user);

                var roles = await _roleService.GetAllAsync();
                foreach (var role in roles)
                    Roles.Add(role);

                var communes = await _communeService.GetAllCommuneAsync();
                foreach (var commune in communes)
                    Communes.Add(commune);
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
            DialogUser = CreateEmptyUser();
            DialogUser.CommuneId = Communes.FirstOrDefault()?.Id ?? 0;
            DialogUser.RoleId = Roles.FirstOrDefault()?.Id ?? 0;

            IsEditMode = false;
            IsDialogOpen = true;
        }

        private void OpenEditDialog(User? user)
        {
            if (user == null) return;

            DialogUser = new User
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Tel = user.Tel,
                Password = user.Password,
                CommuneId = user.CommuneId,
                RoleId = user.RoleId
            };

            IsEditMode = true;
            IsDialogOpen = true;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(DialogUser.Username)
                && !string.IsNullOrWhiteSpace(DialogUser.Password)
                && DialogUser.CommuneId > 0
                && DialogUser.RoleId > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _userService.UpdateAsync(DialogUser);
                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (!success) return;
                }
                else
                {
                    var (success, message, _) = await _userService.CreateAsync(DialogUser);
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

        private async System.Threading.Tasks.Task DeleteAsync(User? user)
        {
            if (user == null) return;

            var confirm = MessageBox.Show(
                $"Supprimer l'utilisateur '{user.Username}' ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            IsLoading = true;

            try
            {
                var (success, message) = await _userService.DeleteAsync(user.Id);
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

        private async System.Threading.Tasks.Task ChangeRoleAsync(User? user)
        {
            if (user == null) return;

            try
            {
                var (success, message) = await _userService.UpdateRoleAsync(user.Id, user.RoleId);
                if (!success)
                {
                    MessageBox.Show(message,
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour du rôle : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog()
        {
            IsDialogOpen = false;
        }
    }
}

