
using Collectivite.Models;
using Collectivite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour la page Livre Journal
    /// </summary>
    public class LivreJournalViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly LivreJournalExportService _exportService;

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool _isDisposed;
        private readonly ExerciceService _exerciceService;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #region Propriétés - Données

        private ObservableCollection<EcritureComptable> _ecritures = new();
        public ObservableCollection<EcritureComptable> Ecritures
        {
            get => _ecritures;
            set
            {
                if (SetProperty(ref _ecritures, value))
                {
                    CalculerTotaux();
                }
            }
        }

        private EcritureComptable? _selectedEcriture;
        public EcritureComptable? SelectedEcriture
        {
            get => _selectedEcriture;
            set => SetProperty(ref _selectedEcriture, value);
        }

        private ObservableCollection<CompteComptable> _comptesDisponibles = new();
        public ObservableCollection<CompteComptable> ComptesDisponibles
        {
            get => _comptesDisponibles;
            set => SetProperty(ref _comptesDisponibles, value);
        }

        #endregion

        #region Propriétés - Filtres

        private bool _showFilters;
        public bool ShowFilters
        {
            get => _showFilters;
            set => SetProperty(ref _showFilters, value);
        }

        private DateTime? _dateDebutDateTime;
        public DateTime? DateDebutDateTime
        {
            get => _dateDebutDateTime;
            set => SetProperty(ref _dateDebutDateTime, value);
        }

        private DateTime? _dateFinDateTime;
        public DateTime? DateFinDateTime
        {
            get => _dateFinDateTime;
            set => SetProperty(ref _dateFinDateTime, value);
        }

        private CompteComptable? _compteFiltre;
        public CompteComptable? CompteFiltre
        {
            get => _compteFiltre;
            set => SetProperty(ref _compteFiltre, value);
        }

        #endregion

        #region Propriétés - Totaux

        private decimal _totalDebit;
        public decimal TotalDebit
        {
            get => _totalDebit;
            set => SetProperty(ref _totalDebit, value);
        }

        private decimal _totalCredit;
        public decimal TotalCredit
        {
            get => _totalCredit;
            set => SetProperty(ref _totalCredit, value);
        }

        private decimal _difference;
        public decimal Difference
        {
            get => _difference;
            set => SetProperty(ref _difference, value);
        }

        private bool _isEquilibre;
        public bool IsEquilibre
        {
            get => _isEquilibre;
            set => SetProperty(ref _isEquilibre, value);
        }

        // Permissions
        public bool CanViewEcritureComptable => SessionManager.HasPermission("EcritureComptable.View");
        public bool CanCreateEcritureComptable => SessionManager.HasPermission("EcritureComptable.Create");
        public bool CanEditEcritureComptable => SessionManager.HasPermission("EcritureComptable.Edit");
        public bool CanDeleteEcritureComptable => SessionManager.HasPermission("EcritureComptable.Delete");

        #endregion

        #region Propriétés - Dialog

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        private string _dialogTitle = "Nouvelle Écriture";
        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        private EcritureComptable _dialogEcriture = new();
        public EcritureComptable DialogEcriture
        {
            get => _dialogEcriture;
            set => SetProperty(ref _dialogEcriture, value);
        }

        #endregion

        #region Propriétés - État

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commandes

        public ICommand LoadDataCommand { get; }
        public ICommand ToggleFiltersCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand VerifierEquilibreCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand OpenAddEcritureCommand { get; }
        public ICommand OpenEditEcritureCommand { get; }
        public ICommand SaveEcritureCommand { get; }
        public ICommand CancelEcritureCommand { get; }
        public ICommand DeleteEcritureCommand { get; }

        #endregion

        public LivreJournalViewModel()
        {
            _exportService = new LivreJournalExportService();
            _exerciceService = ExerciceService.Instance;
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            // Initialiser les commandes
            LoadDataCommand = new RelayCommandAsync(LoadDataAsync);
            ToggleFiltersCommand = new RelayCommandSync(() => ShowFilters = !ShowFilters);
            ApplyFiltersCommand = new RelayCommandAsync(ApplyFiltersAsync);
            ClearFiltersCommand = new RelayCommandAsync(ClearFiltersAsync);
            VerifierEquilibreCommand = new RelayCommandSync(VerifierEquilibre);

            // Commandes d'export
            ExportCommand = new RelayCommandSync(ShowExportMenu);
            ExportExcelCommand = new RelayCommandAsync(ExportToExcelAsync);
            ExportPdfCommand = new RelayCommandAsync(ExportToPdfAsync);

            // Commandes dialog
            OpenAddEcritureCommand = new RelayCommandSync(OpenAddEcriture);
            OpenEditEcritureCommand = new RelayCommandWithParam<EcritureComptable>(OpenEditEcriture);
            SaveEcritureCommand = new RelayCommandAsync(SaveEcritureAsync);
            CancelEcritureCommand = new RelayCommandSync(() => IsDialogOpen = false);
            DeleteEcritureCommand = new RelayCommandWithParamAsync<EcritureComptable>(DeleteEcritureAsync);
        }

        #region Méthodes - Chargement
        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadDataAsync();
            });
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                using var context = new AppDbContext();

                // Charger les comptes
                var comptes = await context.CompteComptables
                    .OrderBy(c => c.NumeroCompte)
                    .ToListAsync();
                ComptesDisponibles = new ObservableCollection<CompteComptable>(comptes);

                // Charger les écritures
                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement:\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Méthodes - Filtres

        public async Task ApplyFiltersAsync()
        {
            try
            {
                IsLoading = true;

                using var context = new AppDbContext();
                var exerciceService = ExerciceService.Instance;
                var exerciceId = 0;
                if (exerciceService.CurrentExercice != null)
                {
                    exerciceId = exerciceService.CurrentExercice.Id;
                }
                var query = context.EcritureComptables
                    .Where(e => e.idExercice == exerciceId)
                    .Include(e => e.CompteDebit)
                    .Include(e => e.CompteCredit)
                    .AsQueryable();

                // Filtre par date début
                if (DateDebutDateTime.HasValue)
                {
                    var dateDebut = DateOnly.FromDateTime(DateDebutDateTime.Value);
                    query = query.Where(e => e.DateEcriture >= dateDebut);
                }

                // Filtre par date fin
                if (DateFinDateTime.HasValue)
                {
                    var dateFin = DateOnly.FromDateTime(DateFinDateTime.Value);
                    query = query.Where(e => e.DateEcriture <= dateFin);
                }

                // Filtre par compte
                if (CompteFiltre != null)
                {
                    query = query.Where(e =>
                        e.CompteDebitId == CompteFiltre.Id ||
                        e.CompteCreditId == CompteFiltre.Id);
                }

                var ecritures = await query
                    .OrderByDescending(e => e.Id)
                    .ToListAsync();

                Ecritures = new ObservableCollection<EcritureComptable>(ecritures);
                CalculerTotaux();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'application des filtres:\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ClearFiltersAsync()
        {
            DateDebutDateTime = null;
            DateFinDateTime = null;
            CompteFiltre = null;
            await ApplyFiltersAsync();
        }

        #endregion

        #region Méthodes - Calculs

        private void CalculerTotaux()
        {
            TotalDebit = Ecritures?.Sum(e => e.Montant) ?? 0;
            TotalCredit = Ecritures?.Sum(e => e.Montant) ?? 0;
            Difference = TotalDebit - TotalCredit;
            IsEquilibre = Math.Abs(Difference) < 0.01m;
        }

        private void VerifierEquilibre()
        {
            CalculerTotaux();

            string message = IsEquilibre
                ? $"✓ Le journal est équilibré.\n\nTotal Débit: {TotalDebit:N0} GNF\nTotal Crédit: {TotalCredit:N0} GNF"
                : $"✗ Le journal n'est PAS équilibré!\n\nTotal Débit: {TotalDebit:N0} GNF\nTotal Crédit: {TotalCredit:N0} GNF\nDifférence: {Difference:N0} GNF";

            MessageBox.Show(message, "Vérification de l'équilibre",
                MessageBoxButton.OK,
                IsEquilibre ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        /// <summary>
        /// Nettoyer les ressources et se désabonner des événements
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _exerciceService.ExerciceChanged -= OnExerciceChanged;
                _isDisposed = true;
            }
        }
        #endregion

        #region Méthodes - Export

        private void ShowExportMenu()
        {
            // Afficher un dialogue pour choisir le format
            var result = MessageBox.Show(
                "Choisissez le format d'export:\n\n" +
                "• Cliquez 'Oui' pour Excel (.xlsx)\n" +
                "• Cliquez 'Non' pour PDF (.pdf)\n" +
                "• Cliquez 'Annuler' pour annuler",
                "Export du Livre Journal",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    _ = ExportToExcelAsync();
                    break;
                case MessageBoxResult.No:
                    _ = ExportToPdfAsync();
                    break;
            }
        }

        public async Task ExportToExcelAsync()
        {
            if (Ecritures == null || !Ecritures.Any())
            {
                MessageBox.Show("Aucune écriture à exporter.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;

                // Dialogue de sauvegarde
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers Excel (*.xlsx)|*.xlsx",
                    FileName = $"LivreJournal_{DateTime.Now:yyyyMMdd}",
                    Title = "Enregistrer le Livre Journal en Excel"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var idCommune = Properties.Settings.Default.CommuneId;

                    // Appel DIRECT avec await (pas de Task.Run)
                    string tempPath = await _exportService.ExportToExcel(
                        idCommune,
                        Ecritures.ToList(),
                        DateDebutDateTime,
                        DateFinDateTime);

                    // Vérifier que le fichier existe
                    if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
                    {
                        throw new FileNotFoundException("Le fichier temporaire n'a pas été créé.");
                    }

                    // Copier vers la destination (opération rapide, pas besoin de Task.Run)
                    File.Copy(tempPath, saveDialog.FileName, true);

                    // Supprimer le fichier temporaire
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignorer les erreurs de suppression
                    }

                    var result = MessageBox.Show(
                        $"Export Excel réussi!\n\nFichier: {saveDialog.FileName}\n\nVoulez-vous ouvrir le fichier?",
                        "Export réussi",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        _exportService.OpenFile(saveDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export Excel:\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExportToPdfAsync()
        {
            if (Ecritures == null || !Ecritures.Any())
            {
                MessageBox.Show("Aucune écriture à exporter.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!CanEditEcritureComptable)
            {
                MessageBox.Show("Accès refusé", "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                // Dialogue de sauvegarde
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers PDF (*.pdf)|*.pdf",
                    FileName = $"LivreJournal_{DateTime.Now:yyyyMMdd}",
                    Title = "Enregistrer le Livre Journal en PDF"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var idCommune = Properties.Settings.Default.CommuneId;

                    // Appel DIRECT avec await (pas de Task.Run)
                    string tempPath = await _exportService.ExportToPdf(
                        idCommune,
                        Ecritures.ToList(),
                        DateDebutDateTime,
                        DateFinDateTime);

                    // Vérifier que le fichier existe
                    if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
                    {
                        throw new FileNotFoundException("Le fichier temporaire n'a pas été créé.");
                    }

                    // Copier vers la destination (opération rapide, pas besoin de Task.Run)
                    File.Copy(tempPath, saveDialog.FileName, true);

                    // Supprimer le fichier temporaire
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignorer les erreurs de suppression
                    }

                    var result = MessageBox.Show(
                        $"Export PDF réussi!\n\nFichier: {saveDialog.FileName}\n\nVoulez-vous ouvrir le fichier?",
                        "Export réussi",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        _exportService.OpenFile(saveDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export PDF:\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Méthodes - Dialog CRUD

        private void OpenAddEcriture()
        {
            DialogTitle = "Nouvelle Écriture";
            DialogEcriture = new EcritureComptable
            {
                DateEcriture = DateOnly.FromDateTime(DateTime.Today)
            };
            IsDialogOpen = true;
        }

        private void OpenEditEcriture(EcritureComptable? ecriture)
        {
            if (ecriture == null) return;

            DialogTitle = "Modifier l'Écriture";
            DialogEcriture = new EcritureComptable
            {
                Id = ecriture.Id,
                DateEcriture = ecriture.DateEcriture,
                CompteDebitId = ecriture.CompteDebitId,
                CompteCreditId = ecriture.CompteCreditId,
                Montant = ecriture.Montant
            };
            IsDialogOpen = true;
        }

        public async Task SaveEcritureAsync()
        {
            try
            {
                // Validation
                if (DialogEcriture.CompteDebitId <= 0 || DialogEcriture.CompteCreditId <= 0)
                {
                    MessageBox.Show("Veuillez sélectionner les comptes débit et crédit.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DialogEcriture.Montant <= 0)
                {
                    MessageBox.Show("Le montant doit être supérieur à zéro.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DialogEcriture.CompteDebitId == DialogEcriture.CompteCreditId)
                {
                    MessageBox.Show("Les comptes débit et crédit doivent être différents.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;

                using var context = new AppDbContext();

                if (DialogEcriture.Id == 0)
                {
                    // Création
                    var exerciceService = ExerciceService.Instance;
                    if (exerciceService.CurrentExercice != null)
                    {
                        DialogEcriture.idExercice = exerciceService.CurrentExercice.Id;
                    }
                    context.EcritureComptables.Add(DialogEcriture);
                }
                else
                {
                    // Modification
                    var existingEcriture = await context.EcritureComptables.FindAsync(DialogEcriture.Id);
                    if (existingEcriture != null)
                    {
                        existingEcriture.DateEcriture = DialogEcriture.DateEcriture;
                        existingEcriture.CompteDebitId = DialogEcriture.CompteDebitId;
                        existingEcriture.CompteCreditId = DialogEcriture.CompteCreditId;
                        existingEcriture.Montant = DialogEcriture.Montant;
                    }
                }

                await context.SaveChangesAsync();
                IsDialogOpen = false;
                await ApplyFiltersAsync();

                MessageBox.Show("Écriture enregistrée avec succès.", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement:\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task DeleteEcritureAsync(EcritureComptable? ecriture)
        {
            if (ecriture == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cette écriture?\n\n" +
                $"Date: {ecriture.DateEcriture:dd/MM/yyyy}\n" +
                $"Montant: {ecriture.Montant:N0} GNF",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;

                    using var context = new AppDbContext();
                    var ecritureToDelete = await context.EcritureComptables.FindAsync(ecriture.Id);
                    if (ecritureToDelete != null)
                    {
                        context.EcritureComptables.Remove(ecritureToDelete);
                        await context.SaveChangesAsync();
                    }

                    await ApplyFiltersAsync();

                    MessageBox.Show("Écriture supprimée avec succès.", "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression:\n{ex.Message}", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        #endregion
    }

    
}