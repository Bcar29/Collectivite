using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    public partial class SecteurPDLViewModel : ObservableObject
    {
        private readonly ISecteurPDLService _secteurService;
        private readonly IProgrammePDLService _programmeService;

        #region Propriétés Observables

        [ObservableProperty]
        private ObservableCollection<SecteurPDL> _secteurs = new();

        [ObservableProperty]
        private ObservableCollection<ProgrammePDL> _programmes = new();

        [ObservableProperty]
        private SecteurPDL? _selectedSecteur;

        [ObservableProperty]
        private ProgrammePDL? _selectedProgrammeFiltre;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _messageErreur = string.Empty;

        [ObservableProperty]
        private string _searchText = string.Empty;

        // Propriétés du formulaire
        [ObservableProperty]
        private string _libelle = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private ProgrammePDL? _selectedProgramme;

        #endregion

        #region Propriétés Calculées

        public int TotalSecteurs => Secteurs.Count;

        public string TitrePage => IsEditing
            ? (SelectedSecteur?.Id > 0 ? "Modifier le Secteur" : "Nouveau Secteur")
            : "Secteurs PDL";

        #endregion

        #region Constructeur

        public SecteurPDLViewModel() : this(new SecteurPDLService(), new ProgrammePDLService()) { }

        public SecteurPDLViewModel(ISecteurPDLService secteurService, IProgrammePDLService programmeService)
        {
            _secteurService = secteurService;
            _programmeService = programmeService;
        }

        #endregion

        #region Commandes

        [RelayCommand]
        public async Task InitialiserAsync()
        {
            await ChargerProgrammesAsync();
            await ChargerDonneesAsync();
        }

        [RelayCommand]
        public async Task ChargerProgrammesAsync()
        {
            try
            {
                var programmes = await _programmeService.GetAllAsync();
                Programmes = new ObservableCollection<ProgrammePDL>(programmes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement programmes: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                List<SecteurPDL> secteurs;

                if (SelectedProgrammeFiltre != null)
                {
                    secteurs = await _secteurService.GetByProgrammeIdAsync(SelectedProgrammeFiltre.Id);
                }
                else
                {
                    secteurs = await _secteurService.GetAllAsync();
                }

                Secteurs = new ObservableCollection<SecteurPDL>(secteurs);
                OnPropertyChanged(nameof(TotalSecteurs));
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

        [RelayCommand]
        public void NouveauSecteur()
        {
            SelectedSecteur = new SecteurPDL();
            Libelle = string.Empty;
            Description = string.Empty;
            SelectedProgramme = Programmes.FirstOrDefault();
            IsEditing = true;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public void ModifierSecteur(SecteurPDL? secteur)
        {
            if (secteur == null) return;

            SelectedSecteur = secteur;
            Libelle = secteur.Libelle;
            Description = secteur.Description ?? string.Empty;
            SelectedProgramme = Programmes.FirstOrDefault(p => p.Id == secteur.ProgrammePDLId);
            IsEditing = true;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public async Task EnregistrerAsync()
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(Libelle))
                {
                    MessageBox.Show("Le libellé est obligatoire.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedProgramme == null)
                {
                    MessageBox.Show("Veuillez sélectionner un programme.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                MessageErreur = string.Empty;

                if (SelectedSecteur == null)
                {
                    SelectedSecteur = new SecteurPDL();
                }

                SelectedSecteur.Libelle = Libelle.Trim();
                SelectedSecteur.Description = Description;
                SelectedSecteur.ProgrammePDLId = SelectedProgramme.Id;

                if (SelectedSecteur.Id == 0)
                {
                    await _secteurService.CreateAsync(SelectedSecteur);
                    MessageBox.Show("Secteur créé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _secteurService.UpdateAsync(SelectedSecteur);
                    MessageBox.Show("Secteur modifié avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                IsEditing = false;
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur d'enregistrement : {ex.Message}";
                MessageBox.Show(MessageErreur, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void Annuler()
        {
            IsEditing = false;
            SelectedSecteur = null;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public async Task SupprimerAsync(SecteurPDL? secteur)
        {
            if (secteur == null) return;

            var result = MessageBox.Show(
                $"Voulez-vous vraiment supprimer le secteur \"{secteur.Libelle}\" ?\n\nCette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var activitesCount = await _secteurService.GetActivitesCountAsync(secteur.Id);
                if (activitesCount > 0)
                {
                    MessageBox.Show(
                        $"Impossible de supprimer ce secteur car il contient {activitesCount} activité(s).",
                        "Suppression impossible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var success = await _secteurService.DeleteAsync(secteur.Id);
                if (success)
                {
                    MessageBox.Show("Secteur supprimé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    await ChargerDonneesAsync();
                }
                else
                {
                    MessageBox.Show("Erreur lors de la suppression.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task FiltrerParProgrammeAsync()
        {
            await ChargerDonneesAsync();
        }

        [RelayCommand]
        public async Task ReinitialiserFiltreAsync()
        {
            SelectedProgrammeFiltre = null;
            await ChargerDonneesAsync();
        }

        #endregion

        #region Handlers

        partial void OnSelectedProgrammeFiltreChanged(ProgrammePDL? value)
        {
            _ = ChargerDonneesAsync();
        }

        #endregion
    }
}