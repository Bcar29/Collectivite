
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
    public partial class BalanceViewModel : ObservableObject
    {
        private readonly IBalanceService _balanceService;

        #region Propriétés observables

        [ObservableProperty]
        private ObservableCollection<BalanceLigneDTO> _lignes = new();

        [ObservableProperty]
        private BalanceTotauxDTO? _totaux;

        [ObservableProperty]
        private BalanceStatsDTO? _statistiques;

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
        private int _moisSelectionne;

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
        private string _sousTitre = "Balance Mensuelle";

        // Liste des mois
        public List<MoisViewItem> Mois { get; } = new()
        {
            new MoisViewItem { Numero = 1, Nom = "Janvier" },
            new MoisViewItem { Numero = 2, Nom = "Février" },
            new MoisViewItem { Numero = 3, Nom = "Mars" },
            new MoisViewItem { Numero = 4, Nom = "Avril" },
            new MoisViewItem { Numero = 5, Nom = "Mai" },
            new MoisViewItem { Numero = 6, Nom = "Juin" },
            new MoisViewItem { Numero = 7, Nom = "Juillet" },
            new MoisViewItem { Numero = 8, Nom = "Août" },
            new MoisViewItem { Numero = 9, Nom = "Septembre" },
            new MoisViewItem { Numero = 10, Nom = "Octobre" },
            new MoisViewItem { Numero = 11, Nom = "Novembre" },
            new MoisViewItem { Numero = 12, Nom = "Décembre" }
        };

        #endregion

        public BalanceViewModel(IBalanceService balanceService)
        {
            _balanceService = balanceService;
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
                AnneesDisponibles = await _balanceService.GetAnneesDisponiblesAsync();
                AnneeSelectionnee = DateTime.Now.Year;

                // Sélectionner le mois courant
                MoisSelectionne = DateTime.Now.Month;

                // Charger les classes de comptes
                var classes = await _balanceService.GetClassesComptesAsync();
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
                await ChargerBalanceAsync();
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
        /// Charge la balance avec les filtres actuels
        /// </summary>
        [RelayCommand]
        public async Task ChargerBalanceAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                var filtre = ConstruireFiltre();

                // Charger les données
                var lignes = await _balanceService.GetBalanceAsync(filtre);
                Lignes = new ObservableCollection<BalanceLigneDTO>(lignes);

                // Charger les totaux
                Totaux = await _balanceService.GetTotauxAsync(filtre);

                // Charger les statistiques
                Statistiques = await _balanceService.GetStatistiquesAsync(filtre);

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
            MoisSelectionne = DateTime.Now.Month;
            ClasseSelectionnee = ClassesComptes.FirstOrDefault();
            RechercheTexte = string.Empty;
            AfficherComptesVides = false;

            await ChargerBalanceAsync();
        }

        /// <summary>
        /// Exporte la balance en Excel
        /// </summary>
        [RelayCommand]
        public async Task ExporterExcelAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = await _balanceService.ExportExcelAsync(filtre);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Balance_{GetNomMois(MoisSelectionne)}_{AnneeSelectionnee}",
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
        /// Exporte la balance en PDF
        /// </summary>
        [RelayCommand]
        public async Task ExporterPdfAsync()
        {
            try
            {
                IsLoading = true;

                var filtre = ConstruireFiltre();
                var bytes = await _balanceService.ExportPdfAsync(filtre);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Balance_{GetNomMois(MoisSelectionne)}_{AnneeSelectionnee}",
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
        /// Imprime la balance (génère un PDF et l'envoie à l'imprimante)
        /// </summary>
        [RelayCommand]
        public async Task ImprimerAsync()
        {
            try
            {
                IsLoading = true;

                // Générer le PDF
                var filtre = ConstruireFiltre();
                var bytes = await _balanceService.ExportPdfAsync(filtre);

                // Créer un fichier temporaire
                string tempFileName = $"Balance_{GetNomMois(MoisSelectionne)}_{AnneeSelectionnee}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                // Sauvegarder le PDF temporaire
                await File.WriteAllBytesAsync(tempFilePath, bytes);

                // Demander confirmation avant d'imprimer
                var result = MessageBox.Show(
                    $"Le document PDF a été généré.\n\nVoulez-vous :\n• Cliquer OUI pour imprimer directement\n• Cliquer NON pour ouvrir l'aperçu avant impression",
                    "Impression de la Balance",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Impression directe via le lecteur PDF par défaut
                    ImprimerPdfDirectement(tempFilePath);
                }
                else if (result == MessageBoxResult.No)
                {
                    // Ouvrir le PDF pour aperçu (l'utilisateur pourra imprimer depuis le lecteur)
                    OuvrirPdfPourApercu(tempFilePath);
                }
                // Si Cancel, ne rien faire

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

        /// <summary>
        /// Imprime le PDF directement via la commande système
        /// </summary>
        private void ImprimerPdfDirectement(string filePath)
        {
            try
            {
                // Utiliser le verbe "print" pour imprimer directement
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
                // Si l'impression directe échoue, ouvrir en aperçu
                MessageBox.Show(
                    $"L'impression directe n'est pas disponible.\nLe document va s'ouvrir pour aperçu.\n\nDétail : {ex.Message}",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                OuvrirPdfPourApercu(filePath);
            }
        }

        /// <summary>
        /// Ouvre le PDF dans le lecteur par défaut pour aperçu
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

                MessageBox.Show(
                    "Le document PDF s'est ouvert dans votre lecteur par défaut.\n\nUtilisez Ctrl+P ou le menu Fichier > Imprimer pour lancer l'impression.",
                    "Aperçu avant impression",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible d'ouvrir le document PDF.\n\nAssurez-vous d'avoir un lecteur PDF installé (Adobe Reader, Foxit, etc.)\n\nDétail : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #region Méthodes privées

        private BalanceFiltreDTO ConstruireFiltre()
        {
            return new BalanceFiltreDTO
            {
                Annee = AnneeSelectionnee,
                Mois = MoisSelectionne,
                ClasseCompte = ClasseSelectionnee?.Classe,
                RechercheTexte = RechercheTexte,
                AfficherComptesVides = AfficherComptesVides
            };
        }

        private void MettreAJourTitre()
        {
            var nomMois = GetNomMois(MoisSelectionne);
            SousTitre = $"Balance Mensuelle {nomMois.ToUpper()} {AnneeSelectionnee}";
        }

        private string GetNomMois(int mois)
        {
            return Mois.FirstOrDefault(m => m.Numero == mois)?.Nom ?? "";
        }

        #endregion

        #region Handlers de changement de filtre

        partial void OnAnneeSelectionneeChanged(int value)
        {
            if (!IsLoading && value > 0)
            {
                _ = ChargerBalanceAsync();
            }
        }

        partial void OnMoisSelectionneChanged(int value)
        {
            if (!IsLoading && value > 0)
            {
                _ = ChargerBalanceAsync();
            }
        }

        partial void OnClasseSelectionneeChanged(ClasseCompteItem? value)
        {
            if (!IsLoading)
            {
                _ = ChargerBalanceAsync();
            }
        }

        partial void OnRechercheTexteChanged(string value)
        {
            if (!IsLoading)
            {
                _ = ChargerBalanceAsync();
            }
        }

        partial void OnAfficherComptesVidesChanged(bool value)
        {
            if (!IsLoading)
            {
                _ = ChargerBalanceAsync();
            }
        }

        #endregion
    }

    #region Classes helpers

    public class MoisViewItem
    {
        public int Numero { get; set; }
        public string Nom { get; set; } = string.Empty;
    }

    public class ClasseCompteItem
    {
        public string? Classe { get; set; }
        public string Libelle { get; set; } = string.Empty;
    }

    #endregion
}