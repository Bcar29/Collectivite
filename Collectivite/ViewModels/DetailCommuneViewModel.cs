using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class DetailCommuneViewModel : ViewModelBase
    {
        private readonly DetailCommuneService _detailCommuneService;
        private readonly CommuneService _communeService;
        private readonly int? _communeIdFilter;

        private bool _isLoading;
        private DetailCommune? _selectedDetailCommune;
        private bool _isDialogOpen;
        private bool _isEditMode;
        private DetailCommune _dialogDetailCommune;
        private Exercice? _selectedExercice;

        public DetailCommuneViewModel(
            DetailCommuneService detailCommuneService,
            CommuneService communeService,
            int? communeId = null)
        {
            _detailCommuneService = detailCommuneService;
            _communeService = communeService;
            _communeIdFilter = communeId;

            _dialogDetailCommune = new DetailCommune
            {
                IdCommune = communeId ?? 0
            };

            // Initialisation des commandes
            LoadDetailCommuneCommand = new RelayCommand(async _ => await LoadDetailCommuneAsync());
            OpenAddDetailCommuneCommand = new RelayCommand(_ => OpenAddDetailCommune());
            OpenEditDetailCommuneCommand = new RelayCommand<DetailCommune>(detail => OpenEditDetailCommune(detail));
            SaveDetailCommuneCommand = new RelayCommand(async _ => await SaveDetailCommuneAsync(), _ => CanSaveDetailCommune());
            CancelDetailCommuneCommand = new RelayCommand(_ => CancelDetailCommune());
            DeleteDetailCommuneCommand = new RelayCommand<DetailCommune>(async detail => await DeleteDetailCommuneAsync(detail));
            CalculerDensiteCommand = new RelayCommand(_ => CalculerDensite());
            CalculerTotalEcolesCommand = new RelayCommand(_ => CalculerTotalEcoles());
            OpenDetailCommuneCommand = new RelayCommand<Commune>(commune => OpenDetailCommune(commune));

            InitializeAsync();
        }

        #region Properties

        public ObservableCollection<DetailCommune> DetailCommunes { get; } = [];
        public ObservableCollection<Commune> Communes { get; } = [];
        public ObservableCollection<Exercice> Exercices { get; } = [];

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public DetailCommune? SelectedDetailCommune
        {
            get => _selectedDetailCommune;
            set => SetProperty(ref _selectedDetailCommune, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public DetailCommune DialogDetailCommune
        {
            get => _dialogDetailCommune;
            set => SetProperty(ref _dialogDetailCommune, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier les détails de la commune" : "Ajouter les détails de la commune";

        /// <summary>
        /// Exercice sélectionné dans le formulaire (ComboBox)
        /// </summary>
        public Exercice? SelectedExercice
        {
            get => _selectedExercice;
            set
            {
                if (SetProperty(ref _selectedExercice, value) && value != null)
                {
                    DialogDetailCommune.ExerciceId = value.Id;
                    OnPropertyChanged(nameof(DialogDetailCommune));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand LoadDetailCommuneCommand { get; }
        public ICommand OpenAddDetailCommuneCommand { get; }
        public ICommand OpenEditDetailCommuneCommand { get; }
        public ICommand SaveDetailCommuneCommand { get; }
        public ICommand CancelDetailCommuneCommand { get; }
        public ICommand DeleteDetailCommuneCommand { get; }
        public ICommand CalculerDensiteCommand { get; }
        public ICommand CalculerTotalEcolesCommand { get; }
        public ICommand OpenDetailCommuneCommand { get; }

        #endregion

        #region Methods

        private async void InitializeAsync()
        {
            try
            {
                await LoadCommunesAsync();
                await LoadExercicesAsync();
                await LoadDetailCommuneAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadDetailCommuneAsync()
        {
            IsLoading = true;

            try
            {
                System.Collections.Generic.List<DetailCommune> details;

                if (_communeIdFilter.HasValue)
                {
                    details = await _detailCommuneService.GetByCommuneAsync(_communeIdFilter.Value);
                }
                else
                {
                    details = await _detailCommuneService.GetAllAsync();
                }

                DetailCommunes.Clear();

                foreach (var detail in details)
                {
                    DetailCommunes.Add(detail);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task LoadCommunesAsync()
        {
            try
            {
                var communes = await _communeService.GetAllCommuneAsync();

                Communes.Clear();

                foreach (var commune in communes)
                {
                    Communes.Add(commune);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des communes : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadExercicesAsync()
        {
            try
            {
                var exerciceService = ExerciceService.Instance;
                var exercices = await exerciceService.GetAllExerciceAsync();

                Exercices.Clear();
                foreach (var ex in exercices)
                {
                    Exercices.Add(ex);
                }

                // Valeur par défaut : CurrentExercice s'il est défini
                if (exerciceService.CurrentExercice != null)
                {
                    SelectedExercice = Exercices.FirstOrDefault(e => e.Id == exerciceService.CurrentExercice.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des exercices : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAddDetailCommune()
        {
            IsEditMode = false;
            DialogDetailCommune = new DetailCommune
            {
                IdCommune = _communeIdFilter ?? 0
            };

            // Par défaut, on met l'exercice courant si disponible
            var exerciceService = ExerciceService.Instance;
            if (exerciceService.CurrentExercice != null)
            {
                SelectedExercice = Exercices.FirstOrDefault(e => e.Id == exerciceService.CurrentExercice.Id);
            }

            IsDialogOpen = true;
        }

        private void OpenEditDetailCommune(DetailCommune? detail)
        {
            if (detail == null) return;

            IsEditMode = true;
            DialogDetailCommune = new DetailCommune
            {
                Id = detail.Id,
                IdCommune = detail.IdCommune,
                ExerciceId = detail.ExerciceId,
                NombreConseillers = detail.NombreConseillers,
                NombreDelegationSpeciale = detail.NombreDelegationSpeciale,
                EffectifTotalPersonnel = detail.EffectifTotalPersonnel,
                EffectifPermanent = detail.EffectifPermanent,
                EffectifTemporaire = detail.EffectifTemporaire,
                NombreQuartiers = detail.NombreQuartiers,
                NombreDistricts = detail.NombreDistricts,
                NombreSecteurs = detail.NombreSecteurs,
                PopulationTotale = detail.PopulationTotale,
                PopulationHommes = detail.PopulationHommes,
                PopulationFemmes = detail.PopulationFemmes,
                Superficie = detail.Superficie,
                Densite = detail.Densite,
                NombreCentresSante = detail.NombreCentresSante,
                NombrePostesSante = detail.NombrePostesSante,
                NombreSanteAmelioree = detail.NombreSanteAmelioree,
                NombreEcoles = detail.NombreEcoles,
                NombreEcolesPrescolaire = detail.NombreEcolesPrescolaire,
                NombreEcolesPrimaire = detail.NombreEcolesPrimaire,
                NombreEcolesCollege = detail.NombreEcolesCollege,
                NombreEcolesLycee = detail.NombreEcolesLycee,
                NombreClassesPrescolaire = detail.NombreClassesPrescolaire,
                NombreClassesPrimaire = detail.NombreClassesPrimaire,
                NombreClassesCollege = detail.NombreClassesCollege,
                NombreClassesLycee = detail.NombreClassesLycee,
                NombreElevesPrescolaire = detail.NombreElevesPrescolaire,
                NombreElevesPrimaire = detail.NombreElevesPrimaire,
                NombreElevesCollege = detail.NombreElevesCollege,
                NombreElevesLycee = detail.NombreElevesLycee,
                NombreForages = detail.NombreForages,
                NombrePointsEau = detail.NombrePointsEau,
                NombreAssociation = detail.NombreAssociation,
                NombreOng = detail.NombreOng,
                NombreOngNationales = detail.NombreOngNationales,
                NombreOngEtrangeres = detail.NombreOngEtrangeres,
                NombreGroupements = detail.NombreGroupements,
                NombreCooperatives = detail.NombreCooperatives,
                NombreDetenteursArmesFeu = detail.NombreDetenteursArmesFeu,
                NombreMarches = detail.NombreMarches,
                NombreMarchesJournaliers = detail.NombreMarchesJournaliers,
                NombreMarchesHebdomadaires = detail.NombreMarchesHebdomadaires
            };

            // Positionner le ComboBox sur l'exercice du détail (si présent dans la liste)
            if (detail.ExerciceId.HasValue)
            {
                SelectedExercice = Exercices.FirstOrDefault(e => e.Id == detail.ExerciceId.Value);
            }

            IsDialogOpen = true;
        }

        private bool CanSaveDetailCommune()
        {
            return DialogDetailCommune.IdCommune > 0 && DialogDetailCommune.Superficie > 0;
        }

        private static void OpenDetailCommune(Commune? commune)
        {
            if (commune == null) return;

            try
            {
                var detailPage = new Views.Pages.DetailCommunePage(commune.Id);
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (mainWindow?.MainContentFrame != null)
                {
                    mainWindow.MainContentFrame.Navigate(detailPage);
                }
                else
                {
                    MessageBox.Show("Impossible de naviguer vers la page de détails.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture des détails : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task SaveDetailCommuneAsync()
        {
            IsLoading = true;

            try
            {
                // S'assurer que l'ExerciceId est bien défini depuis le ComboBox
                if (SelectedExercice != null)
                {
                    DialogDetailCommune.ExerciceId = SelectedExercice.Id;
                }
                else if (DialogDetailCommune.ExerciceId == null)
                {
                    // Si aucun exercice n'est sélectionné, utiliser le CurrentExercice par défaut
                    var exerciceService = ExerciceService.Instance;
                    if (exerciceService.CurrentExercice != null)
                    {
                        DialogDetailCommune.ExerciceId = exerciceService.CurrentExercice.Id;
                    }
                    else
                    {
                        MessageBox.Show("Veuillez sélectionner un exercice.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        IsLoading = false;
                        return;
                    }
                }

                // Calculs automatiques
                DialogDetailCommune.PopulationTotale = DialogDetailCommune.PopulationHommes + DialogDetailCommune.PopulationFemmes;

                if (DialogDetailCommune.Superficie > 0)
                {
                    DialogDetailCommune.Densite = Math.Round(DialogDetailCommune.PopulationTotale / DialogDetailCommune.Superficie, 2);
                }

                // Calcul total des écoles
                DialogDetailCommune.NombreEcoles = DialogDetailCommune.NombreEcolesPrescolaire +
                                                    DialogDetailCommune.NombreEcolesPrimaire +
                                                    DialogDetailCommune.NombreEcolesCollege +
                                                    DialogDetailCommune.NombreEcolesLycee;

                if (IsEditMode)
                {
                    var (success, message) = await _detailCommuneService.UpdateAsync(DialogDetailCommune);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadDetailCommuneAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    var (success, message, _) = await _detailCommuneService.CreateAsync(DialogDetailCommune);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadDetailCommuneAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
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

        private void CancelDetailCommune()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteDetailCommuneAsync(DetailCommune? detail)
        {
            if (detail == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer les détails de la commune '{detail.Commune?.Nom}' ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _detailCommuneService.DeleteAsync(detail.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDetailCommuneAsync();
                }

                IsLoading = false;
            }
        }

        private void CalculerDensite()
        {
            if (DialogDetailCommune.Superficie > 0)
            {
                DialogDetailCommune.PopulationTotale = DialogDetailCommune.PopulationHommes + DialogDetailCommune.PopulationFemmes;
                DialogDetailCommune.Densite = Math.Round(DialogDetailCommune.PopulationTotale / DialogDetailCommune.Superficie, 2);
                OnPropertyChanged(nameof(DialogDetailCommune));
            }
            else
            {
                MessageBox.Show("La superficie doit être supérieure à 0 pour calculer la densité.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CalculerTotalEcoles()
        {
            DialogDetailCommune.NombreEcoles = DialogDetailCommune.NombreEcolesPrescolaire +
                                                DialogDetailCommune.NombreEcolesPrimaire +
                                                DialogDetailCommune.NombreEcolesCollege +
                                                DialogDetailCommune.NombreEcolesLycee;
            OnPropertyChanged(nameof(DialogDetailCommune));
        }

        #endregion
    }
}