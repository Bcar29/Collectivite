using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    public partial class TiersGestionViewModel : ObservableObject
    {
        private readonly ITiersGestionService _tiersService;

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

                // TODO: Implémenter l'export Excel
                await Task.Delay(100);

                MessageBox.Show("La fonctionnalité d'export Excel sera bientôt disponible.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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

                // TODO: Implémenter l'export PDF
                await Task.Delay(100);

                MessageBox.Show("La fonctionnalité d'export PDF sera bientôt disponible.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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
        public void Imprimer()
        {
            MessageBox.Show("La fonctionnalité d'impression sera bientôt disponible.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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