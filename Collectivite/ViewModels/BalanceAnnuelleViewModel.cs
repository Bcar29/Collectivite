
using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    public partial class BalanceAnnuelleViewModel : ObservableObject
    {
        private readonly IBalanceAnnuelleService _balanceAnnuelleService;

        #region Propriétés observables

        [ObservableProperty]
        private ObservableCollection<BalanceAnnuelleLigneDTO> _lignes = new();

        [ObservableProperty]
        private BalanceAnnuelleTotauxDTO? _totaux;

        [ObservableProperty]
        private BalanceAnnuelleStatsDTO? _statistiques;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _messageErreur = string.Empty;

        // Filtres
        [ObservableProperty]
        private List<int> _anneesDisponibles = new();

        [ObservableProperty]
        private int _anneeSelectionnee;

        [ObservableProperty]
        private List<ClasseCompteItem> _classesComptes = new();

        [ObservableProperty]
        private ClasseCompteItem? _classeSelectionnee;

        [ObservableProperty]
        private string _rechercheTexte = string.Empty;

        [ObservableProperty]
        private bool _afficherComptesVides = false;

        // Titre dynamique
        [ObservableProperty]
        private string _titrePeriode = "Balance des Comptes";

        [ObservableProperty]
        private string _sousTitre = "Balance Annuelle";

        #endregion

        public BalanceAnnuelleViewModel(IBalanceAnnuelleService balanceAnnuelleService)
        {
            _balanceAnnuelleService = balanceAnnuelleService;
            // S'abonner au changement d'exercice
            ExerciceService.Instance.ExerciceChanged += OnExerciceChanged;
        }

        /// <summary>
        /// Initialisation du ViewModel
        /// </summary>
        [RelayCommand]
        public async Task InitialiserAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                // Charger les années disponibles
                AnneesDisponibles = await _balanceAnnuelleService.GetAnneesDisponiblesAsync();
                AnneeSelectionnee = DateTime.Now.Year;

                // Charger les classes de comptes
                var classes = await _balanceAnnuelleService.GetClassesComptesAsync();
                ClassesComptes = new List<ClasseCompteItem>
                {
                    new ClasseCompteItem { Classe = null, Libelle = "Toutes les classes" }
                };
                ClassesComptes.AddRange(classes.Select(c => new ClasseCompteItem
                {
                    Classe = c,
                    Libelle = $"Classe {c}"
                }));

                // Charger la balance
                await ChargerBalanceAnnuelleAsync();
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur d'initialisation : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge la balance annuelle avec les filtres actuels
        /// </summary>
        [RelayCommand]
        public async Task ChargerBalanceAnnuelleAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                var filtre = ConstruireFiltre();

                // Charger les données
                var lignes = await _balanceAnnuelleService.GetBalanceAnnuelleAsync(filtre);
                Lignes = new ObservableCollection<BalanceAnnuelleLigneDTO>(lignes);

                // Calculer les totaux et statistiques à partir des lignes déjà chargées (pas de nouvelle requête)
                Totaux = _balanceAnnuelleService.CalculerTotaux(lignes);
                Statistiques = _balanceAnnuelleService.CalculerStatistiques(lignes);

                // Mettre à jour le titre
                MettreAJourTitre();
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur de chargement : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Réinitialise tous les filtres
        /// </summary>
        [RelayCommand]
        public async Task ReinitialiserFiltresAsync()
        {
            AnneeSelectionnee = DateTime.Now.Year;
            ClasseSelectionnee = ClassesComptes.FirstOrDefault();
            RechercheTexte = string.Empty;
            AfficherComptesVides = false;

            await ChargerBalanceAnnuelleAsync();
        }

        /// <summary>
        /// Exporte la balance annuelle en Excel
        /// </summary>
        [RelayCommand]
        public async Task ExporterExcelAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = await _balanceAnnuelleService.ExportExcelAsync(filtre);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Balance_Annuelle_{AnneeSelectionnee}",
                    DefaultExt = ".xlsx",
                    Filter = "Fichiers Excel|*.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, bytes);
                    NotificationService.ShowSuccess("Export Excel réussi !");
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur d'export : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Exporte la balance annuelle en PDF
        /// </summary>
        [RelayCommand]
        public async Task ExporterPdfAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = await _balanceAnnuelleService.ExportPdfAsync(filtre);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Balance_Annuelle_{AnneeSelectionnee}",
                    DefaultExt = ".pdf",
                    Filter = "Fichiers PDF|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, bytes);
                    NotificationService.ShowSuccess("Export PDF réussi !");
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur d'export : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Imprime la balance annuelle
        /// </summary>
        [RelayCommand]
        public async Task ImprimerAsync()
        {
            try
            {
                IsLoading = true;

                // Générer le PDF
                var filtre = ConstruireFiltre();
                var bytes = await _balanceAnnuelleService.ExportPdfAsync(filtre);

                // Créer un fichier temporaire
                string tempFileName = $"Balance_Annuelle_{AnneeSelectionnee}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                // Sauvegarder le PDF temporaire
                await File.WriteAllBytesAsync(tempFilePath, bytes);

                // Demander confirmation avant d'imprimer
                var result = MessageBox.Show(
                    $"Le document PDF a été généré.\n\nVoulez-vous :\n• Cliquer OUI pour imprimer directement\n• Cliquer NON pour ouvrir l'aperçu avant impression",
                    "Impression de la Balance Annuelle",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ImprimerPdfDirectement(tempFilePath);
                }
                else if (result == MessageBoxResult.No)
                {
                    OuvrirPdfPourApercu(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur d'impression : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Imprime le PDF directement
        /// </summary>
        private void ImprimerPdfDirectement(string filePath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Verb = "print",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);

                NotificationService.ShowInfo(
                    "Le document a été envoyé à l'imprimante.\n\nSi la boîte de dialogue d'impression s'ouvre, sélectionnez votre imprimante et cliquez sur Imprimer.");
            }
            catch (Exception ex)
            {
                NotificationService.ShowWarning(
                    $"L'impression directe n'est pas disponible.\nLe document va s'ouvrir pour aperçu.\n\nDétail : {ex.Message}");

                OuvrirPdfPourApercu(filePath);
            }
        }

        /// <summary>
        /// Ouvre le PDF pour aperçu
        /// </summary>
        private void OuvrirPdfPourApercu(string filePath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };

                Process.Start(processInfo);

                NotificationService.ShowInfo(
                    "Le document PDF s'est ouvert dans votre lecteur par défaut.\n\nUtilisez Ctrl+P ou le menu Fichier > Imprimer pour lancer l'impression.");
            }
            catch (Exception ex)
            {
                NotificationService.ShowError(
                    $"Impossible d'ouvrir le document PDF.\n\nAssurez-vous d'avoir un lecteur PDF installé.\n\nDétail : {ex.Message}");
            }
        }

        #region Méthodes privées

        // ════════════════════════════════════════════════════════════
        // MÉTHODE APPELÉE QUAND L'EXERCICE CHANGE
        // ════════════════════════════════════════════════════════════
        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await ChargerBalanceAnnuelleAsync();
            });
        }

        // ════════════════════════════════════════════════════════════
        // MÉTHODE POUR SE DÉSABONNER (éviter les fuites mémoire)
        // ════════════════════════════════════════════════════════════
        public void Cleanup()
        {
            ExerciceService.Instance.ExerciceChanged -= OnExerciceChanged;
        }
        private BalanceAnnuelleFiltreDTO ConstruireFiltre()
        {
            return new BalanceAnnuelleFiltreDTO
            {
                Annee = AnneeSelectionnee,
                ClasseCompte = ClasseSelectionnee?.Classe,
                RechercheTexte = RechercheTexte,
                AfficherComptesVides = AfficherComptesVides
            };
        }

        private void MettreAJourTitre()
        {
            SousTitre = $"BALANCE ANNUELLE {AnneeSelectionnee}";
        }

        #endregion

        #region Handlers de changement de filtre

        partial void OnAnneeSelectionneeChanged(int value)
        {
            if (!IsLoading && value > 0)
            {
                _ = ChargerBalanceAnnuelleAsync();
            }
        }

        partial void OnClasseSelectionneeChanged(ClasseCompteItem? value)
        {
            if (!IsLoading)
            {
                _ = ChargerBalanceAnnuelleAsync();
            }
        }

        partial void OnRechercheTexteChanged(string value)
        {
            if (!IsLoading)
            {
                _ = ChargerBalanceAnnuelleAsync();
            }
        }

        partial void OnAfficherComptesVidesChanged(bool value)
        {
            if (!IsLoading)
            {
                _ = ChargerBalanceAnnuelleAsync();
            }
        }

        #endregion
    }
}