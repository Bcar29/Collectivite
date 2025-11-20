using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour la page de détails d'un engagement
    /// </summary>
    public class EngagementDetailViewModel : ViewModelBase
    {
        private bool _isLoading;
        private Engagement? _engagement;
        private readonly int _engagementId;

        public EngagementDetailViewModel(int engagementId)
        {
            _engagementId = engagementId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            EditCommand = new RelayCommand(_ => Edit());
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync());
            DownloadFileCommand = new RelayCommand(_ => DownloadFile());
            BackCommand = new RelayCommand(_ => NavigateBack());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Engagement? Engagement
        {
            get => _engagement;
            set => SetProperty(ref _engagement, value);
        }

        public bool HasFile => Engagement?.FichierJoin != null;

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand DownloadFileCommand { get; }
        public ICommand BackCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var service = new EngagementService();
                var engagement = await service.GetEngagementByIdAsync(_engagementId);

                if (engagement != null)
                {
                    Engagement = engagement;
                    OnPropertyChanged(nameof(HasFile));
                }
                else
                {
                    MessageBox.Show("Engagement introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigateBack();
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

        private void Edit()
        {
            if (Engagement == null) return;

            // Navigation vers la page de modification
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    var formPage = new Views.Pages.EngagementFormPage(Engagement.Id);
                    frame.Navigate(formPage);
                }
            }
        }

        private async System.Threading.Tasks.Task DeleteAsync()
        {
            if (Engagement == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cet engagement ?\n\n" +
                $"Objet : {Engagement.Objet}\n" +
                $"Montant : {Engagement.MontantEngagement:N0} GNF\n\n" +
                "Cette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var service = new EngagementService();
                var (success, message) = await service.DeleteEngagementAsync(Engagement.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    NavigateBack();
                }

                IsLoading = false;
            }
        }

        private void DownloadFile()
        {
            if (Engagement?.FichierJoin == null)
            {
                MessageBox.Show("Aucun fichier joint à cet engagement.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string fileName = Engagement.FichierName ?? $"Engagement_{Engagement.Id}_Fichier.bin";
                string extension = Path.GetExtension(fileName);

                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Enregistrer le fichier",
                    FileName = fileName,
                    Filter = $"Fichier (*{extension})|*{extension}|Tous les fichiers (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, Engagement.FichierJoin);
                    MessageBox.Show("Fichier téléchargé avec succès.",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du téléchargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateBack()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    if (frame.CanGoBack)
                    {
                        frame.GoBack();
                    }
                    else
                    {
                        frame.Navigate(new Views.Pages.EngagementPage());
                    }
                }
            }
        }

        #endregion
    }
}