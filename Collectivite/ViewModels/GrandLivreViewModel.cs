using Collectivite.Services;
using Collectivite.Models;
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
    public partial class GrandLivreViewModel : ObservableObject
    {
        private readonly IGrandLivreService _grandLivreService;

        #region Propriétés observables

        [ObservableProperty]
        private ObservableCollection<GrandLivreCompteDTO> _comptes = new();

        [ObservableProperty]
        private GrandLivreCompteDTO? _compteSelectionne;

        [ObservableProperty]
        private GrandLivreStatsDTO? _statistiques;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _messageErreur = string.Empty;

        // Filtres
        [ObservableProperty]
        private List<int> _anneesDisponibles = new();

        [ObservableProperty]
        private int? _anneeSelectionnee;

        [ObservableProperty]
        private int? _moisSelectionne;

        [ObservableProperty]
        private List<CompteFiltreItem> _comptesDisponibles = new();

        [ObservableProperty]
        private CompteFiltreItem? _compteFiltreSelectionne;

        [ObservableProperty]
        private string _rechercheTexte = string.Empty;

        [ObservableProperty]
        private bool _afficherComptesVides = false;

        [ObservableProperty]
        private DateOnly? _dateDebut;

        [ObservableProperty]
        private DateOnly? _dateFin;

        // Pour le titre dynamique
        [ObservableProperty]
        private string _titrePeriode = "Grand Livre";

        // Liste des mois
        public List<MoisItem> Mois { get; } = new()
        {
            new MoisItem { Numero = null, Nom = "Tous les mois" },
            new MoisItem { Numero = 1, Nom = "Janvier" },
            new MoisItem { Numero = 2, Nom = "Février" },
            new MoisItem { Numero = 3, Nom = "Mars" },
            new MoisItem { Numero = 4, Nom = "Avril" },
            new MoisItem { Numero = 5, Nom = "Mai" },
            new MoisItem { Numero = 6, Nom = "Juin" },
            new MoisItem { Numero = 7, Nom = "Juillet" },
            new MoisItem { Numero = 8, Nom = "Août" },
            new MoisItem { Numero = 9, Nom = "Septembre" },
            new MoisItem { Numero = 10, Nom = "Octobre" },
            new MoisItem { Numero = 11, Nom = "Novembre" },
            new MoisItem { Numero = 12, Nom = "Décembre" }
        };

        #endregion

        public GrandLivreViewModel(IGrandLivreService grandLivreService)
        {
            _grandLivreService = grandLivreService;

            // ✅ S'abonner à l'événement de changement d'exercice
            // ✅ AJOUTER CETTE LIGNE
            ExerciceService.Instance.ExerciceChanged += OnExerciceChanged;
        }

        // ═══════════════════════════════════════
        // ✅ ÉVÉNEMENT CHANGEMENT D'EXERCICE
        // ═══════════════════════════════════════
        private async void OnExerciceChanged(object? sender, EventArgs e)
        {
            // Recharger les données automatiquement quand l'exercice change
            await ChargerGrandLivreAsync();
        }

        // ═══════════════════════════════════════
        // ✅ MÉTHODE POUR SE DÉSABONNER
        // ═══════════════════════════════════════
        public void Cleanup()
        {
            ExerciceService.Instance.ExerciceChanged -= OnExerciceChanged;
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
                AnneesDisponibles = await _grandLivreService.GetAnneesDisponiblesAsync();

                // Sélectionner l'année courante par défaut
                AnneeSelectionnee = DateTime.Now.Year;

                // Sélectionner le mois courant par défaut
                MoisSelectionne = DateTime.Now.Month;

                // Charger la liste des comptes pour le filtre
                var comptes = await _grandLivreService.GetComptesListAsync();
                ComptesDisponibles = new List<CompteFiltreItem>
                {
                    new CompteFiltreItem { NumeroCompte = null, Libelle = "Tous les comptes" }
                };
                ComptesDisponibles.AddRange(comptes.Select(c => new CompteFiltreItem
                {
                    NumeroCompte = c.Numero,
                    Libelle = $"{c.Numero} - {c.Intitule}"
                }));

                // Charger le Grand Livre
                await ChargerGrandLivreAsync();
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
        /// Charge le Grand Livre avec les filtres actuels
        /// </summary>
        [RelayCommand]
        public async Task ChargerGrandLivreAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                var filtre = ConstruireFiltre();

                // Charger les comptes
                var comptes = await _grandLivreService.GetGrandLivreAsync(filtre);
                Comptes = new ObservableCollection<GrandLivreCompteDTO>(comptes);

                // Charger les statistiques
                Statistiques = await _grandLivreService.GetStatistiquesAsync(filtre);

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
            MoisSelectionne = null;
            CompteFiltreSelectionne = ComptesDisponibles.FirstOrDefault();
            RechercheTexte = string.Empty;
            AfficherComptesVides = false;
            DateDebut = null;
            DateFin = null;

            await ChargerGrandLivreAsync();
        }

        /// <summary>
        /// Exporte le Grand Livre en Excel
        /// </summary>
        [RelayCommand]
        public async Task ExporterExcelAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = await _grandLivreService.ExportExcelAsync(filtre);

                // Sauvegarder le fichier
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"GrandLivre_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".xlsx",
                    Filter = "Fichiers Excel|*.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllBytesAsync(dialog.FileName, bytes);
                    MessageBox.Show("Export réussi !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (NotImplementedException)
            {
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
        /// Exporte le Grand Livre en PDF
        /// </summary>
        [RelayCommand]
        public async Task ExporterPdfAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = await _grandLivreService.ExportPdfAsync(filtre);

                // Sauvegarder le fichier
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"GrandLivre_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".pdf",
                    Filter = "Fichiers PDF|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllBytesAsync(dialog.FileName, bytes);
                    MessageBox.Show("Export réussi !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (NotImplementedException)
            {
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
        /// Imprime le Grand Livre
        /// </summary>
        [RelayCommand]
        public void Imprimer()
        {
            MessageBox.Show("La fonctionnalité d'impression sera bientôt disponible.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Affiche les détails d'un compte
        /// </summary>
        [RelayCommand]
        public void VoirDetailCompte(GrandLivreCompteDTO compte)
        {
            CompteSelectionne = compte;
            // Ouvrir une fenêtre de détail ou naviguer vers une page de détail
        }

        #region Méthodes privées

        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await ChargerGrandLivreAsync();
            });
        }
        private GrandLivreFiltreDTO ConstruireFiltre()
        {
            return new GrandLivreFiltreDTO
            {
                Annee = AnneeSelectionnee,
                Mois = MoisSelectionne,
                NumeroCompte = CompteFiltreSelectionne?.NumeroCompte,
                RechercheTexte = RechercheTexte,
                DateDebut = DateDebut,
                DateFin = DateFin,
                InclureComptesVides = AfficherComptesVides
            };
        }

        private void MettreAJourTitre()
        {
            var parties = new List<string> { "Grand Livre" };

            if (MoisSelectionne.HasValue)
            {
                var mois = Mois.FirstOrDefault(m => m.Numero == MoisSelectionne.Value);
                if (mois != null)
                {
                    parties.Add($"du mois de {mois.Nom}");
                }
            }

            if (AnneeSelectionnee.HasValue)
            {
                parties.Add(AnneeSelectionnee.Value.ToString());
            }

            TitrePeriode = string.Join(" ", parties);
        }

        #endregion

        #region Handlers de changement de filtre

        partial void OnAnneeSelectionneeChanged(int? value)
        {
            if (!IsLoading)
            {
                _ = ChargerGrandLivreAsync();
            }
        }

        partial void OnMoisSelectionneChanged(int? value)
        {
            if (!IsLoading)
            {
                _ = ChargerGrandLivreAsync();
            }
        }

        partial void OnCompteFiltreSelectionneChanged(CompteFiltreItem? value)
        {
            if (!IsLoading)
            {
                _ = ChargerGrandLivreAsync();
            }
        }

        partial void OnRechercheTexteChanged(string value)
        {
            if (!IsLoading)
            {
                _ = ChargerGrandLivreAsync();
            }
        }

        partial void OnAfficherComptesVidesChanged(bool value)
        {
            if (!IsLoading)
            {
                _ = ChargerGrandLivreAsync();
            }
        }

        #endregion
    }

    #region Classes helpers

    public class MoisItem
    {
        public int? Numero { get; set; }
        public string Nom { get; set; } = string.Empty;
    }

    public class CompteFiltreItem
    {
        public string? NumeroCompte { get; set; }
        public string Libelle { get; set; } = string.Empty;
    }

    #endregion
}