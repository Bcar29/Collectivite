using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly UserService _userService = new();
        private readonly User _user;

        private string? _email;
        private string? _tel;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;

        public ProfileViewModel(User user)
        {
            _user = user;
            _email = user.Email;
            _tel = user.Tel;

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

        public string Username => _user.Username;
        public string? CommuneNom => _user.Commune?.Nom;

        public string? Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string? Tel
        {
            get => _tel;
            set => SetProperty(ref _tel, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Signale à la fenêtre qu'elle doit se fermer (true = enregistrement réussi).
        /// </summary>
        public Action<bool>? RequestClose { get; set; }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            if (!string.IsNullOrEmpty(NewPassword) && NewPassword != ConfirmPassword)
            {
                NotificationService.ShowWarning("Les deux mots de passe ne correspondent pas.");
                return;
            }

            try
            {
                var updated = new User
                {
                    Id = _user.Id,
                    Username = _user.Username,
                    Email = Email,
                    Tel = Tel,
                    CommuneId = _user.CommuneId,
                    RoleId = _user.RoleId,
                    Password = NewPassword
                };

                var (success, message) = await _userService.UpdateAsync(updated);

                if (success)
                    NotificationService.ShowSuccess(message);
                else
                    NotificationService.ShowWarning(message);

                if (!success) return;

                // Refléter les changements dans la session en cours, sans nécessiter une reconnexion.
                _user.Email = Email;
                _user.Tel = Tel;

                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
            }
        }
    }
}
