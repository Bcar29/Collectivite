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
using System.Windows.Input;

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

        // ═══════════════════════════════════════════════════════════
        // 🆕 MODE D'AFFICHAGE (Cartes / DataGrid) - SYNTAXE CLASSIQUE
        // ═══════════════════════════════════════════════════════════

        private ModeAffichageGrandLivre _modeAffichage = ModeAffichageGrandLivre.Cartes;

        /// <summary>
        /// Mode d'affichage actuel (Cartes ou DataGrid)
        /// </summary>
        public ModeAffichageGrandLivre ModeAffichage
        {
            get => _modeAffichage;
            set
            {
                if (SetProperty(ref _modeAffichage, value))
                {
                    // Notifier les propriétés dérivées
                    OnPropertyChanged(nameof(EstModeCartes));
                    OnPropertyChanged(nameof(EstModeDataGrid));
                    OnPropertyChanged(nameof(LibelleBoutonMode));
                    OnPropertyChanged(nameof(IconeBoutonMode));

                    // Mettre à jour la collection DataGrid si on passe en mode DataGrid
                    if (value == ModeAffichageGrandLivre.DataGrid)
                    {
                        MettreAJourLignesDataGrid();
                    }
                }
            }
        }

        /// <summary>
        /// Indique si le mode Cartes est actif
        /// </summary>
        public bool EstModeCartes => ModeAffichage == ModeAffichageGrandLivre.Cartes;

        /// <summary>
        /// Indique si le mode DataGrid est actif
        /// </summary>
        public bool EstModeDataGrid => ModeAffichage == ModeAffichageGrandLivre.DataGrid;

        /// <summary>
        /// Libellé du bouton de changement de mode
        /// </summary>
        public string LibelleBoutonMode => EstModeCartes ? "Vue Tableau" : "Vue Cartes";

        /// <summary>
        /// Icône du bouton de changement de mode
        /// </summary>
        public string IconeBoutonMode => EstModeCartes ? "TableLarge" : "Cards";

        // 🆕 Collection aplatie pour le DataGrid (tous les mouvements) - SYNTAXE CLASSIQUE
        private ObservableCollection<GrandLivreLigneDTO> _lignesDataGrid = new();

        public ObservableCollection<GrandLivreLigneDTO> LignesDataGrid
        {
            get => _lignesDataGrid;
            set => SetProperty(ref _lignesDataGrid, value);
        }

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
            ExerciceService.Instance.ExerciceChanged += OnExerciceChanged;
        }

        // ═══════════════════════════════════════
        // ✅ ÉVÉNEMENT CHANGEMENT D'EXERCICE
        // ═══════════════════════════════════════
        private async void OnExerciceChanged(object? sender, EventArgs e)
        {
            await ChargerGrandLivreAsync();
        }

        // ═══════════════════════════════════════
        // ✅ MÉTHODE POUR SE DÉSABONNER
        // ═══════════════════════════════════════
        public void Cleanup()
        {
            ExerciceService.Instance.ExerciceChanged -= OnExerciceChanged;
        }

        // ═══════════════════════════════════════════════════════════
        // 🆕 COMMANDE POUR BASCULER LE MODE D'AFFICHAGE
        // ═══════════════════════════════════════════════════════════
        [RelayCommand]
        public void BasculerModeAffichage()
        {
            ModeAffichage = ModeAffichage == ModeAffichageGrandLivre.Cartes
                ? ModeAffichageGrandLivre.DataGrid
                : ModeAffichageGrandLivre.Cartes;
        }

        /// <summary>
        /// Convertit les comptes en lignes plates pour le DataGrid
        /// </summary>
        private void MettreAJourLignesDataGrid()
        {
            LignesDataGrid.Clear();

            foreach (var compte in Comptes)
            {
                // Ajouter chaque mouvement comme une ligne
                foreach (var mvt in compte.Mouvements)
                {
                    LignesDataGrid.Add(new GrandLivreLigneDTO
                    {
                        NumeroCompte = compte.NumeroCompte,
                        IntituleCompte = compte.IntituleCompte,
                        DateMouvement = mvt.DateEcriture,
                        Libelle = mvt.Libelle,
                        MontantDebit = mvt.MontantDebit,
                        MontantCredit = mvt.MontantCredit,
                        SoldeFormate = $"{mvt.SoldeCumulé:N0}",
                    });
                }

                // Ajouter une ligne de total pour chaque compte
                if (compte.Mouvements.Count != 0)
                {
                    LignesDataGrid.Add(new GrandLivreLigneDTO
                    {
                        NumeroCompte = compte.NumeroCompte,
                        IntituleCompte = $"TOTAL {compte.NumeroCompte}",
                        MontantDebit = compte.TotalDebit,
                        MontantCredit = compte.TotalCredit,
                        EstLigneTotal = true,
                        SoldeFormate = compte.SoldeFormate
                    });
                }
            }
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

                // Calculer les statistiques à partir des comptes déjà chargés (pas de nouvelle requête)
                Statistiques = _grandLivreService.CalculerStatistiques(comptes);

                // Mettre à jour le titre
                MettreAJourTitre();

                // 🆕 Mettre à jour les lignes DataGrid si on est en mode DataGrid
                if (ModeAffichage == ModeAffichageGrandLivre.DataGrid)
                {
                    MettreAJourLignesDataGrid();
                }
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
                    NotificationService.ShowSuccess("Export réussi !");
                }
            }
            catch (NotImplementedException)
            {
                NotificationService.ShowInfo("La fonctionnalité d'export Excel sera bientôt disponible.");
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
                    NotificationService.ShowSuccess("Export réussi !");
                }
            }
            catch (NotImplementedException)
            {
                NotificationService.ShowInfo("La fonctionnalité d'export PDF sera bientôt disponible.");
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
        /// Imprime le Grand Livre
        /// </summary>
        [RelayCommand]
        public async Task ImprimerAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = await _grandLivreService.ExportPdfAsync(filtre);

                // Créer un fichier temporaire
                string tempFileName = $"GrandLivre_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid():N}.pdf";
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), tempFileName);

                // Sauvegarder le PDF temporaire
                await System.IO.File.WriteAllBytesAsync(tempPath, bytes);

                // Ouvrir le PDF avec l'application par défaut
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });

                NotificationService.ShowInfo(
                    "Le document s'ouvre dans votre lecteur PDF.\n\n" +
                    "Utilisez Ctrl+P ou le menu Fichier → Imprimer pour lancer l'impression.");
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

    /// <summary>
    /// Énumération des modes d'affichage du Grand Livre
    /// </summary>
    public enum ModeAffichageGrandLivre
    {
        Cartes,
        DataGrid
    }

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

    /// <summary>
    /// DTO pour une ligne du DataGrid (mouvement aplati avec infos compte)
    /// </summary>
    public class GrandLivreLigneDTO
    {
        public string NumeroCompte { get; set; } = string.Empty;
        public string IntituleCompte { get; set; } = string.Empty;
        public DateOnly? DateMouvement { get; set; }
        public string? Libelle { get; set; }
        public decimal MontantDebit { get; set; }
        public decimal MontantCredit { get; set; }
        public string? NumeroOrdreRecette { get; set; }
        public string? NumeroMandat { get; set; }
        public bool EstLigneTotal { get; set; } = false;
        public string? SoldeFormate { get; set; }

        // Propriétés calculées pour l'affichage
        public string DebitFormate => MontantDebit > 0 ? $"{MontantDebit:N0}" : "";
        public string CreditFormate => MontantCredit > 0 ? $"{MontantCredit:N0}" : "";
        public string DateFormatee => DateMouvement?.ToString("dd/MM/yyyy") ?? "";
    }

    #endregion
}