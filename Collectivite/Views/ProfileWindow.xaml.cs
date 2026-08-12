using Collectivite.Models;
using Collectivite.ViewModels;
using System.Windows;

namespace Collectivite.Views
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow(User user)
        {
            InitializeComponent();

            var viewModel = new ProfileViewModel(user)
            {
                RequestClose = saved =>
                {
                    DialogResult = saved;
                    Close();
                }
            };
            DataContext = viewModel;
        }
    }
}
