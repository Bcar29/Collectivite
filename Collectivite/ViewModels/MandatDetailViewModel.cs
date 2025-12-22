using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Collectivite.ViewModels
{
    public class MandatDetailViewModel : ViewModelBase
    {
        private bool _isLoading;
        private int _mandatId;
        private Mandat? _mandat;
        private Mouvement? _mouvement;
        private List<Mouvement>? _tousLesMouvements;
        private Commune _commune;

        public MandatDetailViewModel(int mandatId)
        {
            _mandatId = mandatId;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            PrintCommand = new RelayCommand(async _ => await PrintAsync());
            ExportPdfCommand = new RelayCommand(async _ => await ExportPdfAsync());
            RetourCommand = new RelayCommand(_ => RetourListe());

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

        public Mandat? Mandat
        {
            get => _mandat;
            set
            {
                if (SetProperty(ref _mandat, value))
                {
                    OnPropertyChanged(nameof(EstPaye));
                    OnPropertyChanged(nameof(EstNonPaye));
                    OnPropertyChanged(nameof(IsMandatValide));
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

        // Utiliser le Status du mandat
        public bool EstPaye => Mandat?.Status == Mandat.StatutMandat.Payé || Mandat?.Status == Mandat.StatutMandat.Partiel;
        public bool EstNonPaye => Mandat?.Status == Mandat.StatutMandat.Non_Payé;
        public bool IsMandatValide => Mandat?.Etat == Mandat.EtatMandat.Validé;

        public Brush EtatBackground
        {
            get
            {
                if (Mandat == null) return new SolidColorBrush(Colors.Gray);

                return Mandat.Etat switch
                {
                    Mandat.EtatMandat.Validé => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Vert
                    Mandat.EtatMandat.Non_Validé => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        public Brush StatutBackground
        {
            get
            {
                if (Mandat == null) return new SolidColorBrush(Colors.Gray);

                return Mandat.Status switch
                {
                    Mandat.StatutMandat.Payé => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Vert
                    Mandat.StatutMandat.Partiel => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Bleu
                    Mandat.StatutMandat.Non_Payé => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Rouge
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        // Propriétés pour le mode de paiement (visible seulement si un Mouvement existe)
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
        public ICommand RetourCommand { get; }

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
                var mandatService = new MandatService();
                var mandat = await mandatService.GetMandatByIdAsync(_mandatId);

                if (mandat != null)
                {
                    Commune = commune;
                    Mandat = mandat;

                    // Charger TOUS les mouvements associés à ce mandat
                    using (var context = new AppDbContext())
                    {
                        var mouvementService = new MouvementService(context);

                        // Récupérer tous les mouvements
                        var mouvements = await mouvementService.GetMouvementsByMandatIdAsync(_mandatId);
                        TousLesMouvements = mouvements;

                        // Le mouvement principal est le plus récent
                        Mouvement = mouvements.FirstOrDefault();

                        // Debug
                        if (mouvements.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Trouvé {mouvements.Count} mouvement(s) pour le mandat {_mandatId}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Aucun mouvement trouvé pour le mandat {_mandatId}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Mandat introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    RetourListe();
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
        /// Imprime le mandat (génère un PDF temporaire et l'ouvre pour impression)
        /// </summary>
        private async Task PrintAsync()
        {
            if (Mandat == null)
            {
                MessageBox.Show("Aucun mandat à imprimer.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;

                // Générer le PDF (utilise Task.Run pour éviter le deadlock WPF)
                byte[] pdfBytes = await Task.Run(() => MandatPdfExporter.Exporter(Mandat, Commune, Mouvement, TousLesMouvements));

                // Créer un fichier temporaire
                string tempFileName = $"Mandat_{Mandat.NumeroMandat}_{Guid.NewGuid():N}.pdf";
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
        /// Exporte le mandat en PDF et propose la sauvegarde
        /// </summary>
        private async Task ExportPdfAsync()
        {
            if (Mandat == null)
            {
                MessageBox.Show("Aucun mandat à exporter.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;

                // Générer le PDF (utilise Task.Run pour éviter le deadlock WPF)
                byte[] pdfBytes = await Task.Run(() => MandatPdfExporter.Exporter(Mandat, Commune, Mouvement, TousLesMouvements));

                // Nom du fichier par défaut
                string defaultFileName = $"Mandat_{Mandat.NumeroMandat}_{DateTime.Now:yyyyMMdd}.pdf";

                // Boîte de dialogue de sauvegarde
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF (*.pdf)|*.pdf",
                    FileName = defaultFileName,
                    Title = "Enregistrer le mandat en PDF",
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

        private void RetourListe()
        {
            NavigationService.Instance.GoBack();
        }

        #endregion
    }
}