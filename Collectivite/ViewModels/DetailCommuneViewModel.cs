using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.Generic;
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
            OpenDetailCommuneCommand = new RelayCommand<Commune>(commune => OpenDetailCommune(commune));

            // Assistant par étapes (wizard)
            GoToNextStepCommand = new RelayCommand(_ => GoToNextStep(), _ => !IsLastStep);
            GoToPreviousStepCommand = new RelayCommand(_ => GoToPreviousStep(), _ => !IsFirstStep);

            InitializeAsync();
        }

        #region Properties

        public ObservableCollection<DetailCommune> DetailCommunes { get; } = new ObservableCollection<DetailCommune>();
        public ObservableCollection<Commune> Communes { get; } = new ObservableCollection<Commune>();
        public ObservableCollection<Exercice> Exercices { get; } = new ObservableCollection<Exercice>();

        public bool CanViewDetailCommune => SessionManager.HasPermission("DetailCommune.View");
        public bool CanCreateDetailCommune => SessionManager.HasPermission("DetailCommune.Create");
        public bool CanEditDetailCommune => SessionManager.HasPermission("DetailCommune.Edit");
        public bool CanDeleteDetailCommune => SessionManager.HasPermission("DetailCommune.Delete");

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

        // ═══════════════════════════════════════════════════════════
        // ASSISTANT PAR ÉTAPES (WIZARD)
        // ═══════════════════════════════════════════════════════════

        public IReadOnlyList<string> StepTitles { get; } = new[]
        {
            "Démographie",
            "Administration & Personnel",
            "Éducation",
            "Santé & Infrastructures",
            "ONG & Organisations",
            "Économie & Sécurité"
        };

        private int _currentStepIndex;
        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (SetProperty(ref _currentStepIndex, value))
                {
                    OnPropertyChanged(nameof(IsFirstStep));
                    OnPropertyChanged(nameof(IsLastStep));
                    OnPropertyChanged(nameof(CurrentStepNumberLabel));
                }
            }
        }

        public bool IsFirstStep => CurrentStepIndex == 0;
        public bool IsLastStep => CurrentStepIndex == StepTitles.Count - 1;
        public string CurrentStepNumberLabel => $"Étape {CurrentStepIndex + 1} sur {StepTitles.Count} : {StepTitles[CurrentStepIndex]}";

        // ═══════════════════════════════════════════════════════════
        // CHAMPS SAISISSABLES QUI ALIMENTENT UN CALCUL AUTOMATIQUE
        // (DetailCommune n'implémente pas INotifyPropertyChanged : on
        // passe par ces propriétés du ViewModel pour recalculer et
        // notifier la vue à chaque frappe, sans bouton "Calculer")
        // ═══════════════════════════════════════════════════════════

        public int PopulationHommes
        {
            get => DialogDetailCommune.PopulationHommes;
            set
            {
                DialogDetailCommune.PopulationHommes = value;
                OnPropertyChanged();
                CalculerDensite();
            }
        }

        public int PopulationFemmes
        {
            get => DialogDetailCommune.PopulationFemmes;
            set
            {
                DialogDetailCommune.PopulationFemmes = value;
                OnPropertyChanged();
                CalculerDensite();
            }
        }

        public double Superficie
        {
            get => DialogDetailCommune.Superficie;
            set
            {
                DialogDetailCommune.Superficie = value;
                OnPropertyChanged();
                CalculerDensite();
            }
        }

        public int NombreEcolesPrescolaire
        {
            get => DialogDetailCommune.NombreEcolesPrescolaire;
            set
            {
                DialogDetailCommune.NombreEcolesPrescolaire = value;
                OnPropertyChanged();
                CalculerTotalEcoles();
            }
        }

        public int NombreEcolesPrimaire
        {
            get => DialogDetailCommune.NombreEcolesPrimaire;
            set
            {
                DialogDetailCommune.NombreEcolesPrimaire = value;
                OnPropertyChanged();
                CalculerTotalEcoles();
            }
        }

        public int NombreEcolesCollege
        {
            get => DialogDetailCommune.NombreEcolesCollege;
            set
            {
                DialogDetailCommune.NombreEcolesCollege = value;
                OnPropertyChanged();
                CalculerTotalEcoles();
            }
        }

        public int NombreEcolesLycee
        {
            get => DialogDetailCommune.NombreEcolesLycee;
            set
            {
                DialogDetailCommune.NombreEcolesLycee = value;
                OnPropertyChanged();
                CalculerTotalEcoles();
            }
        }

        /// <summary>Population totale, recalculée automatiquement (lecture seule).</summary>
        public int PopulationTotale => DialogDetailCommune.PopulationTotale;

        /// <summary>Densité (hab/km²), recalculée automatiquement (lecture seule).</summary>
        public double Densite => DialogDetailCommune.Densite;

        /// <summary>Nombre total d'écoles, recalculé automatiquement (lecture seule).</summary>
        public int NombreEcoles => DialogDetailCommune.NombreEcoles;

        #endregion

        #region Commands

        public ICommand LoadDetailCommuneCommand { get; }
        public ICommand OpenAddDetailCommuneCommand { get; }
        public ICommand OpenEditDetailCommuneCommand { get; }
        public ICommand SaveDetailCommuneCommand { get; }
        public ICommand CancelDetailCommuneCommand { get; }
        public ICommand DeleteDetailCommuneCommand { get; }
        public ICommand OpenDetailCommuneCommand { get; }
        public ICommand GoToNextStepCommand { get; }
        public ICommand GoToPreviousStepCommand { get; }

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
                NotificationService.ShowError($"Erreur lors de l'initialisation : {ex.Message}");
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
                NotificationService.ShowError($"Erreur lors du chargement : {ex.Message}");
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
                NotificationService.ShowError($"Erreur lors du chargement des communes : {ex.Message}");
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
                NotificationService.ShowError($"Erreur lors du chargement des exercices : {ex.Message}");
            }
        }

        private void OpenAddDetailCommune()
        {
            if (!CanCreateDetailCommune)
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

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

            CurrentStepIndex = 0;
            NotifyDialogFieldsChanged();
            IsDialogOpen = true;
        }

        private void OpenEditDetailCommune(DetailCommune? detail)
        {
            if (detail == null) return;

            if (!CanEditDetailCommune)
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

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

            CurrentStepIndex = 0;
            NotifyDialogFieldsChanged();
            IsDialogOpen = true;
        }

        /// <summary>
        /// Notifie la vue que tous les champs saisissables/calculés doivent être relus
        /// (nécessaire car DialogDetailCommune est remplacé par une nouvelle instance
        /// à chaque ouverture du dialog, et DetailCommune n'implémente pas
        /// INotifyPropertyChanged).
        /// </summary>
        private void NotifyDialogFieldsChanged()
        {
            OnPropertyChanged(nameof(PopulationHommes));
            OnPropertyChanged(nameof(PopulationFemmes));
            OnPropertyChanged(nameof(Superficie));
            OnPropertyChanged(nameof(NombreEcolesPrescolaire));
            OnPropertyChanged(nameof(NombreEcolesPrimaire));
            OnPropertyChanged(nameof(NombreEcolesCollege));
            OnPropertyChanged(nameof(NombreEcolesLycee));
            OnPropertyChanged(nameof(PopulationTotale));
            OnPropertyChanged(nameof(Densite));
            OnPropertyChanged(nameof(NombreEcoles));
        }

        private void GoToNextStep()
        {
            if (!IsLastStep)
            {
                CurrentStepIndex++;
            }
        }

        private void GoToPreviousStep()
        {
            if (!IsFirstStep)
            {
                CurrentStepIndex--;
            }
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
                    NotificationService.ShowError("Impossible de naviguer vers la page de détails.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur lors de l'ouverture des détails : {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SaveDetailCommuneAsync()
        {
            if (!(IsEditMode ? CanEditDetailCommune : CanCreateDetailCommune))
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

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
                        NotificationService.ShowWarning("Veuillez sélectionner un exercice.");
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
                        NotificationService.ShowSuccess(message);
                        IsDialogOpen = false;
                        await LoadDetailCommuneAsync();
                    }
                    else
                    {
                        NotificationService.ShowWarning(message);
                    }
                }
                else
                {
                    var (success, message, _) = await _detailCommuneService.CreateAsync(DialogDetailCommune);

                    if (success)
                    {
                        NotificationService.ShowSuccess(message);
                        IsDialogOpen = false;
                        await LoadDetailCommuneAsync();
                    }
                    else
                    {
                        NotificationService.ShowWarning(message);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.ShowError($"Erreur : {ex.Message}");
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

            if (!CanDeleteDetailCommune)
            {
                NotificationService.ShowWarning("Vous n'avez pas la permission nécessaire pour cette action.");
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer les détails de la commune '{detail.Commune?.Nom}' ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _detailCommuneService.DeleteAsync(detail.Id);

                if (success)
                {
                    NotificationService.ShowSuccess(message);
                }
                else
                {
                    NotificationService.ShowWarning(message);
                }

                if (success)
                {
                    await LoadDetailCommuneAsync();
                }

                IsLoading = false;
            }
        }

        /// <summary>
        /// Recalcule automatiquement la population totale et la densité. Appelée à
        /// chaque frappe sur un champ dont dépend le calcul (population, superficie) -
        /// aucun bouton "Calculer" n'est nécessaire.
        /// </summary>
        private void CalculerDensite()
        {
            DialogDetailCommune.PopulationTotale = DialogDetailCommune.PopulationHommes + DialogDetailCommune.PopulationFemmes;
            DialogDetailCommune.Densite = DialogDetailCommune.Superficie > 0
                ? Math.Round(DialogDetailCommune.PopulationTotale / DialogDetailCommune.Superficie, 2)
                : 0;

            OnPropertyChanged(nameof(PopulationTotale));
            OnPropertyChanged(nameof(Densite));
        }

        /// <summary>
        /// Recalcule automatiquement le nombre total d'écoles à chaque frappe sur un
        /// champ "écoles par niveau" - aucun bouton "Calculer" n'est nécessaire.
        /// </summary>
        private void CalculerTotalEcoles()
        {
            DialogDetailCommune.NombreEcoles = DialogDetailCommune.NombreEcolesPrescolaire +
                                                DialogDetailCommune.NombreEcolesPrimaire +
                                                DialogDetailCommune.NombreEcolesCollege +
                                                DialogDetailCommune.NombreEcolesLycee;
            OnPropertyChanged(nameof(NombreEcoles));
        }

        #endregion
    }
}