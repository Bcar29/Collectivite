using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class MandatFormViewModel : ViewModelBase
    {
        private bool _isLoading;
        private bool _isEditMode;
        private int? _mandatId;
        private Mandat _mandat;
        private BudgetLine? _selectedBudgetLine;
        private Engagement? _selectedEngagement;
        private string? _erreurPrecomptes;

        public MandatFormViewModel(int? mandatId = null)
        {
            _mandatId = mandatId;
            _isEditMode = mandatId.HasValue;

            _mandat = new Mandat
            {
                DateEmission = DateTime.Now,
                Mois = (TypeMois)DateTime.Now.Month - 1,
                MontantBrut = 0,
                Rts = 0,
                AutresPrecomptes = 0,
                MontantNet = 0,
                NumeroMandat = ""
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            CalculerMontantNetCommand = new RelayCommand(_ => CalculerMontantNet());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            ConvertMontantToLettresCommand = new RelayCommand(_ => ConvertMontantToLettres());
            ValiderPrecomptesCommand = new RelayCommand(_ => ValiderPrecomptes());

            // Charger les données
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Engagement> Engagements { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public Mandat Mandat
        {
            get => _mandat;
            set => SetProperty(ref _mandat, value);
        }

        /// <summary>
        /// Engagement sélectionné - Charge automatiquement le montant brut
        /// </summary>
        public Engagement? SelectedEngagement
        {
            get => _selectedEngagement;
            set
            {
                if (SetProperty(ref _selectedEngagement, value))
                {
                    if (value != null)
                    {
                        // Mettre à jour l'ID de l'engagement dans le mandat
                        Mandat.EngagementId = value.Id;

                        // Charger automatiquement le montant brut depuis l'engagement
                        Mandat.MontantBrut = value.MontantEngagement;

                        // Réinitialiser les précomptes si c'est une nouvelle sélection
                        if (!IsEditMode)
                        {
                            Mandat.Rts = 0;
                            Mandat.AutresPrecomptes = 0;
                        }

                        // Calculer le montant net
                        CalculerMontantNet();

                        // Charger le BudgetLine
                        _ = LoadBudgetLineAsync(value.Id);

                        // Notifier les changements
                        OnPropertyChanged(nameof(Mandat));
                        OnPropertyChanged(nameof(MontantBrutFormate));
                    }
                    else
                    {
                        Mandat.EngagementId = 0;
                        Mandat.MontantBrut = 0;
                        SelectedBudgetLine = null;
                        CalculerMontantNet();
                        OnPropertyChanged(nameof(Mandat));
                        OnPropertyChanged(nameof(MontantBrutFormate));
                    }
                }
            }
        }

        public BudgetLine? SelectedBudgetLine
        {
            get => _selectedBudgetLine;
            set
            {
                if (SetProperty(ref _selectedBudgetLine, value))
                {
                    OnPropertyChanged(nameof(MontantDisponible));
                    OnPropertyChanged(nameof(NomenclatureCode));
                }
            }
        }

        /// <summary>
        /// Message d'erreur pour les précomptes
        /// </summary>
        public string? ErreurPrecomptes
        {
            get => _erreurPrecomptes;
            set => SetProperty(ref _erreurPrecomptes, value);
        }

        /// <summary>
        /// Indique si les précomptes sont valides
        /// </summary>
        public bool PrecomptesValides => string.IsNullOrEmpty(ErreurPrecomptes);

        /// <summary>
        /// Indique si les précomptes dépassent le montant brut
        /// </summary>
        public bool HasErreurPrecomptes => !string.IsNullOrEmpty(ErreurPrecomptes);

        public string PageTitle => IsEditMode ? "Modifier le mandat" : "Nouveau mandat";

        // Liste des mois
        public Array MoisList => Enum.GetValues(typeof(TypeMois));

        // Propriétés calculées pour affichage
        public decimal MontantDisponible => SelectedBudgetLine?.MontantDefinitif ?? 0;
        public string NomenclatureCode => SelectedBudgetLine?.Nommenclature?.CodeNomenclature ?? "";

        /// <summary>
        /// Montant brut formaté pour affichage (lecture seule)
        /// </summary>
        public string MontantBrutFormate => $"{Mandat.MontantBrut:N0} GNF";

        /// <summary>
        /// Total des précomptes (RTS + Autres)
        /// </summary>
        public decimal TotalPrecomptes => Mandat.Rts + Mandat.AutresPrecomptes;

        /// <summary>
        /// Pourcentage des précomptes par rapport au montant brut
        /// </summary>
        public decimal PourcentagePrecomptes => Mandat.MontantBrut > 0
            ? Math.Round((TotalPrecomptes / Mandat.MontantBrut) * 100, 2)
            : 0;

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand CalculerMontantNetCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ConvertMontantToLettresCommand { get; }
        public ICommand ValiderPrecomptesCommand { get; }

        #endregion

        #region Methods

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les engagements
                var engagementService = new EngagementService();
                var engagements = await engagementService.GetEngagementsForMandatAsync();

                Engagements.Clear();
                foreach (var e in engagements)
                {
                    Engagements.Add(e);
                }

                // Si mode édition, charger le mandat
                if (IsEditMode && _mandatId.HasValue)
                {
                    var mandatService = new MandatService();
                    var existingMandat = await mandatService.GetMandatByIdAsync(_mandatId.Value);

                    if (existingMandat != null)
                    {
                        Mandat = new Mandat
                        {
                            Id = existingMandat.Id,
                            NumeroMandat = existingMandat.NumeroMandat,
                            Bordereau = existingMandat.Bordereau,
                            Mois = existingMandat.Mois,
                            EngagementId = existingMandat.EngagementId,
                            MontantBrut = existingMandat.MontantBrut,
                            Rts = existingMandat.Rts,
                            AutresPrecomptes = existingMandat.AutresPrecomptes,
                            MontantNet = existingMandat.MontantNet,
                            MontantLettre = existingMandat.MontantLettre,
                            DateEmission = existingMandat.DateEmission,
                            Objet = existingMandat.Objet,
                            DatePaiement = existingMandat.DatePaiement
                        };

                        // Sélectionner l'engagement correspondant sans déclencher le rechargement du montant
                        _selectedEngagement = Engagements.FirstOrDefault(e => e.Id == existingMandat.EngagementId);

                        // Si l'engagement n'est pas dans la liste (car déjà utilisé), le recharger
                        if (_selectedEngagement == null && existingMandat.Engagement != null)
                        {
                            _selectedEngagement = existingMandat.Engagement;
                            Engagements.Add(_selectedEngagement);
                        }

                        OnPropertyChanged(nameof(SelectedEngagement));
                        OnPropertyChanged(nameof(MontantBrutFormate));

                        // Charger le BudgetLine
                        await LoadBudgetLineAsync(existingMandat.EngagementId);
                    }
                    else
                    {
                        MessageBox.Show("Mandat introuvable.", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Cancel();
                    }
                }
                else
                {
                    // Mode création : générer le numéro automatiquement
                    var mandatService = new MandatService();
                    var nextNumero = await mandatService.GenerateNextNumeroAsync();
                    Mandat.NumeroMandat = nextNumero;
                    OnPropertyChanged(nameof(Mandat));
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

        /// <summary>
        /// Charge le BudgetLine pour l'engagement sélectionné
        /// </summary>
        private async Task LoadBudgetLineAsync(int engagementId)
        {
            if (engagementId <= 0) return;

            try
            {
                var mandatService = new MandatService();
                var budgetLine = await mandatService.GetBudgetLineByEngagementIdAsync(engagementId);
                SelectedBudgetLine = budgetLine;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement BudgetLine: {ex.Message}");
            }
        }

        /// <summary>
        /// Valide que les précomptes ne dépassent pas le montant brut
        /// </summary>
        private void ValiderPrecomptes()
        {
            decimal totalPrecomptes = Mandat.Rts + Mandat.AutresPrecomptes;

            if (totalPrecomptes > Mandat.MontantBrut)
            {
                ErreurPrecomptes = $"⚠️ Le total des précomptes ({totalPrecomptes:N0} GNF) ne peut pas dépasser le montant brut ({Mandat.MontantBrut:N0} GNF)";

                // Réajuster les valeurs si nécessaire
                // On diminue proportionnellement
                if (Mandat.MontantBrut > 0)
                {
                    decimal ratio = Mandat.MontantBrut / totalPrecomptes;
                    Mandat.Rts = Math.Floor(Mandat.Rts * ratio);
                    Mandat.AutresPrecomptes = Math.Floor(Mandat.AutresPrecomptes * ratio);
                    OnPropertyChanged(nameof(Mandat));
                }
            }
            else if (totalPrecomptes < 0)
            {
                ErreurPrecomptes = "⚠️ Les précomptes ne peuvent pas être négatifs";

                // Réinitialiser les valeurs négatives
                if (Mandat.Rts < 0) Mandat.Rts = 0;
                if (Mandat.AutresPrecomptes < 0) Mandat.AutresPrecomptes = 0;
                OnPropertyChanged(nameof(Mandat));
            }
            else
            {
                ErreurPrecomptes = null;
            }

            OnPropertyChanged(nameof(TotalPrecomptes));
            OnPropertyChanged(nameof(PourcentagePrecomptes));
            OnPropertyChanged(nameof(PrecomptesValides));
            OnPropertyChanged(nameof(HasErreurPrecomptes));

            // Recalculer le montant net
            CalculerMontantNet();
        }

        /// <summary>
        /// Calcule le montant net après déduction des précomptes
        /// </summary>
        private void CalculerMontantNet()
        {
            // Valider d'abord les précomptes
            decimal totalPrecomptes = Mandat.Rts + Mandat.AutresPrecomptes;

            if (totalPrecomptes > Mandat.MontantBrut)
            {
                ErreurPrecomptes = $"⚠️ Total précomptes ({totalPrecomptes:N0} GNF) > Montant brut ({Mandat.MontantBrut:N0} GNF)";
                Mandat.MontantNet = 0;
            }
            else if (Mandat.Rts < 0 || Mandat.AutresPrecomptes < 0)
            {
                ErreurPrecomptes = "⚠️ Les précomptes ne peuvent pas être négatifs";
                Mandat.MontantNet = Mandat.MontantBrut;
            }
            else
            {
                ErreurPrecomptes = null;
                Mandat.MontantNet = Mandat.MontantBrut - Mandat.Rts - Mandat.AutresPrecomptes;
            }

            OnPropertyChanged(nameof(Mandat));
            OnPropertyChanged(nameof(TotalPrecomptes));
            OnPropertyChanged(nameof(PourcentagePrecomptes));
            OnPropertyChanged(nameof(PrecomptesValides));
            OnPropertyChanged(nameof(HasErreurPrecomptes));
        }

        /// <summary>
        /// Vérifie si le formulaire peut être sauvegardé
        /// </summary>
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Mandat.NumeroMandat) &&
                   Mandat.EngagementId > 0 &&
                   Mandat.MontantBrut > 0 &&
                   Mandat.MontantNet > 0 &&
                   !string.IsNullOrWhiteSpace(Mandat.MontantLettre) &&
                   !string.IsNullOrWhiteSpace(Mandat.Objet) &&
                   PrecomptesValides;
        }

        private async Task SaveAsync()
        {
            // Validation finale des précomptes avant sauvegarde
            if (!PrecomptesValides)
            {
                MessageBox.Show(ErreurPrecomptes ?? "Les précomptes sont invalides.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Mandat.MontantNet <= 0)
            {
                MessageBox.Show("Le montant net doit être supérieur à zéro.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var mandatService = new MandatService();

                if (IsEditMode)
                {
                    var (success, message) = await mandatService.UpdateMandatAsync(Mandat);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        Cancel();
                    }
                }
                else
                {
                    var (success, message, mandat) = await mandatService.CreateMandatAsync(Mandat);

                    // Si Mandat est créé avec succès, générer l'écriture comptable
                    if (success && mandat != null)
                    {
                        var currentEngagement = SelectedEngagement ?? Engagements
                            .FirstOrDefault(e => e.Id == Mandat.EngagementId);

                        if (currentEngagement != null)
                        {
                            var budgetLine = currentEngagement.BudgetLine;

                            if (budgetLine?.Nommenclature != null)
                            {
                                var (ecritureSuccess, ecritureMessage, ecriture) =
                                    await EcritureComptableHelper.GenererEcritureComptableAsync(
                                        budgetLine,
                                        null,
                                        mandat
                                    );

                                if (ecritureSuccess)
                                {
                                    message += "\n\n" + ecritureMessage;
                                }
                                else
                                {
                                    message += "\n\n⚠️ " + ecritureMessage;
                                }
                            }
                        }
                    }

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        Cancel();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            NavigationService.Instance.NavigateTo(new Views.Pages.MandatListPage());
        }

        private void ConvertMontantToLettres()
        {
            if (Mandat.MontantNet > 0)
            {
                Mandat.MontantLettre = Convertir.ConvertirNombreEnLettres((long)Mandat.MontantNet);
                OnPropertyChanged(nameof(Mandat));
            }
            else
            {
                MessageBox.Show("Le montant net doit être supérieur à zéro pour être converti en lettres.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
    }
}