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

            var authService = SessionManager.AuthService;

            _viewModel = new LoginViewModel(authService);

            // ⚠️ IMPORTANT : Définir le DataContext
            DataContext = _viewModel;

            // Permettre Enter pour valider
            this.KeyDown += LoginWindow_KeyDown;
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