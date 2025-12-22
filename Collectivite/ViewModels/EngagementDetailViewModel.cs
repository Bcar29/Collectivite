using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
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
        private Commune _commune;

        public EngagementDetailViewModel(int engagementId)
        {
            _engagementId = engagementId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            BackCommand = new RelayCommand(_ => GoBack());
            PrintCommand = new RelayCommand(async _ => await PrintAsync());
            ExportPdfCommand = new RelayCommand(async _ => await ExportPdfAsync());
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

        public Commune Commune
        {
            get => _commune;
            set => SetProperty(ref _commune, value);
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

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les infos de la commune avec relations
                var communeService = new CommuneService();
                var commune = await communeService.GetCommuneByIdWithRelationsAsync(
                    Properties.Settings.Default.CommuneId
                );

                var service = new EngagementService();
                var engagement = await service.GetEngagementByIdAsync(_engagementId);

                if (engagement != null)
                {
                    Commune = commune;
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

        /// <summary>
        /// Imprime l'engagement (génère un PDF temporaire et l'ouvre pour impression)
        /// </summary>
        private async Task PrintAsync()
        {
            if (Engagement == null)
            {
                MessageBox.Show("Aucun engagement à imprimer.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;

                // Générer le PDF (utilise Task.Run pour éviter le deadlock WPF)
                byte[] pdfBytes = await Task.Run(() => EngagementPdfExporter.Exporter(Engagement, Commune));

                // Créer un fichier temporaire
                string tempFileName = $"Engagement_{Engagement.Exercice?.Libelle}_{Guid.NewGuid():N}.pdf";
                string tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                // Écrire le fichier
                await File.WriteAllBytesAsync(tempPath, pdfBytes);

                // Ouvrir le PDF avec l'application par défaut
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });

                MessageBox.Show(
                    "Le document s'ouvre dans votre lecteur PDF.\n\n" +
                    "Utilisez Ctrl+P ou le menu Fichier → Imprimer pour lancer l'impression.",
                    "Impression",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Exporte l'engagement en PDF et propose la sauvegarde
        /// </summary>
        private async Task ExportPdfAsync()
        {
            if (Engagement == null)
            {
                MessageBox.Show("Aucun engagement à exporter.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;

                // Générer le PDF (utilise Task.Run pour éviter le deadlock WPF)
                byte[] pdfBytes = await Task.Run(() => EngagementPdfExporter.Exporter(Engagement, Commune));

                // Nom du fichier par défaut
                string defaultFileName = $"Engagement_{Engagement.Exercice?.Libelle}_{DateTime.Now:yyyyMMdd}.pdf";

                // Boîte de dialogue de sauvegarde
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF (*.pdf)|*.pdf",
                    FileName = defaultFileName,
                    Title = "Enregistrer l'engagement en PDF",
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Sauvegarder le fichier
                    await File.WriteAllBytesAsync(saveDialog.FileName, pdfBytes);

                    // Demander si l'utilisateur veut ouvrir le fichier
                    var result = MessageBox.Show(
                        "Le fichier PDF a été créé avec succès !\n\n" +
                        $"Emplacement : {saveDialog.FileName}\n\n" +
                        "Voulez-vous l'ouvrir maintenant ?",
                        "Export réussi",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Ouvrir le PDF avec l'application par défaut
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Télécharge le fichier joint à l'engagement
        /// </summary>
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
                // Déterminer l'extension du fichier
                string fileName = Engagement.FichierName ?? "fichier_engagement";
                string extension = Path.GetExtension(fileName);

                if (string.IsNullOrEmpty(extension))
                {
                    extension = ".*";
                }

                var saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = $"Fichier (*{extension})|*{extension}|Tous les fichiers (*.*)|*.*",
                    Title = "Enregistrer le fichier joint"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, Engagement.FichierJoin);

                    var result = MessageBox.Show(
                        "Fichier téléchargé avec succès !\n\nVoulez-vous l'ouvrir ?",
                        "Succès",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
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