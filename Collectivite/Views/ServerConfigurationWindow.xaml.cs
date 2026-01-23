using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.Views
{
    /// <summary>
    /// Interaction logic for ServerConfigurationWindow.xaml
    /// </summary>
    public partial class ServerConfigurationWindow : Window
    {
        public ServerConfigurationWindow()
        {
            InitializeComponent();
            DataContext = new ServerConfigurationViewModel();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ServerConfigurationViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Déclencher la commande manuellement pour tester
            if (DataContext is ServerConfigurationViewModel viewModel && viewModel.SaveCommand.CanExecute(null))
            {
                viewModel.SaveCommand.Execute(null);
            }
        }

        // Permet de déplacer la fenêtre en cliquant et glissant
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}
