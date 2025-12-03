using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// Version corrigée avec meilleurs logs et gestion DateTime pour DatePicker
    /// </summary>
    public class LivreJournalViewModel : ViewModelBase
    {
        private readonly EcritureComptableService _ecritureService;
        private readonly CompteComptableService _compteService;

        private bool _isLoading;
        private EcritureComptable? _selectedEcriture;
        private bool _isDialogOpen;
        private EcritureComptable _dialogEcriture;
        private bool _isEditMode;

        // Filtres - Utilisation de DateTime pour compatibilité avec DatePicker
        private DateTime? _dateDebutDateTime;
        private DateTime? _dateFinDateTime;
        private CompteComptable? _compteFiltre;
        private bool _showFilters;

        // Totaux
        private decimal _totalDebit;
        private decimal _totalCredit;
        private decimal _difference;

        public LivreJournalViewModel(EcritureComptableService ecritureService, CompteComptableService compteService)
        {
            System.Diagnostics.Debug.WriteLine("🚀 Initialisation du LivreJournalViewModel");

            _ecritureService = ecritureService;
            _compteService = compteService;

            _dialogEcriture = new EcritureComptable
            {
                DateEcriture = DateOnly.FromDateTime(DateTime.Today),
                Montant = 0
            };

            // Initialiser les dates de filtre (mois en cours)
            var today = DateTime.Today;
            _dateDebutDateTime = new DateTime(today.Year, today.Month, 1);
            _dateFinDateTime = today;

            System.Diagnostics.Debug.WriteLine($"📅 Dates initialisées : {_dateDebutDateTime:dd/MM/yyyy} - {_dateFinDateTime:dd/MM/yyyy}");

            // Commandes
            LoadEcrituresCommand = new RelayCommand(async _ => await LoadEcrituresAsync());
            LoadComptesCommand = new RelayCommand(async _ => await LoadComptesAsync());
            OpenAddEcritureCommand = new RelayCommand(_ => OpenAddEcriture());
            OpenEditEcritureCommand = new RelayCommand<EcritureComptable>(ecriture => OpenEditEcriture(ecriture));
            SaveEcritureCommand = new RelayCommand(async _ => await SaveEcritureAsync(), _ => CanSaveEcriture());
            CancelEcritureCommand = new RelayCommand(_ => CancelEcriture());
            DeleteEcritureCommand = new RelayCommand<EcritureComptable>(async ecriture => await DeleteEcritureAsync(ecriture));
            ApplyFiltersCommand = new RelayCommand(async _ => await ApplyFiltersAsync());
            ClearFiltersCommand = new RelayCommand(async _ => await ClearFiltersAsync());
            ToggleFiltersCommand = new RelayCommand(_ => ShowFilters = !ShowFilters);
            ExportCommand = new RelayCommand(_ => ExportLivreJournal());
            VerifierEquilibreCommand = new RelayCommand(async _ => await VerifierEquilibreAsync());

            System.Diagnostics.Debug.WriteLine("✅ Commandes initialisées");

            // Charger les données au démarrage
            System.Diagnostics.Debug.WriteLine("🔄 Lancement de InitializeAsync...");
            _ = InitializeAsync();
        }

        #region Properties

        public ObservableCollection<EcritureComptable> Ecritures { get; } = new();
        public ObservableCollection<CompteComptable> ComptesDisponibles { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public EcritureComptable? SelectedEcriture
        {
            get => _selectedEcriture;
            set => SetProperty(ref _selectedEcriture, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public EcritureComptable DialogEcriture
        {
            get => _dialogEcriture;
            set => SetProperty(ref _dialogEcriture, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier l'Écriture" : "Nouvelle Écriture";

        // Filtres - DateTime pour DatePicker
        public DateTime? DateDebutDateTime
        {
            get => _dateDebutDateTime;
            set => SetProperty(ref _dateDebutDateTime, value);
        }

        public DateTime? DateFinDateTime
        {
            get => _dateFinDateTime;
            set => SetProperty(ref _dateFinDateTime, value);
        }

        public CompteComptable? CompteFiltre
        {
            get => _compteFiltre;
            set => SetProperty(ref _compteFiltre, value);
        }

        public bool ShowFilters
        {
            get => _showFilters;
            set => SetProperty(ref _showFilters, value);
        }

        // Totaux
        public decimal TotalDebit
        {
            get => _totalDebit;
            set => SetProperty(ref _totalDebit, value);
        }

        public decimal TotalCredit
        {
            get => _totalCredit;
            set => SetProperty(ref _totalCredit, value);
        }

        public decimal Difference
        {
            get => _difference;
            set => SetProperty(ref _difference, value);
        }

        public bool IsEquilibre => TotalCredit == TotalDebit;

        #endregion

        #region Commands

        public ICommand LoadEcrituresCommand { get; }
        public ICommand LoadComptesCommand { get; }
        public ICommand OpenAddEcritureCommand { get; }
        public ICommand OpenEditEcritureCommand { get; }
        public ICommand SaveEcritureCommand { get; }
        public ICommand CancelEcritureCommand { get; }
        public ICommand DeleteEcritureCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ToggleFiltersCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand VerifierEquilibreCommand { get; }

        #endregion

        #region Methods

        private async Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔄 InitializeAsync - Début");
            try
            {
                await LoadComptesAsync();
                await LoadEcrituresAsync();
                System.Diagnostics.Debug.WriteLine("✅ InitializeAsync - Terminé avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ InitializeAsync - Erreur : {ex.Message}");
                MessageBox.Show($"Erreur d'initialisation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Charger toutes les écritures
        public async Task LoadEcrituresAsync()
        {
            IsLoading = true;
            System.Diagnostics.Debug.WriteLine("📊 LoadEcrituresAsync - Début");

            try
            {
                List<EcritureComptable> ecritures;

                // Convertir DateTime en DateOnly pour le service
                DateOnly? dateDebut = DateDebutDateTime.HasValue
                    ? DateOnly.FromDateTime(DateDebutDateTime.Value)
                    : null;
                DateOnly? dateFin = DateFinDateTime.HasValue
                    ? DateOnly.FromDateTime(DateFinDateTime.Value)
                    : null;

                // Appliquer les filtres si définis
                if (dateDebut.HasValue && dateFin.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"📅 Filtrage par période : {dateDebut} à {dateFin}");
                    ecritures = await _ecritureService.GetEcrituresByPeriodeAsync(dateDebut.Value, dateFin.Value);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("📋 Chargement de toutes les écritures");
                    ecritures = await _ecritureService.GetEcrituresComptablesAsync();
                }

                System.Diagnostics.Debug.WriteLine($"✅ {ecritures.Count} écritures récupérées de la base");

                // Filtrer par compte si nécessaire
                if (CompteFiltre != null && CompteFiltre.Id > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 Filtrage par compte : {CompteFiltre.NumeroCompte}");
                    ecritures = ecritures.Where(e =>
                        e.CompteDebitId == CompteFiltre.Id ||
                        e.CompteCreditId == CompteFiltre.Id).ToList();
                    System.Diagnostics.Debug.WriteLine($"   → {ecritures.Count} écritures après filtrage");
                }

                Ecritures.Clear();
                System.Diagnostics.Debug.WriteLine("🔄 Ajout des écritures à la collection...");

                foreach (var ecriture in ecritures)
                {
                    Ecritures.Add(ecriture);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Collection Ecritures.Count = {Ecritures.Count}");

                CalculerTotaux();

                if (Ecritures.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Aucune écriture à afficher");
                    System.Diagnostics.Debug.WriteLine("   Raisons possibles :");
                    System.Diagnostics.Debug.WriteLine("   1. La base de données est vide");
                    System.Diagnostics.Debug.WriteLine("   2. Les filtres excluent toutes les écritures");
                    System.Diagnostics.Debug.WriteLine("   3. Problème de chargement");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERREUR dans LoadEcrituresAsync : {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack Trace : {ex.StackTrace}");

                MessageBox.Show($"Erreur lors du chargement des écritures : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("📊 LoadEcrituresAsync - Fin");
            }
        }

        // Charger les comptes disponibles
        public async Task LoadComptesAsync()
        {
            System.Diagnostics.Debug.WriteLine("💼 LoadComptesAsync - Début");
            try
            {
                var comptes = await _compteService.GetCompteComptablesAsync();
                System.Diagnostics.Debug.WriteLine($"✅ {comptes.Count} comptes récupérés");

                ComptesDisponibles.Clear();

                // Ajouter une option vide pour le filtre
                ComptesDisponibles.Add(new CompteComptable
                {
                    Id = 0,
                    NumeroCompte = "",
                    IntituleCompte = "-- Tous les comptes --"
                });

                foreach (var compte in comptes)
                {
                    ComptesDisponibles.Add(compte);
                }

                System.Diagnostics.Debug.WriteLine($"✅ ComptesDisponibles.Count = {ComptesDisponibles.Count}");

                if (comptes.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ ATTENTION : Aucun compte trouvé !");
                    System.Diagnostics.Debug.WriteLine("   → Créez d'abord des comptes dans le Plan Comptable");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur dans LoadComptesAsync : {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des comptes : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Calculer les totaux
        private void CalculerTotaux()
        {
            System.Diagnostics.Debug.WriteLine("🧮 Calcul des totaux...");
            TotalDebit = Ecritures.Sum(e => e.Montant);
            TotalCredit = Ecritures.Sum(e => e.Montant);
            Difference = TotalDebit - TotalCredit;
            OnPropertyChanged(nameof(IsEquilibre));

            System.Diagnostics.Debug.WriteLine($"   Total Débit: {TotalDebit:N2}");
            System.Diagnostics.Debug.WriteLine($"   Total Crédit: {TotalCredit:N2}");
            System.Diagnostics.Debug.WriteLine($"   Différence: {Difference:N2}");
            System.Diagnostics.Debug.WriteLine($"   Équilibré: {IsEquilibre}");
        }

        // Ouvrir le dialogue pour ajouter une écriture
        private void OpenAddEcriture()
        {
            System.Diagnostics.Debug.WriteLine("➕ Ouverture dialogue nouvelle écriture");
            IsEditMode = false;
            DialogEcriture = new EcritureComptable
            {
                DateEcriture = DateOnly.FromDateTime(DateTime.Today),
                Montant = 0,
                CompteDebitId = 0,
                CompteCreditId = 0
            };

            OnPropertyChanged(nameof(DialogEcriture));
            IsDialogOpen = true;
        }

        // Ouvrir le dialogue pour modifier une écriture
        private void OpenEditEcriture(EcritureComptable? ecriture)
        {
            if (ecriture == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ OpenEditEcriture appelé avec null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"✏️ Ouverture dialogue édition écriture ID: {ecriture.Id}");
            IsEditMode = true;
            DialogEcriture = new EcritureComptable
            {
                Id = ecriture.Id,
                DateEcriture = ecriture.DateEcriture,
                CompteDebitId = ecriture.CompteDebitId,
                CompteCreditId = ecriture.CompteCreditId,
                Montant = ecriture.Montant,
                OrdreRecetteId = ecriture.OrdreRecetteId,
                MandatId = ecriture.MandatId
            };

            OnPropertyChanged(nameof(DialogEcriture));
            IsDialogOpen = true;
        }

        // Vérifier si on peut sauvegarder
        private bool CanSaveEcriture()
        {
            return DialogEcriture.CompteDebitId > 0 &&
                   DialogEcriture.CompteCreditId > 0 &&
                   DialogEcriture.CompteDebitId != DialogEcriture.CompteCreditId &&
                   DialogEcriture.Montant > 0;
        }

        // Sauvegarder l'écriture
        private async Task SaveEcritureAsync()
        {
            System.Diagnostics.Debug.WriteLine($"💾 Sauvegarde écriture (Mode: {(IsEditMode ? "Édition" : "Création")})");
            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _ecritureService.UpdateEcritureAsync(DialogEcriture);
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Écriture mise à jour");
                        MessageBox.Show("Écriture mise à jour avec succès",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadEcrituresAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Échec mise à jour : {message}");
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var (success, message, _) = await _ecritureService.CreateEcritureAsync(DialogEcriture);
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Écriture créée");
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadEcrituresAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Échec création : {message}");
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception : {ex.Message}");
                MessageBox.Show($"Erreur lors de l'enregistrement de l'écriture : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Annuler l'édition
        private void CancelEcriture()
        {
            System.Diagnostics.Debug.WriteLine("❌ Annulation dialogue");
            IsDialogOpen = false;
        }

        // Supprimer une écriture
        private async Task DeleteEcritureAsync(EcritureComptable? ecriture)
        {
            if (ecriture == null)
                return;

            System.Diagnostics.Debug.WriteLine($"🗑️ Demande suppression écriture ID: {ecriture.Id}");

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cette écriture du {ecriture.DateEcriture:dd/MM/yyyy} ?\n" +
                $"Débit: {ecriture.CompteDebit?.NumeroCompte} - {ecriture.CompteDebit?.IntituleCompte}\n" +
                $"Crédit: {ecriture.CompteCredit?.NumeroCompte} - {ecriture.CompteCredit?.IntituleCompte}\n" +
                $"Montant: {ecriture.Montant:N2}",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _ecritureService.DeleteEcritureAsync(ecriture.Id);

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Écriture supprimée");
                    MessageBox.Show(message, "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadEcrituresAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Échec suppression : {message}");
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                IsLoading = false;
            }
        }

        // Appliquer les filtres
        private async Task ApplyFiltersAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔍 Application des filtres");
            await LoadEcrituresAsync();
        }

        // Effacer les filtres
        private async Task ClearFiltersAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Réinitialisation des filtres");
            var today = DateTime.Today;
            DateDebutDateTime = new DateTime(today.Year, today.Month, 1);
            DateFinDateTime = today;
            CompteFiltre = null;
            await LoadEcrituresAsync();
        }

        // Vérifier l'équilibre
        private async Task VerifierEquilibreAsync()
        {
            System.Diagnostics.Debug.WriteLine("⚖️ Vérification de l'équilibre");
            try
            {
                DateOnly? dateDebut = DateDebutDateTime.HasValue
                    ? DateOnly.FromDateTime(DateDebutDateTime.Value)
                    : null;
                DateOnly? dateFin = DateFinDateTime.HasValue
                    ? DateOnly.FromDateTime(DateFinDateTime.Value)
                    : null;

                var (isEquilibre, totalDebit, totalCredit) = await _ecritureService.VerifierEquilibreAsync(
                    dateDebut, dateFin);

                string message = isEquilibre
                    ? $"✓ Le journal est équilibré !\n\nTotal Débit: {totalDebit:N2}\nTotal Crédit: {totalCredit:N2}"
                    : $"✗ Le journal n'est pas équilibré !\n\nTotal Débit: {totalDebit:N2}\nTotal Crédit: {totalCredit:N2}\nDifférence: {Math.Abs(totalDebit - totalCredit):N2}";

                MessageBox.Show(message, "Vérification de l'équilibre",
                    MessageBoxButton.OK,
                    isEquilibre ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur vérification : {ex.Message}");
                MessageBox.Show($"Erreur lors de la vérification : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Exporter le livre journal
        private void ExportLivreJournal()
        {
            System.Diagnostics.Debug.WriteLine("📤 Export demandé");
            MessageBox.Show("Fonctionnalité d'export en cours de développement.\n" +
                          "Vous pourrez exporter en Excel, PDF ou CSV.",
                "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}