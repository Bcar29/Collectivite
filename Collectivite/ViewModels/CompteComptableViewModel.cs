using Collectivite.Models;
using Collectivite.Services;
using System;
using Collectivite.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Collectivite.ViewModels
{
    public class CompteComptableViewModel : ViewModelBase
    {
        private readonly CompteComptableService _compteService;
        private readonly NommenclatureService _nomenclatureService;

        private bool _isLoading;
        private CompteComptable? _selectedCompte;
        private bool _isDialogOpen;
        private CompteComptable _dialogCompte;
        private bool _isEditMode;

        // Nouvelles propriétés pour la gestion des nomenclatures
        private NatureType _natureSelectionnee = NatureType.Recette;
        private SectionType _sectionSelectionnee = SectionType.Fonctionnement;
        private Nommenclature? _nomenclatureSelectionnee;
        private bool _isNommenclatureMode = true;

        public CompteComptableViewModel(CompteComptableService compteService, NommenclatureService nomenclatureService)
        {
            _compteService = compteService;
            _nomenclatureService = nomenclatureService;

            _dialogCompte = new CompteComptable
            {
                NumeroCompte = "",
                IntituleCompte = "",
                ContrePartieId = null
            };

            // Commandes existantes
            LoadCompteCommand = new RelayCommand(async _ => await LoadCompteAsync());
            LoadComptesRacinesCommand = new RelayCommand(async _ => await LoadComptesRacinesAsync());
            LoadSousComptesCommand = new RelayCommand<int?>(async parentId => await LoadSousComptesAsync(parentId));
            OppenAddCompteCommand = new RelayCommand(_ => OpenAddCompte());
            OppenEditCompteCommand = new RelayCommand<CompteComptable>(compte => OpenEditCompte(compte));
            SaveCompteCommand = new RelayCommand(async _ => await SaveCompteAsync(), _ => CanSaveCompte());
            CancelCompteCommand = new RelayCommand(_ => CancelCompte());
            DeleteCompteCommand = new RelayCommand<CompteComptable>(async compte => await DeleteCompteAsync(compte));

            // Nouvelles commandes pour les nomenclatures
            LoadNomenclaturesCommand = new RelayCommand(async _ => await LoadNomenclaturesAsync());
            ChangerModeCommand = new RelayCommand<bool?>(mode => ChangerMode(mode));

            // Charger les données au démarrage
            LoadCompteCommand.Execute(null);
        }

        #region Properties 
        public ObservableCollection<CompteComptable> CompteComptables { get; } = [];
        public ObservableCollection<CompteComptable> ComptesParentDisponibles { get; } = [];

        // Nouvelle collection pour les nomenclatures
        public ObservableCollection<Nommenclature> NomenclaturesDisponibles { get; } = [];

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public CompteComptable? SelectedCompte
        {
            get => _selectedCompte;
            set => SetProperty(ref _selectedCompte, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public CompteComptable DialogCompte
        {
            get => _dialogCompte;
            set => SetProperty(ref _dialogCompte, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier le Compte" : "Ajouter un Compte";

        // Nouvelles propriétés pour la gestion des nomenclatures
        public NatureType NatureSelectionnee
        {
            get => _natureSelectionnee;
            set
            {
                if (SetProperty(ref _natureSelectionnee, value))
                {
                    LoadNomenclaturesCommand.Execute(null);
                }
            }
        }

        public SectionType SectionSelectionnee
        {
            get => _sectionSelectionnee;
            set
            {
                if (SetProperty(ref _sectionSelectionnee, value))
                {
                    LoadNomenclaturesCommand.Execute(null);
                }
            }
        }

        public Nommenclature? NommenclatureSelectionnee
        {
            get => _nomenclatureSelectionnee;
            set
            {
                if (SetProperty(ref _nomenclatureSelectionnee, value))
                {
                    OnNommenclatureSelected();
                }
            }
        }

        public bool IsNommenclatureMode
        {
            get => _isNommenclatureMode;
            set
            {
                if (SetProperty(ref _isNommenclatureMode, value))
                {
                    if (value)
                    {
                        // Mode nomenclature: recharger les nomenclatures
                        LoadNomenclaturesCommand.Execute(null);
                    }
                    else
                    {
                        // Mode saisie libre: réinitialiser
                        NommenclatureSelectionnee = null;
                    }
                }
            }
        }

        public bool IsSaisieLibreMode => !IsNommenclatureMode;

        #endregion

        #region Commands
        public ICommand LoadCompteCommand { get; }
        public ICommand LoadComptesRacinesCommand { get; }
        public ICommand LoadSousComptesCommand { get; }
        public ICommand OppenAddCompteCommand { get; }
        public ICommand OppenEditCompteCommand { get; }
        public ICommand SaveCompteCommand { get; }
        public ICommand CancelCompteCommand { get; }
        public ICommand DeleteCompteCommand { get; }

        // Nouvelles commandes
        public ICommand LoadNomenclaturesCommand { get; }
        public ICommand ChangerModeCommand { get; }
        #endregion

        #region Methods

        // Charger tous les comptes
        public async System.Threading.Tasks.Task LoadCompteAsync()
        {
            IsLoading = true;
            try
            {
                var comptes = await _compteService.GetCompteComptablesAsync();
                CompteComptables.Clear();
                foreach (var compte in comptes)
                {
                    CompteComptables.Add(compte);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des comptes : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Charger uniquement les comptes racines (sans parent)
        public async System.Threading.Tasks.Task LoadComptesRacinesAsync()
        {
            IsLoading = true;
            try
            {
                var comptes = await _compteService.GetComptesRacinesAsync();
                CompteComptables.Clear();
                foreach (var compte in comptes)
                {
                    CompteComptables.Add(compte);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des comptes racines : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Charger les sous-comptes d'un compte parent
        public async System.Threading.Tasks.Task LoadSousComptesAsync(int? parentId)
        {
            if (!parentId.HasValue)
                return;

            IsLoading = true;
            try
            {
                var sousComptes = await _compteService.GetSousComptesAsync(parentId.Value);
                CompteComptables.Clear();
                foreach (var compte in sousComptes)
                {
                    CompteComptables.Add(compte);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des sous-comptes : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Charger les comptes disponibles pour la sélection du parent
        private async System.Threading.Tasks.Task LoadComptesForParentSelectionAsync()
        {
            try
            {
                var comptes = await _compteService.GetCompteComptablesAsync();
                ComptesParentDisponibles.Clear();

                // Ajouter une option "Aucun parent" (compte racine)
                ComptesParentDisponibles.Add(new CompteComptable
                {
                    Id = 0,
                    NumeroCompte = "",
                    IntituleCompte = "-- Aucun parent (Compte racine) --"
                });

                foreach (var compte in comptes)
                {
                    // En mode édition, exclure le compte lui-même pour éviter qu'il soit son propre parent
                    if (IsEditMode && compte.Id == DialogCompte.Id)
                        continue;

                    ComptesParentDisponibles.Add(compte);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des comptes parents : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // NOUVELLE MÉTHODE: Charger les nomenclatures sans enfants avec filtres
        private async System.Threading.Tasks.Task LoadNomenclaturesAsync()
        {
            try
            {
                var nomenclatures = await _nomenclatureService.GetNommenclaturesSansEnfantsAvecFiltresAsync(
                    nature: NatureSelectionnee,
                    section: SectionSelectionnee
                );

                NomenclaturesDisponibles.Clear();
                foreach (var nomenclature in nomenclatures)
                {
                    NomenclaturesDisponibles.Add(nomenclature);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des nomenclatures : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // NOUVELLE MÉTHODE: Gestion de la sélection d'une nomenclature
        private void OnNommenclatureSelected()
        {
            if (NommenclatureSelectionnee != null && IsNommenclatureMode)
            {
                // Remplir automatiquement le numéro et l'intitulé
                DialogCompte.NumeroCompte = NommenclatureSelectionnee.CodeNomenclature;
                DialogCompte.IntituleCompte = NommenclatureSelectionnee.Intitule ?? "";

                // Notifier les changements
                OnPropertyChanged(nameof(DialogCompte));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // NOUVELLE MÉTHODE: Changer le mode (nomenclature vs saisie libre)
        private void ChangerMode(bool? isNommenclatureMode)
        {
            if (isNommenclatureMode.HasValue)
            {
                IsNommenclatureMode = isNommenclatureMode.Value;
            }
        }

        private async Task OpenAddCompte()
        {
            IsEditMode = false;
            DialogCompte = new CompteComptable
            {
                NumeroCompte = "",
                IntituleCompte = "",
                ContrePartieId = null
            };

            // Réinitialiser les sélections
            IsNommenclatureMode = true;
            NommenclatureSelectionnee = null;
            NatureSelectionnee = NatureType.Recette;
            SectionSelectionnee = SectionType.Fonctionnement;

           await LoadComptesForParentSelectionAsync();
            LoadNomenclaturesCommand.Execute(null);

            OnPropertyChanged(nameof(DialogCompte));
            IsDialogOpen = true;
        }

        private async Task OpenEditCompte(CompteComptable? compte)
        {
            if (compte == null)
                return;

            IsEditMode = true;
            DialogCompte = new CompteComptable
            {
                Id = compte.Id,
                NumeroCompte = compte.NumeroCompte,
                IntituleCompte = compte.IntituleCompte,
                ContrePartieId = compte.ContrePartieId
            };

            // En mode édition, on passe en saisie libre par défaut
            IsNommenclatureMode = false;
            NommenclatureSelectionnee = null;

           await  LoadComptesForParentSelectionAsync();
            OnPropertyChanged(nameof(DialogCompte));
            IsDialogOpen = true;
        }

        private bool CanSaveCompte()
        {
            return !string.IsNullOrWhiteSpace(DialogCompte.NumeroCompte) &&
                   !string.IsNullOrWhiteSpace(DialogCompte.IntituleCompte);
        }

        private async System.Threading.Tasks.Task SaveCompteAsync()
        {
            IsLoading = true;

            try
            {
                // Si l'ID du parent est 0 (option "Aucun parent"), on met null
                if (DialogCompte.ContrePartieId == 0)
                {
                    DialogCompte.ContrePartieId = null;
                }

                if (IsEditMode)
                {
                    var (success, message) = await _compteService.UpdateCompteComptable(DialogCompte);
                    if (success)
                    {
                        MessageBox.Show("Compte mis à jour avec succès",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadCompteAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var (success, message, _) = await _compteService.CreateCompteComptable(DialogCompte);
                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadCompteAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du compte : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelCompte()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteCompteAsync(CompteComptable? compte)
        {
            if (compte == null)
                return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le compte {compte.NumeroCompte} - {compte.IntituleCompte} ?\n\n" +
                $"Note : La suppression sera impossible si ce compte possède des sous-comptes.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _compteService.DeleteCompteComptableAsync(compte.Id);

                if (success)
                {
                    MessageBox.Show(message, "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCompteAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                IsLoading = false;
            }
        }

        #endregion
    }
}