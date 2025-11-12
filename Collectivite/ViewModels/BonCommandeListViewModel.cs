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
    public class BonCommandeListViewModel : ViewModelBase
    {
        private bool _isLoading;
        private BonCommande? _selectedBonCommande;

        public BonCommandeListViewModel()
        {
            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenAddPageCommand = new RelayCommand(_ => OpenAddPage());
            OpenEditPageCommand = new RelayCommand<BonCommande>(bc => OpenEditPage(bc));
            OpenDetailsPageCommand = new RelayCommand<BonCommande>(bc => OpenDetailsPage(bc));
            DeleteCommand = new RelayCommand<BonCommande>(async bc => await DeleteAsync(bc));

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<BonCommande> BonCommandes { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public BonCommande? SelectedBonCommande
        {
            get => _selectedBonCommande;
            set => SetProperty(ref _selectedBonCommande, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddPageCommand { get; }
        public ICommand OpenEditPageCommand { get; }
        public ICommand OpenDetailsPageCommand { get; }
        public ICommand DeleteCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var service = new BonCommandeService();
                var bonCommandes = await service.GetAllBonCommandesAsync();

                BonCommandes.Clear();
                foreach (var bc in bonCommandes)
                {
                    BonCommandes.Add(bc);
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

        private void OpenAddPage()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    frame.Navigate(new Views.Pages.BonCommandeFormPage());
                }
            }
        }

        private void OpenEditPage(BonCommande? bonCommande)
        {
            if (bonCommande == null) return;

            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    frame.Navigate(new Views.Pages.BonCommandeFormPage(bonCommande.Id));
                }
            }
        }

        private void OpenDetailsPage(BonCommande? bonCommande)
        {
            if (bonCommande == null) return;

            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    frame.Navigate(new Views.Pages.BonCommandeDetailsPage(bonCommande.Id));
                }
            }
        }

        private async System.Threading.Tasks.Task DeleteAsync(BonCommande? bonCommande)
        {
            if (bonCommande == null) return;

            var result = MessageBox.Show(
                $"⚠️ Supprimer le bon de commande '{bonCommande.Numero}' ?\n\n" +
                $"Cette action supprimera également tous les détails associés.\n" +
                $"Cette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var service = new BonCommandeService();
                var (success, message) = await service.DeleteBonCommandeAsync(bonCommande.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}