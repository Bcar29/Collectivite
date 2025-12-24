using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    public partial class UsersPage : Page
    {
        public UsersPage()
        {
            InitializeComponent();
        }

        private UsersViewModel ViewModel => (UsersViewModel)DataContext;

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (sender is ComboBox combo && combo.DataContext is Models.User user)
            {
                if (!combo.IsDropDownOpen && !combo.IsKeyboardFocusWithin)
                {
                    return;
                }

                if (ViewModel.ChangeRoleCommand.CanExecute(user))
                {
                    ViewModel.ChangeRoleCommand.Execute(user);
                }
            }
        }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                ViewModel.DialogUser.Password = pb.Password;
            }
        }


    }
}

