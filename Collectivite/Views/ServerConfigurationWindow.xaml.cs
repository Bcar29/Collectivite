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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (HasUnsavedInput() &&
                MessageBox.Show(
                    "Des informations ont été saisies et seront perdues. Voulez-vous vraiment fermer ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            DialogResult = false;
            Close();
        }

        private bool HasUnsavedInput()
        {
            return DataContext is ServerConfigurationViewModel viewModel &&
                   (!string.IsNullOrWhiteSpace(viewModel.Server) ||
                    !string.IsNullOrWhiteSpace(viewModel.Database) ||
                    !string.IsNullOrWhiteSpace(viewModel.User) ||
                    !string.IsNullOrWhiteSpace(viewModel.Password));
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
