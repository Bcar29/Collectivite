using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    public partial class ActivitePDLViewModel : ObservableObject
    {
        private readonly IActivitePDLService _activiteService;
        private readonly IPDLService _pdlService;
        private readonly ISecteurPDLService _secteurService;
        private readonly IProgrammePDLService _programmeService;
        private readonly IODDService _oddService;
        private readonly ICompetenceCollectiviteService _competenceService;
        private readonly IBeneficiairePDLService _beneficiaireService;
        private readonly IActeurPDLService _acteurService;
        private readonly IStructureExecutionPDLService _structureService;

        #region Collections de données

        [ObservableProperty]
        private ObservableCollection<ActivitePDL> _activites = new();

        [ObservableProperty]
        private ObservableCollection<PDL> _pdlList = new();

        [ObservableProperty]
        private ObservableCollection<ProgrammePDL> _programmes = new();

        [ObservableProperty]
        private ObservableCollection<SecteurPDL> _secteurs = new();

        [ObservableProperty]
        private ObservableCollection<SecteurPDL> _secteursFiltres = new();

        [ObservableProperty]
        private ObservableCollection<ODD> _oddList = new();

        [ObservableProperty]
        private ObservableCollection<CompetenceCollectivite> _competences = new();

        [ObservableProperty]
        private ObservableCollection<BeneficiairePDL> _beneficiaires = new();

        [ObservableProperty]
        private ObservableCollection<ActeurPDL> _acteurs = new();

        [ObservableProperty]
        private ObservableCollection<StructureExecutionPDL> _structures = new();

        // Sélections multiples (N-N)
        [ObservableProperty]
        private ObservableCollection<BeneficiairePDL> _selectedBeneficiaires = new();

        [ObservableProperty]
        private ObservableCollection<ActeurPDL> _selectedActeurs = new();

        [ObservableProperty]
        private ObservableCollection<StructureExecutionPDL> _selectedStructures = new();

        #endregion

        #region Propriétés de sélection

        [ObservableProperty]
        private ActivitePDL? _selectedActivite;

        [ObservableProperty]
        private PDL? _selectedPDL;

        [ObservableProperty]
        private ProgrammePDL? _selectedProgramme;

        [ObservableProperty]
        private SecteurPDL? _selectedSecteur;

        [ObservableProperty]
        private ODD? _selectedODD;

        [ObservableProperty]
        private CompetenceCollectivite? _selectedCompetence;

        // Filtres
        [ObservableProperty]
        private PDL? _filtrePDL;

        [ObservableProperty]
        private SecteurPDL? _filtreSecteur;

        #endregion

        #region Propriétés du formulaire

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _resultat = string.Empty;

        [ObservableProperty]
        private DateTime _dateDebut = DateTime.Today;

        [ObservableProperty]
        private DateTime _dateFin = DateTime.Today.AddMonths(6);

        [ObservableProperty]
        private string _financementInterne = string.Empty;

        [ObservableProperty]
        private string _financementExterne = string.Empty;

        #endregion

        #region État de l'interface

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _messageErreur = string.Empty;

        [ObservableProperty]
        private string _searchText = string.Empty;

        #endregion

        #region Propriétés Calculées

        public int TotalActivites => Activites.Count;

        public string TitrePage => IsEditing
            ? (SelectedActivite?.Id > 0 ? "Modifier l'Activité" : "Nouvelle Activité")
            : "Activités PDL";

        #endregion

        #region Constructeur

        public ActivitePDLViewModel() : this(
            new ActivitePDLService(),
            new PDLService(),
            new SecteurPDLService(),
            new ProgrammePDLService(),
            new ODDService(),
            new CompetenceCollectiviteService(),
            new BeneficiairePDLService(),
            new ActeurPDLService(),
            new StructureExecutionPDLService())
        { }

        public ActivitePDLViewModel(
            IActivitePDLService activiteService,
            IPDLService pdlService,
            ISecteurPDLService secteurService,
            IProgrammePDLService programmeService,
            IODDService oddService,
            ICompetenceCollectiviteService competenceService,
            IBeneficiairePDLService beneficiaireService,
            IActeurPDLService acteurService,
            IStructureExecutionPDLService structureService)
        {
            _activiteService = activiteService;
            _pdlService = pdlService;
            _secteurService = secteurService;
            _programmeService = programmeService;
            _oddService = oddService;
            _competenceService = competenceService;
            _beneficiaireService = beneficiaireService;
            _acteurService = acteurService;
            _structureService = structureService;
        }

        #endregion

        #region Commandes d'initialisation

        [RelayCommand]
        public async Task InitialiserAsync()
        {
            try
            {
                await ChargerReferentielAsync();
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur d'initialisation : {ex.Message}";
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task ChargerReferentielAsync()
        {
            try
            {
                IsLoading = true;

                // Charger toutes les données de référence en parallèle
                var pdlTask = _pdlService.GetAllAsync();
                var programmesTask = _programmeService.GetAllAsync();
                var secteursTask = _secteurService.GetAllAsync();
                var oddTask = _oddService.GetAllAsync();
                var competencesTask = _competenceService.GetAllAsync();
                var beneficiairesTask = _beneficiaireService.GetAllAsync();
                var acteursTask = _acteurService.GetAllAsync();
                var structuresTask = _structureService.GetAllAsync();

                await Task.WhenAll(
                    pdlTask, programmesTask, secteursTask, oddTask,
                    competencesTask, beneficiairesTask, acteursTask, structuresTask);

                PdlList = new ObservableCollection<PDL>(await pdlTask);
                Programmes = new ObservableCollection<ProgrammePDL>(await programmesTask);
                Secteurs = new ObservableCollection<SecteurPDL>(await secteursTask);
                SecteursFiltres = new ObservableCollection<SecteurPDL>(Secteurs);
                OddList = new ObservableCollection<ODD>(await oddTask);
                Competences = new ObservableCollection<CompetenceCollectivite>(await competencesTask);
                Beneficiaires = new ObservableCollection<BeneficiairePDL>(await beneficiairesTask);
                Acteurs = new ObservableCollection<ActeurPDL>(await acteursTask);
                Structures = new ObservableCollection<StructureExecutionPDL>(await structuresTask);
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur de chargement référentiel : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                List<ActivitePDL> activites;

                if (FiltrePDL != null)
                {
                    activites = await _activiteService.GetByPDLIdAsync(FiltrePDL.Id);
                }
                else if (FiltreSecteur != null)
                {
                    activites = await _activiteService.GetBySecteurIdAsync(FiltreSecteur.Id);
                }
                else
                {
                    activites = await _activiteService.GetAllAsync();
                }

                Activites = new ObservableCollection<ActivitePDL>(activites);
                OnPropertyChanged(nameof(TotalActivites));
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

        #endregion

        #region Commandes CRUD

        [RelayCommand]
        public void NouvelleActivite()
        {
            SelectedActivite = new ActivitePDL();

            // Réinitialiser le formulaire
            Description = string.Empty;
            Resultat = string.Empty;
            DateDebut = DateTime.Today;
            DateFin = DateTime.Today.AddMonths(6);
            FinancementInterne = string.Empty;
            FinancementExterne = string.Empty;

            SelectedPDL = PdlList.FirstOrDefault();
            SelectedProgramme = null;
            SelectedSecteur = null;
            SelectedODD = null;
            SelectedCompetence = null;

            SelectedBeneficiaires.Clear();
            SelectedActeurs.Clear();
            SelectedStructures.Clear();

            IsEditing = true;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public async Task ModifierActiviteAsync(ActivitePDL? activite)
        {
            if (activite == null) return;

            try
            {
                IsLoading = true;

                // Charger l'activité avec tous ses détails
                SelectedActivite = await _activiteService.GetByIdWithDetailsAsync(activite.Id);

                if (SelectedActivite == null) return;

                // Remplir le formulaire
                Description = SelectedActivite.Description;
                Resultat = SelectedActivite.Resultat ?? string.Empty;
                DateDebut = SelectedActivite.DateDebut;
                DateFin = SelectedActivite.DateFin;
                FinancementInterne = SelectedActivite.FinancementInterne;
                FinancementExterne = SelectedActivite.FinancementExterne;

                SelectedPDL = PdlList.FirstOrDefault(p => p.Id == SelectedActivite.PDLId);
                SelectedSecteur = Secteurs.FirstOrDefault(s => s.Id == SelectedActivite.SecteurPDLId);
                SelectedProgramme = Programmes.FirstOrDefault(p => p.Id == SelectedSecteur?.ProgrammePDLId);
                SelectedODD = OddList.FirstOrDefault(o => o.Id == SelectedActivite.ODDId);
                SelectedCompetence = Competences.FirstOrDefault(c => c.Id == SelectedActivite.CompetenceCollectiviteId);

                // Charger les relations N-N
                SelectedBeneficiaires = new ObservableCollection<BeneficiairePDL>(
                    SelectedActivite.Beneficiaires ?? new List<BeneficiairePDL>());
                SelectedActeurs = new ObservableCollection<ActeurPDL>(
                    SelectedActivite.Acteurs ?? new List<ActeurPDL>());
                SelectedStructures = new ObservableCollection<StructureExecutionPDL>(
                    SelectedActivite.StructureExecutions ?? new List<StructureExecutionPDL>());

                IsEditing = true;
                OnPropertyChanged(nameof(TitrePage));
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
        public async Task EnregistrerAsync()
        {
            try
            {
                // Validations
                if (string.IsNullOrWhiteSpace(Description))
                {
                    MessageBox.Show("La description est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedPDL == null)
                {
                    MessageBox.Show("Veuillez sélectionner un PDL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedSecteur == null)
                {
                    MessageBox.Show("Veuillez sélectionner un secteur.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedODD == null)
                {
                    MessageBox.Show("Veuillez sélectionner un ODD.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedCompetence == null)
                {
                    MessageBox.Show("Veuillez sélectionner une compétence.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DateDebut >= DateFin)
                {
                    MessageBox.Show("La date de début doit être antérieure à la date de fin.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                MessageErreur = string.Empty;

                if (SelectedActivite == null)
                {
                    SelectedActivite = new ActivitePDL();
                }

                // Mise à jour des propriétés
                SelectedActivite.Description = Description.Trim();
                SelectedActivite.Resultat = Resultat;
                SelectedActivite.DateDebut = DateDebut;
                SelectedActivite.DateFin = DateFin;
                SelectedActivite.FinancementInterne = FinancementInterne;
                SelectedActivite.FinancementExterne = FinancementExterne;
                SelectedActivite.PDLId = SelectedPDL.Id;
                SelectedActivite.SecteurPDLId = SelectedSecteur.Id;
                SelectedActivite.ODDId = SelectedODD.Id;
                SelectedActivite.CompetenceCollectiviteId = SelectedCompetence.Id;

                if (SelectedActivite.Id == 0)
                {
                    // Création
                    await _activiteService.CreateAsync(SelectedActivite);

                    // Ajouter les relations N-N
                    await _activiteService.UpdateBeneficiairesAsync(SelectedActivite.Id,
                        SelectedBeneficiaires.Select(b => b.Id).ToList());
                    await _activiteService.UpdateActeursAsync(SelectedActivite.Id,
                        SelectedActeurs.Select(a => a.Id).ToList());
                    await _activiteService.UpdateStructuresAsync(SelectedActivite.Id,
                        SelectedStructures.Select(s => s.Id).ToList());

                    MessageBox.Show("Activité créée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Modification
                    await _activiteService.UpdateAsync(SelectedActivite);

                    // Mettre à jour les relations N-N
                    await _activiteService.UpdateBeneficiairesAsync(SelectedActivite.Id,
                        SelectedBeneficiaires.Select(b => b.Id).ToList());
                    await _activiteService.UpdateActeursAsync(SelectedActivite.Id,
                        SelectedActeurs.Select(a => a.Id).ToList());
                    await _activiteService.UpdateStructuresAsync(SelectedActivite.Id,
                        SelectedStructures.Select(s => s.Id).ToList());

                    MessageBox.Show("Activité modifiée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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
            SelectedActivite = null;
            OnPropertyChanged(nameof(TitrePage));
        }

        [RelayCommand]
        public async Task SupprimerAsync(ActivitePDL? activite)
        {
            if (activite == null) return;

            var result = MessageBox.Show(
                $"Voulez-vous vraiment supprimer cette activité ?\n\n\"{activite.Description}\"\n\nCette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var success = await _activiteService.DeleteAsync(activite.Id);
                if (success)
                {
                    MessageBox.Show("Activité supprimée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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

        #region Commandes de gestion N-N

        [RelayCommand]
        public void AjouterBeneficiaire(BeneficiairePDL? beneficiaire)
        {
            if (beneficiaire == null) return;
            if (!SelectedBeneficiaires.Contains(beneficiaire))
            {
                SelectedBeneficiaires.Add(beneficiaire);
            }
        }

        [RelayCommand]
        public void RetirerBeneficiaire(BeneficiairePDL? beneficiaire)
        {
            if (beneficiaire == null) return;
            SelectedBeneficiaires.Remove(beneficiaire);
        }

        [RelayCommand]
        public void AjouterActeur(ActeurPDL? acteur)
        {
            if (acteur == null) return;
            if (!SelectedActeurs.Contains(acteur))
            {
                SelectedActeurs.Add(acteur);
            }
        }

        [RelayCommand]
        public void RetirerActeur(ActeurPDL? acteur)
        {
            if (acteur == null) return;
            SelectedActeurs.Remove(acteur);
        }

        [RelayCommand]
        public void AjouterStructure(StructureExecutionPDL? structure)
        {
            if (structure == null) return;
            if (!SelectedStructures.Contains(structure))
            {
                SelectedStructures.Add(structure);
            }
        }

        [RelayCommand]
        public void RetirerStructure(StructureExecutionPDL? structure)
        {
            if (structure == null) return;
            SelectedStructures.Remove(structure);
        }

        #endregion

        #region Commandes de filtrage

        [RelayCommand]
        public async Task FiltrerAsync()
        {
            await ChargerDonneesAsync();
        }

        [RelayCommand]
        public async Task ReinitialiserFiltresAsync()
        {
            FiltrePDL = null;
            FiltreSecteur = null;
            await ChargerDonneesAsync();
        }

        #endregion

        #region Handlers de changement

        partial void OnSelectedProgrammeChanged(ProgrammePDL? value)
        {
            // Filtrer les secteurs par programme sélectionné
            if (value != null)
            {
                SecteursFiltres = new ObservableCollection<SecteurPDL>(
                    Secteurs.Where(s => s.ProgrammePDLId == value.Id));
            }
            else
            {
                SecteursFiltres = new ObservableCollection<SecteurPDL>(Secteurs);
            }

            // Réinitialiser le secteur sélectionné si nécessaire
            if (SelectedSecteur != null && value != null && SelectedSecteur.ProgrammePDLId != value.Id)
            {
                SelectedSecteur = null;
            }
        }

        partial void OnFiltrePDLChanged(PDL? value)
        {
            _ = ChargerDonneesAsync();
        }

        partial void OnFiltreSecteurChanged(SecteurPDL? value)
        {
            _ = ChargerDonneesAsync();
        }

        #endregion
    }
}