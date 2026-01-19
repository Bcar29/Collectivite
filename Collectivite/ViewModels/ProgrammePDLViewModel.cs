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
    public partial class ProgrammePDLViewModel : ObservableObject
    {
        private readonly IProgrammePDLService _programmeService;

        #region Propriétés Observables

        [ObservableProperty]
        private ObservableCollection<ProgrammePDL> _programmes = new();

        [ObservableProperty]
        private ProgrammePDL? _selectedProgramme;

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

        #endregion

        #region Propriétés Calculées

        public int TotalProgrammes => Programmes.Count;

        public string TitrePage => IsEditing
            ? (SelectedProgramme?.Id > 0 ? "Modifier le Programme" : "Nouveau Programme")
            : "Programmes PDL";

        #endregion

        #region Constructeur

        public ProgrammePDLViewModel() : this(new ProgrammePDLService()) { }

        public ProgrammePDLViewModel(IProgrammePDLService programmeService)
        {
            _programmeService = programmeService;
        }

        #endregion

        #region Commandes

        [RelayCommand]
        public async Task InitialiserAsync()
        {
            await ChargerDonneesAsync();
        }

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                var programmes = await _programmeService.GetAllAsync();
                Programmes = new ObservableCollection<ProgrammePDL>(programmes);

                OnPropertyChanged(nameof(TotalProgrammes));
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
        public void NouveauProgramme()
        {
            SelectedProgramme = new ProgrammePDL();
            Libelle = string.Empty;
            Description = string.Empty;
            IsEditing = true;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public void ModifierProgramme(ProgrammePDL? programme)
        {
            if (programme == null) return;

            SelectedProgramme = programme;
            Libelle = programme.Libelle;
            Description = programme.Description ?? string.Empty;
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

                IsLoading = true;
                MessageErreur = string.Empty;

                if (SelectedProgramme == null)
                {
                    SelectedProgramme = new ProgrammePDL();
                }

                SelectedProgramme.Libelle = Libelle.Trim();
                SelectedProgramme.Description = Description;

                if (SelectedProgramme.Id == 0)
                {
                    await _programmeService.CreateAsync(SelectedProgramme);
                    MessageBox.Show("Programme créé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _programmeService.UpdateAsync(SelectedProgramme);
                    MessageBox.Show("Programme modifié avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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
            SelectedProgramme = null;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public async Task SupprimerAsync(ProgrammePDL? programme)
        {
            if (programme == null) return;

            var result = MessageBox.Show(
                $"Voulez-vous vraiment supprimer le programme \"{programme.Libelle}\" ?\n\nCette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var secteursCount = await _programmeService.GetSecteursCountAsync(programme.Id);
                if (secteursCount > 0)
                {
                    MessageBox.Show(
                        $"Impossible de supprimer ce programme car il contient {secteursCount} secteur(s).",
                        "Suppression impossible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var success = await _programmeService.DeleteAsync(programme.Id);
                if (success)
                {
                    MessageBox.Show("Programme supprimé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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

        #endregion

        #region Méthodes de Filtrage

        partial void OnSearchTextChanged(string value)
        {
            // Filtrage local si nécessaire
        }

        #endregion
    }
}