using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Collectivite.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();

            // CRÉER LE CONTEXTE DE BASE DE DONNÉES
            var context = new Services.AppDbContext();

            // CRÉER LE SERVICE D'AUTHENTIFICATION
            var authService = new AuthService(context);

            // CRÉER LE VIEWMODEL
            _viewModel = new LoginViewModel(authService);

            // ⚠️ IMPORTANT : Définir le DataContext
            DataContext = _viewModel;

            // Permettre Enter pour valider
            this.KeyDown += LoginWindow_KeyDown;
        }

        // Synchroniser le PasswordBox avec le ViewModel
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = passwordBox.Password;
            }
        }

        // Validation avec Enter
        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_viewModel.LoginCommand.CanExecute(null))
                {
                    _viewModel.LoginCommand.Execute(null);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.Source == this || e.Source is Border)
            {
                try { DragMove(); } catch { }
            }
        }
    }
}