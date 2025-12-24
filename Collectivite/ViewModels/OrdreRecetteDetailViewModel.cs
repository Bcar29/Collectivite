using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Xps;

namespace Collectivite.ViewModels
{
    public class OrdreRecetteDetailViewModel : ViewModelBase
    {
        private bool _isLoading;
        private int _ordreRecetteId;
        private OrdreRecette? _ordreRecette;
        private Mouvement? _mouvement;
        private List<Mouvement>? _tousLesMouvements;
        private Commune _commune;

        public OrdreRecetteDetailViewModel(int ordreRecetteId)
        {
            _ordreRecetteId = ordreRecetteId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            PrintCommand = new RelayCommand(async _ => await PrintAsync());
            ExportPdfCommand = new RelayCommand(async _ => await ExportPdfAsync());
            NavigateBackCommand = new RelayCommand(_ => NavigateBack());

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

        public OrdreRecette? OrdreRecette
        {
            get => _ordreRecette;
            set
            {
                if (SetProperty(ref _ordreRecette, value))
                {
                    OnPropertyChanged(nameof(EstEncaisse));
                    OnPropertyChanged(nameof(EstNonEncaisse));
                    OnPropertyChanged(nameof(IsOrdreValide));
                    OnPropertyChanged(nameof(EtatBackground));
                    OnPropertyChanged(nameof(StatutBackground));
                }
            }
        }

        public Mouvement? Mouvement
        {
            get => _mouvement;
            set
            {
                if (SetProperty(ref _mouvement, value))
                {
                    OnPropertyChanged(nameof(ModePaiement));
                    OnPropertyChanged(nameof(ModePaiementIcon));
                    OnPropertyChanged(nameof(EstVirement));
                    OnPropertyChanged(nameof(EstCheque));
                    OnPropertyChanged(nameof(EstEspece));
                    OnPropertyChanged(nameof(HasMouvement));
                }
            }
        }

        public List<Mouvement>? TousLesMouvements
        {
            get => _tousLesMouvements;
            set => SetProperty(ref _tousLesMouvements, value);
        }

        // Propriété pour vérifier si des mouvements existent
        public bool HasMouvement => Mouvement != null || (TousLesMouvements != null && TousLesMouvements.Any());

        // Utiliser le Statut de l'ordre
        public bool EstEncaisse => OrdreRecette?.Status == OrdreRecette.StatutOrdre.Enciassé || OrdreRecette?.Status == OrdreRecette.StatutOrdre.Partiel;
        public bool EstNonEncaisse => OrdreRecette?.Status == OrdreRecette.StatutOrdre.Non_Encaissé;
        public bool IsOrdreValide => OrdreRecette?.Etat == OrdreRecette.EtatOdre.Validé;

        public Brush EtatBackground
        {
            get
            {
                if (OrdreRecette == null) return new SolidColorBrush(Colors.Gray);

                return OrdreRecette.Etat switch
                {
                    OrdreRecette.EtatOdre.Validé => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Vert
                    OrdreRecette.EtatOdre.Non_Validé => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        public Brush StatutBackground
        {
            get
            {
                if (OrdreRecette == null) return new SolidColorBrush(Colors.Gray);

                return OrdreRecette.Status switch
                {
                    OrdreRecette.StatutOrdre.Enciassé => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Vert
                    OrdreRecette.StatutOrdre.Partiel => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Bleu
                    OrdreRecette.StatutOrdre.Non_Encaissé => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Rouge
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        // Propriétés pour le mode d'encaissement (visible seulement si un Mouvement existe)
        public string ModePaiement
        {
            get
            {
                if (Mouvement == null) return "";

                if (!string.IsNullOrEmpty(Mouvement.RefVirement))
                    return "Virement bancaire";
                else if (!string.IsNullOrEmpty(Mouvement.RefChèque))
                    return "Chèque";
                else
                    return "Espèces";
            }
        }

        public string ModePaiementIcon
        {
            get
            {
                if (Mouvement == null) return "Cash";

                if (!string.IsNullOrEmpty(Mouvement.RefVirement))
                    return "BankTransfer";
                else if (!string.IsNullOrEmpty(Mouvement.RefChèque))
                    return "CheckDecagram";
                else
                    return "Cash";
            }
        }

        public bool EstVirement => !string.IsNullOrEmpty(Mouvement?.RefVirement);
        public bool EstCheque => !string.IsNullOrEmpty(Mouvement?.RefChèque);
        public bool EstEspece => Mouvement != null && string.IsNullOrEmpty(Mouvement.RefVirement) && string.IsNullOrEmpty(Mouvement.RefChèque);

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand NavigateBackCommand { get; }

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

                var ordreRecetteService = new OrdreRecetteService();
                var ordre = await ordreRecetteService.GetOrdreRecetteByIdAsync(_ordreRecetteId);

                if (ordre != null)
                {
                    Commune = commune;
                    OrdreRecette = ordre;

                    // Charger TOUS les mouvements associés à cet ordre
                    using (var context = new AppDbContext())
                    {
                        var mouvementService = new MouvementService(context);

                        // Récupérer tous les mouvements
                        var mouvements = await mouvementService.GetMouvementsByOrdreRecetteIdAsync(_ordreRecetteId);
                        TousLesMouvements = mouvements;

                        // Le mouvement principal est le plus récent
                        Mouvement = mouvements.FirstOrDefault();

                        // Debug
                        if (mouvements.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Trouvé {mouvements.Count} mouvement(s) pour l'ordre {_ordreRecetteId}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Aucun mouvement trouvé pour l'ordre {_ordreRecetteId}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Ordre de recette introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigateBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Exporte l'ordre de recette en PDF
        /// </summary>
        private async System.Threading.Tasks.Task ExportPdfAsync()
        {
            if (OrdreRecette == null)
            {
                MessageBox.Show("Aucun ordre de recette à exporter.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                // Générer le PDF avec TOUS les mouvements
                var pdfBytes = await OrdreRecettePdfExporter.ExporterAsync(OrdreRecette, Commune, Mouvement, TousLesMouvements);

                // Ouvrir la boîte de dialogue de sauvegarde
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichier PDF (*.pdf)|*.pdf",
                    FileName = $"OrdreRecette_{OrdreRecette.NumeroOrdre}_{DateTime.Now:yyyyMMdd}",
                    Title = "Enregistrer l'ordre de recette en PDF"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(saveDialog.FileName, pdfBytes);

                    var result = MessageBox.Show(
                        "Le fichier PDF a été créé avec succès.\n\nVoulez-vous l'ouvrir maintenant ?",
                        "Export réussi",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
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
        /// Imprime l'ordre de recette
        /// </summary>
        private async System.Threading.Tasks.Task PrintAsync()
        {
            if (OrdreRecette == null)
            {
                MessageBox.Show("Aucun ordre de recette à imprimer.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                // Générer le PDF avec TOUS les mouvements
                var pdfBytes = await OrdreRecettePdfExporter.ExporterAsync(OrdreRecette, Commune, Mouvement, TousLesMouvements);

                string tempFile = Path.Combine(Path.GetTempPath(), $"OrdreRecette_{OrdreRecette.NumeroOrdre}_{Guid.NewGuid()}.pdf");
                await File.WriteAllBytesAsync(tempFile, pdfBytes);

                var result = MessageBox.Show(
                    "Le document va s'ouvrir dans votre lecteur PDF.\n\nUtilisez Ctrl+P ou le menu Fichier > Imprimer pour lancer l'impression.",
                    "Impression",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.OK)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempFile,
                        UseShellExecute = true
                    });
                }
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
        private void NavigateBack()
        {
            NavigationService.Instance.GoBack();
        }

        #endregion
    }
}