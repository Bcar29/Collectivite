using Collectivite.Models;
using System.Windows;

namespace Collectivite.Views
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow(User user)
        {
            InitializeComponent();
            DataContext = user;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

