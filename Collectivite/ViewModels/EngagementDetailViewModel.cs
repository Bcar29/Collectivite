using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Collectivite.ViewModels
{
    public class EngagementDetailViewModel : ViewModelBase
    {
        private bool _isLoading;
        private Engagement? _engagement;
        private int _engagementId;

        public EngagementDetailViewModel(int engagementId)
        {
            _engagementId = engagementId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            BackCommand = new RelayCommand(_ => GoBack());
            PrintCommand = new RelayCommand(_ => Print());
            ExportPdfCommand = new RelayCommand(_ => ExportPdf());
            DownloadFileCommand = new RelayCommand(_ => DownloadFile());

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
            set
            {
                if (SetProperty(ref _engagement, value))
                {
                    OnPropertyChanged(nameof(HasFile));
                    OnPropertyChanged(nameof(IsEngagementValide));
                    OnPropertyChanged(nameof(EtatBackground));
                }
            }
        }

        public bool HasFile => Engagement?.FichierJoin != null && Engagement.FichierJoin.Length > 0;

        public bool IsEngagementValide => Engagement?.Etat == Engagement.EtatEngagement.Validé;

        public Brush EtatBackground
        {
            get
            {
                if (Engagement == null) return new SolidColorBrush(Colors.Gray);

                return Engagement.Etat switch
                {
                    Engagement.EtatEngagement.Validé => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Vert
                    Engagement.EtatEngagement.Non_Validé => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand DownloadFileCommand { get; }

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
                }
                else
                {
                    MessageBox.Show("Engagement introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    GoBack();
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

        private void GoBack()
        {
            NavigationService.Instance.GoBack();
        }

        private void Print()
        {
            // TODO: Implémenter l'impression
            MessageBox.Show("Fonctionnalité d'impression en cours de développement.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportPdf()
        {
            // TODO: Implémenter l'export PDF
            MessageBox.Show("Fonctionnalité d'export PDF en cours de développement.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DownloadFile()
        {
            if (Engagement?.FichierJoin == null || Engagement.FichierJoin.Length == 0)
            {
                MessageBox.Show("Aucun fichier disponible.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = Engagement.FichierName ?? "fichier_engagement",
                    Filter = "Tous les fichiers (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, Engagement.FichierJoin);
                    MessageBox.Show("Fichier téléchargé avec succès.", "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du téléchargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}