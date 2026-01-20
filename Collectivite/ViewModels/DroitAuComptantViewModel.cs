
using Collectivite.Models;
using Collectivite.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion des Droits au Comptant
    /// </summary>
    public class DroitAuComptantViewModel : INotifyPropertyChanged
    {
        private readonly DroitAuComptantService _service;

        public event PropertyChangedEventHandler? PropertyChanged;

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

        private void RafraichirEtatEnregistrer()
        {
            OnPropertyChanged(nameof(CanEnregistrerOperation));
            CommandManager.InvalidateRequerySuggested();
        }

        #region Propriétés - Liste principale

        private ObservableCollection<DroitAuComptantDTO> _droitsAuComptant = new();
        public ObservableCollection<DroitAuComptantDTO> DroitsAuComptant
        {
            get => _droitsAuComptant;
            set
            {
                if (SetProperty(ref _droitsAuComptant, value))
                {
                    AppliquerFiltres();
                    OnPropertyChanged(nameof(TotalOperations));
                    OnPropertyChanged(nameof(TotalMontant));
                    OnPropertyChanged(nameof(TotalMontantFormate));
                }
            }
        }

        private ObservableCollection<DroitAuComptantDTO> _droitsAuComptantFiltres = new();
        public ObservableCollection<DroitAuComptantDTO> DroitsAuComptantFiltres
        {
            get => _droitsAuComptantFiltres;
            set
            {
                if (SetProperty(ref _droitsAuComptantFiltres, value))
                {
                    OnPropertyChanged(nameof(ResultatsFiltresCount));
                }
            }
        }

        private DroitAuComptantDTO? _selectedDroit;
        public DroitAuComptantDTO? SelectedDroit
        {
            get => _selectedDroit;
            set => SetProperty(ref _selectedDroit, value);
        }

        #endregion

        #region Propriétés - Filtres

        private string _filtreNumeroOrdre = string.Empty;
        public string FiltreNumeroOrdre
        {
            get => _filtreNumeroOrdre;
            set => SetProperty(ref _filtreNumeroOrdre, value);
        }

        private DateTime? _filtreDateDebut;
        public DateTime? FiltreDateDebut
        {
            get => _filtreDateDebut;
            set => SetProperty(ref _filtreDateDebut, value);
        }

        private DateTime? _filtreDateFin;
        public DateTime? FiltreDateFin
        {
            get => _filtreDateFin;
            set => SetProperty(ref _filtreDateFin, value);
        }

        private string? _filtreModeReglement;
        public string? FiltreModeReglement
        {
            get => _filtreModeReglement;
            set => SetProperty(ref _filtreModeReglement, value);
        }

        public List<string> ModesReglementFiltre { get; } = new List<string>
        {
            "Tous",
            "Espèces",
            "Virement",
            "Chèque"
        };

        #endregion

        #region Propriétés - ComboBox

        private ObservableCollection<ImputationDTO> _imputations = new();
        public ObservableCollection<ImputationDTO> Imputations
        {
            get => _imputations;
            set => SetProperty(ref _imputations, value);
        }

        private ImputationDTO? _selectedImputation;
        public ImputationDTO? SelectedImputation
        {
            get => _selectedImputation;
            set
            {
                if (SetProperty(ref _selectedImputation, value))
                {
                    RafraichirEtatEnregistrer();
                }
            }
        }

        private ObservableCollection<Tiers> _tiersList = new();
        public ObservableCollection<Tiers> TiersList
        {
            get => _tiersList;
            set => SetProperty(ref _tiersList, value);
        }

        private Tiers? _selectedTiers;
        public Tiers? SelectedTiers
        {
            get => _selectedTiers;
            set => SetProperty(ref _selectedTiers, value);
        }

        #endregion

        #region Propriétés - Dialog

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        // Mode du dialog : true = création, false = modification
        private bool _isCreationMode = true;
        public bool IsCreationMode
        {
            get => _isCreationMode;
            set
            {
                if (SetProperty(ref _isCreationMode, value))
                {
                    OnPropertyChanged(nameof(DialogTitle));
                    OnPropertyChanged(nameof(DialogSubtitle));
                    OnPropertyChanged(nameof(DialogHeaderColor));
                    OnPropertyChanged(nameof(DialogButtonText));
                    OnPropertyChanged(nameof(DialogButtonIcon));
                    OnPropertyChanged(nameof(DialogButtonColor));
                    RafraichirEtatEnregistrer();
                }
            }
        }

        // ID de l'opération en cours de modification
        private int _editingOrdreRecetteId;
        private int? _editingMouvementId;

        // Propriétés dynamiques du dialog
        public string DialogTitle => IsCreationMode ? "Nouvelle opération" : "Modifier l'opération";
        public string DialogSubtitle => IsCreationMode ? "Droit au comptant virtuel" : "Modification des informations";
        public Brush DialogHeaderColor => IsCreationMode
            ? new SolidColorBrush(Color.FromRgb(25, 118, 210))   // Bleu #1976D2
            : new SolidColorBrush(Color.FromRgb(255, 152, 0));   // Orange #FF9800
        public string DialogButtonText => IsCreationMode ? "Enregistrer" : "Mettre à jour";
        public string DialogButtonIcon => IsCreationMode ? "ContentSave" : "Check";
        public Brush DialogButtonColor => IsCreationMode
            ? new SolidColorBrush(Color.FromRgb(76, 175, 80))    // Vert #4CAF50
            : new SolidColorBrush(Color.FromRgb(255, 152, 0));   // Orange #FF9800

        private DateTime _dialogDate = DateTime.Today;
        public DateTime DialogDate
        {
            get => _dialogDate;
            set
            {
                if (SetProperty(ref _dialogDate, value))
                {
                    RafraichirEtatEnregistrer();
                }
            }
        }

        private decimal _dialogMontant;
        public decimal DialogMontant
        {
            get => _dialogMontant;
            set
            {
                if (SetProperty(ref _dialogMontant, value))
                {
                    RafraichirEtatEnregistrer();
                }
            }
        }

        private string _dialogComptable = string.Empty;
        public string DialogComptable
        {
            get => _dialogComptable;
            set
            {
                if (SetProperty(ref _dialogComptable, value))
                {
                    RafraichirEtatEnregistrer();
                }
            }
        }

        private string _dialogMotifs = string.Empty;
        public string DialogMotifs
        {
            get => _dialogMotifs;
            set => SetProperty(ref _dialogMotifs, value);
        }

        private ModeReglement _dialogModeReglement = ModeReglement.Espece;
        public ModeReglement DialogModeReglement
        {
            get => _dialogModeReglement;
            set
            {
                if (SetProperty(ref _dialogModeReglement, value))
                {
                    OnPropertyChanged(nameof(IsVirementVisible));
                    OnPropertyChanged(nameof(IsChequeVisible));
                    RafraichirEtatEnregistrer();
                }
            }
        }

        private string? _dialogRefVirement;
        public string? DialogRefVirement
        {
            get => _dialogRefVirement;
            set
            {
                if (SetProperty(ref _dialogRefVirement, value))
                {
                    RafraichirEtatEnregistrer();
                }
            }
        }

        private string? _dialogNumBanque;
        public string? DialogNumBanque
        {
            get => _dialogNumBanque;
            set => SetProperty(ref _dialogNumBanque, value);
        }

        private string? _dialogRefCheque;
        public string? DialogRefCheque
        {
            get => _dialogRefCheque;
            set
            {
                if (SetProperty(ref _dialogRefCheque, value))
                {
                    RafraichirEtatEnregistrer();
                }
            }
        }

        public bool IsVirementVisible => DialogModeReglement == ModeReglement.Virement;
        public bool IsChequeVisible => DialogModeReglement == ModeReglement.Cheque;

        public bool CanEnregistrerOperation
        {
            get
            {
                if (DialogMontant <= 0) return false;
                if (string.IsNullOrWhiteSpace(DialogComptable)) return false;
                if (IsCreationMode && SelectedImputation == null) return false;
                if (DialogModeReglement == ModeReglement.Virement && string.IsNullOrWhiteSpace(DialogRefVirement))
                    return false;
                if (DialogModeReglement == ModeReglement.Cheque && string.IsNullOrWhiteSpace(DialogRefCheque))
                    return false;
                return true;
            }
        }

        #endregion

        #region Propriétés - État

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _messageErreur = string.Empty;
        public string MessageErreur
        {
            get => _messageErreur;
            set => SetProperty(ref _messageErreur, value);
        }

        #endregion

        #region Propriétés - Statistiques

        public int TotalOperations => DroitsAuComptant?.Count ?? 0;
        public int ResultatsFiltresCount => DroitsAuComptantFiltres?.Count ?? 0;

        public decimal TotalMontant
        {
            get
            {
                decimal total = 0;
                if (DroitsAuComptant != null)
                {
                    foreach (var d in DroitsAuComptant)
                    {
                        total += d.MontantEncaisse;
                    }
                }
                return total;
            }
        }

        public string TotalMontantFormate => TotalMontant.ToString("N0") + " GNF";

        #endregion

        #region Commandes

        public ICommand ChargerDonneesCommand { get; }
        public ICommand OuvrirDialogCommand { get; }
        public ICommand FermerDialogCommand { get; }
        public ICommand EnregistrerOperationCommand { get; }

        // Commandes de filtrage
        public ICommand AppliquerFiltresCommand { get; }
        public ICommand ReinitialiserFiltresCommand { get; }

        // Commandes d'actions
        public ICommand VoirDetailsCommand { get; }
        public ICommand ModifierCommand { get; }
        public ICommand SupprimerCommand { get; }

        #endregion

        public DroitAuComptantViewModel()
        {
            _service = new DroitAuComptantService();

            // Initialiser les commandes
            ChargerDonneesCommand = new RelayCommandAsync(ChargerDonneesAsync);
            OuvrirDialogCommand = new RelayCommandSync(OuvrirDialogCreation);
            FermerDialogCommand = new RelayCommandSync(FermerDialog);
            EnregistrerOperationCommand = new RelayCommandAsync(EnregistrerOperationAsync, () => CanEnregistrerOperation);

            // Commandes de filtrage
            AppliquerFiltresCommand = new RelayCommandSync(AppliquerFiltres);
            ReinitialiserFiltresCommand = new RelayCommandSync(ReinitialiserFiltres);

            // Commandes d'actions
            VoirDetailsCommand = new RelayCommandWithParam<DroitAuComptantDTO>(VoirDetails);
            ModifierCommand = new RelayCommandWithParam<DroitAuComptantDTO>(OuvrirDialogModification);
            SupprimerCommand = new RelayCommandWithParamAsync<DroitAuComptantDTO>(SupprimerAsync);

            // Valeur par défaut du filtre mode
            FiltreModeReglement = "Tous";
        }

        #region Méthodes - Chargement

        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                var droits = await _service.GetDroitsAuComptantAsync();
                DroitsAuComptant = new ObservableCollection<DroitAuComptantDTO>(droits);

                var imputations = await _service.GetImputationsAsync();
                Imputations = new ObservableCollection<ImputationDTO>(imputations);

                var tiers = await _service.GetTiersListAsync();
                TiersList = new ObservableCollection<Tiers>(tiers);

                AppliquerFiltres();

                OnPropertyChanged(nameof(TotalOperations));
                OnPropertyChanged(nameof(TotalMontant));
                OnPropertyChanged(nameof(TotalMontantFormate));
            }
            catch (Exception ex)
            {
                MessageErreur = ex.Message;
                MessageBox.Show($"Erreur de chargement:\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Méthodes - Filtrage

        public void AppliquerFiltres()
        {
            if (DroitsAuComptant == null)
            {
                DroitsAuComptantFiltres = new ObservableCollection<DroitAuComptantDTO>();
                return;
            }

            var resultats = DroitsAuComptant.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltreNumeroOrdre))
            {
                resultats = resultats.Where(d =>
                    d.NumeroOrdre.Contains(FiltreNumeroOrdre, StringComparison.OrdinalIgnoreCase));
            }

            if (FiltreDateDebut.HasValue)
            {
                resultats = resultats.Where(d => d.DateOrdre.ToDateTime(TimeOnly.MinValue) >= FiltreDateDebut.Value);
            }

            if (FiltreDateFin.HasValue)
            {
                resultats = resultats.Where(d => d.DateOrdre.ToDateTime(TimeOnly.MinValue) <= FiltreDateFin.Value);
            }

            if (!string.IsNullOrEmpty(FiltreModeReglement) && FiltreModeReglement != "Tous")
            {
                resultats = resultats.Where(d =>
                    d.ModeReglement.Equals(FiltreModeReglement, StringComparison.OrdinalIgnoreCase));
            }

            DroitsAuComptantFiltres = new ObservableCollection<DroitAuComptantDTO>(resultats);
            OnPropertyChanged(nameof(ResultatsFiltresCount));
        }

        public void ReinitialiserFiltres()
        {
            FiltreNumeroOrdre = string.Empty;
            FiltreDateDebut = null;
            FiltreDateFin = null;
            FiltreModeReglement = "Tous";

            AppliquerFiltres();
        }

        #endregion

        #region Méthodes - Dialog Création

        public void OuvrirDialogCreation()
        {
            IsCreationMode = true;

            // Réinitialiser les champs
            SelectedImputation = null;
            SelectedTiers = null;
            DialogDate = DateTime.Today;
            DialogMontant = 0;
            DialogComptable = string.Empty;
            DialogMotifs = string.Empty;
            DialogModeReglement = ModeReglement.Espece;
            DialogRefVirement = null;
            DialogNumBanque = null;
            DialogRefCheque = null;

            OnPropertyChanged(nameof(IsVirementVisible));
            OnPropertyChanged(nameof(IsChequeVisible));
            RafraichirEtatEnregistrer();

            IsDialogOpen = true;
        }

        #endregion

        #region Méthodes - Dialog Modification

        public void OuvrirDialogModification(DroitAuComptantDTO? droit)
        {
            if (droit == null) return;

            IsCreationMode = false;

            // Sauvegarder les IDs pour la mise à jour
            _editingOrdreRecetteId = droit.OrdreRecetteId;
            _editingMouvementId = droit.MouvementId;

            // Charger les données existantes
            DialogDate = droit.DateOrdre.ToDateTime(TimeOnly.MaxValue);
            DialogMontant = droit.MontantEncaisse;
            DialogComptable = droit.Comptable ?? string.Empty;
            DialogMotifs = droit.Motifs ?? string.Empty;

            // Trouver l'imputation correspondante
            SelectedImputation = Imputations.FirstOrDefault(i =>
                i.BudgetLineId == droit.BudgetLineId);

            // Trouver le tiers correspondant
            SelectedTiers = droit.TiersId.HasValue
                ? TiersList.FirstOrDefault(t => t.Id == droit.TiersId.Value)
                : null;

            // Définir le mode de règlement
            DialogModeReglement = droit.ModeReglement.ToLower() switch
            {
                "virement" => ModeReglement.Virement,
                "chèque" or "cheque" => ModeReglement.Cheque,
                _ => ModeReglement.Espece
            };

            // Charger les références
            DialogRefVirement = droit.RefVirement;
            DialogNumBanque = droit.NumBanqueBenef;
            DialogRefCheque = droit.RefCheque;

            OnPropertyChanged(nameof(IsVirementVisible));
            OnPropertyChanged(nameof(IsChequeVisible));
            RafraichirEtatEnregistrer();

            IsDialogOpen = true;
        }

        #endregion

        #region Méthodes - Enregistrement (Création et Modification)

        public void FermerDialog()
        {
            IsDialogOpen = false;
        }

        public async Task EnregistrerOperationAsync()
        {
            try
            {
                // Validation commune
                if (DialogMontant <= 0)
                {
                    MessageBox.Show("Le montant doit être supérieur à zéro.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(DialogComptable))
                {
                    MessageBox.Show("Le nom du comptable est obligatoire.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DialogModeReglement == ModeReglement.Virement && string.IsNullOrWhiteSpace(DialogRefVirement))
                {
                    MessageBox.Show("La référence du virement est obligatoire.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DialogModeReglement == ModeReglement.Cheque && string.IsNullOrWhiteSpace(DialogRefCheque))
                {
                    MessageBox.Show("La référence du chèque est obligatoire.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;

                bool success;
                string message;

                if (IsCreationMode)
                {
                    // Mode création
                    if (SelectedImputation == null)
                    {
                        IsLoading = false;
                        MessageBox.Show("Veuillez sélectionner une imputation.", "Validation",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var dto = new DroitAuComptantCreationDTO
                    {
                        BudgetLineId = SelectedImputation.BudgetLineId,
                        CompteComptableId = SelectedImputation.CompteComptableId,
                        DateOrdre = DateOnly.FromDateTime(DialogDate),
                        Montant = DialogMontant,
                        Comptable = DialogComptable,
                        Motifs = DialogMotifs,
                        TiersId = SelectedTiers?.Id,
                        ModeReglement = DialogModeReglement,
                        RefVirement = DialogRefVirement,
                        NumBanqueBenef = DialogNumBanque,
                        RefCheque = DialogRefCheque
                    };

                    (success, message) = await _service.CreerOperationAsync(dto);
                }
                else
                {
                    // Mode modification
                    var dto = new DroitAuComptantModificationDTO
                    {
                        OrdreRecetteId = _editingOrdreRecetteId,
                        MouvementId = _editingMouvementId,
                        DateOrdre = DateOnly.FromDateTime(DialogDate),
                        Montant = DialogMontant,
                        Comptable = DialogComptable,
                        Motifs = DialogMotifs,
                        TiersId = SelectedTiers?.Id,
                        ModeReglement = DialogModeReglement,
                        RefVirement = DialogRefVirement,
                        NumBanqueBenef = DialogNumBanque,
                        RefCheque = DialogRefCheque
                    };

                    (success, message) = await _service.ModifierOperationAsync(dto);
                }

                if (success)
                {
                    MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    IsDialogOpen = false;
                    await ChargerDonneesAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Méthodes - Actions

        public void VoirDetails(DroitAuComptantDTO? droit)
        {
            if (droit == null) return;

            var details = $"═══ DÉTAILS DE L'OPÉRATION ═══\n\n" +
                         $"N° Ordre: {droit.NumeroOrdre}\n" +
                         $"Date: {droit.DateOrdreFormatee}\n\n" +
                         $"Imputation: {droit.Imputation}\n" +
                         $"Débiteur: {droit.Debiteur}\n\n" +
                         $"Montant: {droit.MontantEncaisseFormate}\n" +
                         $"Mode de règlement: {droit.ModeReglement}\n";

            if (!string.IsNullOrEmpty(droit.RefVirement))
                details += $"Réf. virement: {droit.RefVirement}\n";
            if (!string.IsNullOrEmpty(droit.NumBanqueBenef))
                details += $"N° compte: {droit.NumBanqueBenef}\n";
            if (!string.IsNullOrEmpty(droit.RefCheque))
                details += $"Réf. chèque: {droit.RefCheque}\n";

            details += $"\nComptable: {droit.Comptable}\n";
            if (!string.IsNullOrEmpty(droit.Motifs))
                details += $"Motifs: {droit.Motifs}\n";

            details += $"\n═══════════════════════════\n" +
                      $"ID Ordre Recette: {droit.OrdreRecetteId}\n" +
                      $"ID Mouvement: {droit.MouvementId}";

            MessageBox.Show(details, "Détails de l'opération", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task SupprimerAsync(DroitAuComptantDTO? droit)
        {
            if (droit == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cette opération ?\n\n" +
                $"N° Ordre: {droit.NumeroOrdre}\n" +
                $"Montant: {droit.MontantEncaisseFormate}\n\n" +
                $"Cette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;

                    var (success, message) = await _service.SupprimerOperationAsync(droit.OrdreRecetteId, droit.MouvementId);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        await ChargerDonneesAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        #endregion
    }

    #region Classes de commandes personnalisées

    public class RelayCommandSync : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommandSync(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }

    public class RelayCommandAsync : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommandAsync(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;
            _isExecuting = true;
            try { await _execute(); }
            finally { _isExecuting = false; }
        }
    }

    public class RelayCommandWithParam<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommandWithParam(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter == null) return _canExecute?.Invoke(default) ?? true;
            return _canExecute?.Invoke((T)parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter == null ? default : (T)parameter);
        }
    }

    public class RelayCommandWithParamAsync<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommandWithParamAsync(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_isExecuting) return false;
            if (parameter == null) return _canExecute?.Invoke(default) ?? true;
            return _canExecute?.Invoke((T)parameter) ?? true;
        }

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;
            _isExecuting = true;
            try { await _execute(parameter == null ? default : (T)parameter); }
            finally { _isExecuting = false; }
        }
    }

    #endregion
}