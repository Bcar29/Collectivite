using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfSharp.Pdf.Content.Objects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    public partial class PDLViewModel : ObservableObject
    {
        private readonly IPDLService _pdlService;

        #region Propriétés Observables

        [ObservableProperty]
        private ObservableCollection<PDL> _pdlList = new();

        [ObservableProperty]
        private PDL? _selectedPDL;

        [ObservableProperty]
        private PDL? _currentPDL;

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
        private DateTime _dateDebut = DateTime.Today;

        [ObservableProperty]
        private DateTime _dateFin = DateTime.Today.AddYears(5);

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _ficName = string.Empty;

        [ObservableProperty]
        private byte[]? _fichierJoin;

        #endregion

        #region Propriétés Calculées

        public int TotalPDL => PdlList.Count;

        public string TitrePage => IsEditing
            ? (SelectedPDL?.Id > 0 ? "Modifier le PDL" : "Nouveau PDL")
            : "Programme de Développement Local";

        #endregion

        #region Constructeur

        public PDLViewModel() : this(new PDLService()) { }

        public PDLViewModel(IPDLService pdlService)
        {
            _pdlService = pdlService;
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

                var pdlList = await _pdlService.GetAllAsync();
                PdlList = new ObservableCollection<PDL>(pdlList);

                // Récupérer le PDL courant
                CurrentPDL = await _pdlService.GetCurrentPDLAsync();

                OnPropertyChanged(nameof(TotalPDL));
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
        public void NouveauPDL()
        {
            SelectedPDL = new PDL();
            DateDebut = DateTime.Today;
            DateFin = DateTime.Today.AddYears(5);
            Description = string.Empty;
            FicName = string.Empty;
            FichierJoin = null;
            IsEditing = true;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public void ModifierPDL(PDL? pdl)
        {
            if (pdl == null) return;

            SelectedPDL = pdl;
            DateDebut = pdl.DateDebut;
            DateFin = pdl.DateFin;
            Description = pdl.Description ?? string.Empty;
            FicName = pdl.FicName ?? string.Empty;
            FichierJoin = pdl.FickierJoin;
            IsEditing = true;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public async Task EnregistrerAsync()
        {
            try
            {
                // Validation
                if (DateDebut >= DateFin)
                {
                    MessageBox.Show("La date de début doit être antérieure à la date de fin.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                MessageErreur = string.Empty;

                if (SelectedPDL == null)
                {
                    SelectedPDL = new PDL();
                }

                SelectedPDL.DateDebut = DateDebut;
                SelectedPDL.DateFin = DateFin;
                SelectedPDL.Description = Description;
                SelectedPDL.FicName = FicName;
                SelectedPDL.FickierJoin = FichierJoin;

                if (SelectedPDL.Id == 0)
                {
                    await _pdlService.CreateAsync(SelectedPDL);
                    MessageBox.Show("PDL créé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _pdlService.UpdateAsync(SelectedPDL);
                    MessageBox.Show("PDL modifié avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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
            SelectedPDL = null;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public async Task SupprimerAsync(PDL? pdl)
        {
            if (pdl == null) return;

            var result = MessageBox.Show(
                $"Voulez-vous vraiment supprimer ce PDL ({pdl.DateDebut:yyyy} - {pdl.DateFin:yyyy}) ?\n\nCette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var activitesCount = await _pdlService.GetActivitesCountAsync(pdl.Id);
                if (activitesCount > 0)
                {
                    MessageBox.Show(
                        $"Impossible de supprimer ce PDL car il contient {activitesCount} activité(s).",
                        "Suppression impossible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var success = await _pdlService.DeleteAsync(pdl.Id);
                if (success)
                {
                    MessageBox.Show("PDL supprimé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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
        public async Task VoirDetailsAsync(PDL? pdl)
        {
            if (pdl == null) return;

            try
            {
                IsLoading = true;
                SelectedPDL = await _pdlService.GetByIdWithDetailsAsync(pdl.Id);
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