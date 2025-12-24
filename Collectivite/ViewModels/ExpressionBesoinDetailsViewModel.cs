using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class ExpressionBesoinDetailsViewModel : ViewModelBase
    {
        private bool _isLoading;
        private ExpressionBesoin? _expressionBesoin;
        private int _expressionBesoinId;
        private Commune _commune;

        public ExpressionBesoinDetailsViewModel(int expressionBesoinId)
        {
            _expressionBesoinId = expressionBesoinId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            GoBackCommand = new RelayCommand(_ => GoBack());
            PrintCommand = new RelayCommand(async _ => await PrintAsync());
            ExportPdfCommand = new RelayCommand(async _ => await ExportPdfAsync());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public Commune Commune
        {
            get => _commune;
            set => SetProperty(ref _commune, value);
        }

        public ObservableCollection<DetailExpressionBesoin> Details { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ExpressionBesoin? ExpressionBesoin
        {
            get => _expressionBesoin;
            set => SetProperty(ref _expressionBesoin, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportPdfCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                // Charger les infos de la commune avec relations
                var communeService = new CommuneService();
                var commune = await communeService.GetCommuneByIdWithRelationsAsync(
                    Properties.Settings.Default.CommuneId
                );

                var service = new ExpressionBesoinService();
                var expressionBesoin = await service.GetExpressionBesoinByIdAsync(_expressionBesoinId);

                if (expressionBesoin != null)
                {
                    Commune = commune;
                    ExpressionBesoin = expressionBesoin;

                    Details.Clear();
                    if (expressionBesoin.Details != null)
                    {
                        foreach (var detail in expressionBesoin.Details)
                        {
                            Details.Add(detail);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Expression de besoin introuvable.", "Erreur",
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

        /// <summary>
        /// Imprime l'expression de besoin (génère un PDF temporaire et l'ouvre pour impression)
        /// </summary>
        private async System.Threading.Tasks.Task PrintAsync()
        {
            if (ExpressionBesoin == null)
            {
                MessageBox.Show("Aucune expression de besoin à imprimer.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;

                // Générer le PDF
                var exporter = new ExpressionBesoinPdfExporter();
                var pdfBytes = await exporter.ExporterAsync(
                    ExpressionBesoin,
                    Commune,
                    Details.ToList()
                );

                // Créer un fichier temporaire
                string tempFileName = $"ExpressionBesoin_{ExpressionBesoin.Numero}_{Guid.NewGuid():N}.pdf";
                string tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                // Sauvegarder le PDF temporaire
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
        /// Exporte l'expression de besoin en PDF
        /// </summary>
        private async System.Threading.Tasks.Task ExportPdfAsync()
        {
            if (ExpressionBesoin == null)
            {
                MessageBox.Show("Aucune expression de besoin à exporter.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Boîte de dialogue pour sauvegarder
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF|*.pdf",
                    FileName = $"ExpressionBesoin_{ExpressionBesoin.Numero}_{DateTime.Now:yyyyMMdd}.pdf",
                    Title = "Exporter l'Expression de Besoin en PDF"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                IsLoading = true;

                // Générer le PDF
                var exporter = new ExpressionBesoinPdfExporter();
                var pdfBytes = await exporter.ExporterAsync(
                    ExpressionBesoin,
                    Commune,
                    Details.ToList()
                );

                // Sauvegarder le fichier
                await File.WriteAllBytesAsync(saveFileDialog.FileName, pdfBytes);

                // Demander si l'utilisateur veut ouvrir le fichier
                var result = MessageBox.Show(
                    "Export PDF réalisé avec succès !\n\n" +
                    "Voulez-vous ouvrir le fichier ?",
                    "Succès",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
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

        private void GoBack()
        {
            NavigationService.Instance.GoBack();
        }

        #endregion
    }
}