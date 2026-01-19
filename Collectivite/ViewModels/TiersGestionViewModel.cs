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
using Microsoft.Win32;

namespace Collectivite.ViewModels
{
    public partial class TiersGestionViewModel : ObservableObject
    {
        private readonly ITiersGestionService _tiersService;
        private readonly TiersExportService _tiersExportService;

        #region Propriétés observables

        // ═══════════════════════════════════════════════════════════
        // COLLECTIONS
        // ═══════════════════════════════════════════════════════════

        [ObservableProperty]
        private ObservableCollection<TiersDebiteurDTO> _debiteurs = new();

        [ObservableProperty]
        private ObservableCollection<TiersCreancierDTO> _creanciers = new();

        [ObservableProperty]
        private TiersDebiteurDTO? _debiteurSelectionne;

        [ObservableProperty]
        private TiersCreancierDTO? _creancierSelectionne;

        // ═══════════════════════════════════════════════════════════
        // STATISTIQUES
        // ═══════════════════════════════════════════════════════════

        [ObservableProperty]
        private TiersStatistiquesDTO? _statistiques;

        // Statistiques Débiteurs
        [ObservableProperty]
        private int _nombreDebiteurs;

        [ObservableProperty]
        private string _totalAPayerFormate = "0";

        [ObservableProperty]
        private string _totalPayeFormate = "0";

        [ObservableProperty]
        private string _resteAPayerFormate = "0";

        // Statistiques Créanciers
        [ObservableProperty]
        private int _nombreCreanciers;

        [ObservableProperty]
        private string _totalAEncaisserFormate = "0";

        [ObservableProperty]
        private string _totalEncaisseFormate = "0";

        [ObservableProperty]
        private string _resteAEncaisserFormate = "0";

        // ═══════════════════════════════════════════════════════════
        // ONGLETS
        // ═══════════════════════════════════════════════════════════

        private int _ongletSelectionne;
        public int OngletSelectionne
        {
            get => _ongletSelectionne;
            set
            {
                if (SetProperty(ref _ongletSelectionne, value))
                {
                    OnPropertyChanged(nameof(EstOngletDebiteurs));
                    OnPropertyChanged(nameof(EstOngletCreanciers));
                    OnPropertyChanged(nameof(TitreOnglet));
                }
            }
        }

        public bool EstOngletDebiteurs => OngletSelectionne == 0;
        public bool EstOngletCreanciers => OngletSelectionne == 1;

        public string TitreOnglet => EstOngletDebiteurs ? "Débiteurs" : "Créanciers";

        // ═══════════════════════════════════════════════════════════
        // FILTRES
        // ═══════════════════════════════════════════════════════════

        [ObservableProperty]
        private string _rechercheTexte = string.Empty;

        [ObservableProperty]
        private string _statutFiltre = "Tous";

        [ObservableProperty]
        private bool _inclureSoldes = true;

        public List<string> StatutsDisponibles { get; } = new()
        {
            "Tous",
            "Soldé",
            "En cours",
            "Non payé"
        };

        // ═══════════════════════════════════════════════════════════
        // ÉTAT
        // ═══════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _messageErreur = string.Empty;

        [ObservableProperty]
        private string _titrePage = "Gestion des Tiers";

        #endregion

        #region Constructeur

        public TiersGestionViewModel(ITiersGestionService tiersService)
        {
            _tiersService = tiersService;
            _tiersExportService = new TiersExportService();

            // S'abonner au changement d'exercice
            ExerciceService.Instance.ExerciceChanged += OnExerciceChanged;
        }

        public TiersGestionViewModel() : this(new TiersGestionService())
        {
        }

        #endregion

        #region Événements

        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await ChargerDonneesAsync();
            });
        }

        public void Cleanup()
        {
            ExerciceService.Instance.ExerciceChanged -= OnExerciceChanged;
        }

        #endregion

        #region Commandes

        /// <summary>
        /// Initialise le ViewModel
        /// </summary>
        [RelayCommand]
        public async Task InitialiserAsync()
        {
            await ChargerDonneesAsync();
        }

        /// <summary>
        /// Charge toutes les données (débiteurs, créanciers, statistiques)
        /// </summary>
        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                var filtre = ConstruireFiltre();

                // Charger en parallèle
                var debiteursTache = _tiersService.GetTiersDebiteursAsync(filtre);
                var creanciersTache = _tiersService.GetTiersCreanciersAsync(filtre);
                var statsTache = _tiersService.GetStatistiquesAsync();

                await Task.WhenAll(debiteursTache, creanciersTache, statsTache);

                // Mettre à jour les collections
                Debiteurs = new ObservableCollection<TiersDebiteurDTO>(await debiteursTache);
                Creanciers = new ObservableCollection<TiersCreancierDTO>(await creanciersTache);
                Statistiques = await statsTache;

                // Mettre à jour les statistiques affichées
                MettreAJourStatistiques();

                // Mettre à jour le titre
                var exercice = ExerciceService.Instance.CurrentExercice;
                TitrePage = $"Gestion des Tiers - {exercice?.Libelle}";
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
        /// Réinitialise les filtres
        /// </summary>
        [RelayCommand]
        public async Task ReinitialiserFiltresAsync()
        {
            RechercheTexte = string.Empty;
            StatutFiltre = "Tous";
            InclureSoldes = true;

            await ChargerDonneesAsync();
        }

        /// <summary>
        /// Exporte les données en Excel
        /// </summary>
        [RelayCommand]
        public async Task ExporterExcelAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = EstOngletDebiteurs
                    ? await _tiersExportService.ExportDebiteursExcelAsync(Debiteurs.ToList(), filtre)
                    : await _tiersExportService.ExportCreanciersExcelAsync(Creanciers.ToList(), filtre);

                var dialog = new SaveFileDialog
                {
                    FileName = $"{TitreOnglet}_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".xlsx",
                    Filter = "Fichiers Excel|*.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, bytes);
                    MessageBox.Show("Export Excel réussi !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'export : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Exporte les données en PDF
        /// </summary>
        [RelayCommand]
        public async Task ExporterPdfAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = EstOngletDebiteurs
                    ? await _tiersExportService.ExportDebiteursPdfAsync(Debiteurs.ToList(), filtre)
                    : await _tiersExportService.ExportCreanciersPdfAsync(Creanciers.ToList(), filtre);

                var dialog = new SaveFileDialog
                {
                    FileName = $"{TitreOnglet}_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".pdf",
                    Filter = "Fichiers PDF|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, bytes);
                    MessageBox.Show("Export PDF réussi !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'export : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Imprime les données
        /// </summary>
        [RelayCommand]
        public async Task ImprimerAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = EstOngletDebiteurs
                    ? await _tiersExportService.ExportDebiteursPdfAsync(Debiteurs.ToList(), filtre)
                    : await _tiersExportService.ExportCreanciersPdfAsync(Creanciers.ToList(), filtre);

                string tempFileName = $"{TitreOnglet}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                await File.WriteAllBytesAsync(tempFilePath, bytes);

                var result = MessageBox.Show(
                    "Le document PDF a été généré.\n\nVoulez-vous :\n• Cliquer OUI pour imprimer directement\n• Cliquer NON pour ouvrir l'aperçu avant impression",
                    $"Impression - {TitreOnglet}",
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
                MessageBox.Show($"Erreur d'impression : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

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

                MessageBox.Show(
                    "Le document a été envoyé à l'imprimante.\n\nSi la boîte de dialogue d'impression s'ouvre, sélectionnez votre imprimante et cliquez sur Imprimer.",
                    "Impression en cours",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"L'impression directe n'est pas disponible.\nLe document va s'ouvrir pour aperçu.\n\nDétail : {ex.Message}",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                OuvrirPdfPourApercu(filePath);
            }
        }

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le PDF : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Voir le détail d'un débiteur
        /// </summary>
        [RelayCommand]
        public void VoirDetailDebiteur(TiersDebiteurDTO? debiteur)
        {
            if (debiteur == null) return;
            DebiteurSelectionne = debiteur;
            // TODO: Ouvrir une fenêtre de détail
        }

        /// <summary>
        /// Voir le détail d'un créancier
        /// </summary>
        [RelayCommand]
        public void VoirDetailCreancier(TiersCreancierDTO? creancier)
        {
            if (creancier == null) return;
            CreancierSelectionne = creancier;
            // TODO: Ouvrir une fenêtre de détail
        }

        #endregion

        #region Méthodes privées

        private TiersFiltreDTO ConstruireFiltre()
        {
            var statut = StatutFiltre;

            // Adapter le statut pour les créanciers
            if (EstOngletCreanciers && statut == "Non payé")
            {
                statut = "Non encaissé";
            }

            return new TiersFiltreDTO
            {
                ExerciceId = ExerciceService.Instance.CurrentExercice?.Id,
                RechercheTexte = RechercheTexte,
                Statut = statut,
                IncluireSoldes = InclureSoldes
            };
        }

        private void MettreAJourStatistiques()
        {
            if (Statistiques == null) return;

            // Débiteurs
            NombreDebiteurs = Statistiques.NombreDebiteurs;
            TotalAPayerFormate = Statistiques.TotalAPayerFormate;
            TotalPayeFormate = Statistiques.TotalPayeFormate;
            ResteAPayerFormate = Statistiques.ResteAPayerFormate;

            // Créanciers
            NombreCreanciers = Statistiques.NombreCreanciers;
            TotalAEncaisserFormate = Statistiques.TotalAEncaisserFormate;
            TotalEncaisseFormate = Statistiques.TotalEncaisseFormate;
            ResteAEncaisserFormate = Statistiques.ResteAEncaisserFormate;
        }

        #endregion

        #region Handlers de changement de filtre

        partial void OnRechercheTexteChanged(string value)
        {
            if (!IsLoading)
            {
                _ = ChargerDonneesAsync();
            }
        }

        partial void OnStatutFiltreChanged(string value)
        {
            if (!IsLoading)
            {
                _ = ChargerDonneesAsync();
            }
        }

        partial void OnInclureSoldesChanged(bool value)
        {
            if (!IsLoading)
            {
                _ = ChargerDonneesAsync();
            }
        }

        #endregion
    }
}