using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;


namespace Collectivite.ViewModels
{
    /// <summary>
    /// ViewModel pour le formulaire d'ajout/modification d'engagement
    /// </summary>
    public class EngagementFormViewModel : ViewModelBase
    {
        private bool _isLoading;
        private Engagement _engagement;
        private bool _isEditMode;
        private string _fichierName;
        private readonly ExerciceService _exerciceService;
        private bool _isDisposed;
        private BudgetLine? _selectedBudgetLine;
        private int commune = Properties.Settings.Default.CommuneId;
        private string _budgetLineSearchText = string.Empty;
        private ICollectionView? _filteredBudgetLines;

        public EngagementFormViewModel(int? engagementId = null)
        {
            _exerciceService = ExerciceService.Instance;
            _exerciceService.ExerciceChanged += OnExerciceChanged;

            _engagement = new Engagement
            {
                DateEngagement = DateTime.Now,
                ExerciceId = _exerciceService.CurrentExercice?.Id ?? 0,
                CommuneId = commune
            };

            _fichierName = string.Empty;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            ChooseFileCommand = new RelayCommand(_ => ChooseFile());
            CalculerDisponibleCommand = new RelayCommand(_ => CalculerDisponible());
            ConvertMontantToLettresCommand = new RelayCommand(_ => ConvertMontantToLettres());

            // Charger les données
            LoadDataCommand.Execute(null);

            // Si ID fourni, charger l'engagement
            if (engagementId.HasValue)
            {
                _isEditMode = true;
                LoadEngagementAsync(engagementId.Value);
            }
        }

        #region Properties

        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Commune> Communes { get; } = new();
        public ObservableCollection<BudgetLine> BudgetLines { get; } = new();
        public ObservableCollection<Tiers> TiersList { get; } = new();
        public ObservableCollection<Facture> Factures { get; } = new();
        public ObservableCollection<BonCommande> BonCommandes { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public BudgetLine? SelectedBudgetLine
        {
            get => _selectedBudgetLine;
            set
            {
                if (SetProperty(ref _selectedBudgetLine, value))
                {
                    if (_selectedBudgetLine != null)
                    {
                        Engagement.CreditsBudgetaires = _selectedBudgetLine.MontantDefinitif;
                        _ = LoadEngagementsForSelectedBudgetLineAsync();
                    }
                }
            }
        }

        public Engagement Engagement
        {
            get => _engagement;
            set => SetProperty(ref _engagement, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string FichierName
        {
            get => _fichierName;
            set => SetProperty(ref _fichierName, value);
        }

        public string PageTitle => IsEditMode ? "Modifier l'engagement" : "Nouvel engagement";

        public decimal DisponibleBudgetaire => Engagement.CreditsBudgetaires - Engagement.EngagementsAnterieurs;
        public string BudgetLineSearchText
        {
            get => _budgetLineSearchText;
            set
            {
                if (SetProperty(ref _budgetLineSearchText, value))
                {
                    _filteredBudgetLines?.Refresh();
                }
            }
        }
        public ICollectionView FilteredBudgetLines
        {
            get
            {
                if (_filteredBudgetLines == null)
                {
                    _filteredBudgetLines = CollectionViewSource.GetDefaultView(BudgetLines);
                    _filteredBudgetLines.Filter = FilterBudgetLines;
                }
                return _filteredBudgetLines;
            }
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ChooseFileCommand { get; }
        public ICommand CalculerDisponibleCommand { get; }
        public ICommand ConvertMontantToLettresCommand { get; }

        #endregion

        #region Methods

        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadDataAsync();
            });
        }

        public async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Charger les exercices
                var exerciceService = new ExerciceService();
                var exercices = await exerciceService.GetAllExerciceAsync();
                Exercices.Clear();
                foreach (var ex in exercices.Where(e => !e.EstCloture))
                {
                    Exercices.Add(ex);
                }

                // Charger les communes
                var communeService = new CommuneService();
                var communes = await communeService.GetAllCommuneAsync();
                Communes.Clear();
                foreach (var com in communes)
                {
                    Communes.Add(com);
                }

                // Charger les tiers
                var tiersService = new TiersService();
                var tiers = await tiersService.GetTiersActifsAsync();
                TiersList.Clear();
                foreach (var t in tiers)
                {
                    TiersList.Add(t);
                }

                // Charger les lignes budgétaires
                var budgetLineService = new BudgetLineService();
                BudgetLines.Clear();
                var budgetLines = await budgetLineService.GetDepenseForEngagement();
                foreach (var bl in budgetLines)
                {
                    if (bl.Nommenclature.Article != "662")
                    {
                        BudgetLines.Add(bl);
                    }
                }
                FilteredBudgetLines.Refresh();

                // Charger les factures
                var factureService = new FactureService();
                var factures = await factureService.GetAllFacturesAsync();
                Factures.Clear();
                foreach (var f in factures)
                {
                    Factures.Add(f);
                }

                // Charger les bons de commande
                var bonCommandeService = new BonCommandeService();
                var bonCommandes = await bonCommandeService.GetAllBonCommandesAsync();
                BonCommandes.Clear();
                foreach (var bc in bonCommandes)
                {
                    BonCommandes.Add(bc);
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

        private async void LoadEngagementAsync(int engagementId)
        {
            IsLoading = true;

            try
            {
                var service = new EngagementService();
                var engagement = await service.GetEngagementByIdAsync(engagementId);

                if (engagement != null)
                {
                    Engagement = new Engagement
                    {
                        Id = engagement.Id,
                        ExerciceId = engagement.ExerciceId,
                        CommuneId = engagement.CommuneId,
                        BudgetLineId = engagement.BudgetLineId,
                        TiersId = engagement.TiersId,
                        Objet = engagement.Objet,
                        DateEngagement = engagement.DateEngagement,
                        CreditsBudgetaires = engagement.CreditsBudgetaires,
                        EngagementsAnterieurs = engagement.EngagementsAnterieurs,
                        MontantEngagement = engagement.MontantEngagement,
                        FichierJoin = engagement.FichierJoin,
                        FichierName = engagement.FichierName,
                        ContratId = engagement.ContratId,
                        FactureId = engagement.FactureId,
                        MontantLettre = engagement.MontantLettre,
                        BonCommandeId = engagement.BonCommandeId
                    };

                    if (engagement.FichierJoin != null)
                    {
                        FichierName = engagement.FichierName ?? "Fichier existant";
                    }
                }
                else
                {
                    MessageBox.Show("Engagement introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Cancel();
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

        private bool CanSave()
        {
            return Engagement != null &&
                   Engagement.ExerciceId > 0 &&
                   Engagement.CommuneId > 0 &&
                   Engagement.BudgetLineId > 0 &&
                   !string.IsNullOrWhiteSpace(Engagement.Objet);
        }

        private async Task SaveAsync()
        {
            // Validation supplémentaire
            if (Engagement.MontantEngagement > DisponibleBudgetaire)
            {
                MessageBox.Show(
                    $"Le montant de l'engagement ({Engagement.MontantEngagement:N0} GNF) dépasse le disponible budgétaire ({DisponibleBudgetaire:N0} GNF).",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Vérifier les permissions avant d'appeler le service
            if (IsEditMode)
            {
                if (!SessionManager.HasPermission("Engagement.Edit"))
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de modifier les engagements.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                if (!SessionManager.HasPermission("Engagement.Create"))
                {
                    MessageBox.Show("Accès refusé : vous n'avez pas la permission de créer des engagements.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            IsLoading = true;

            try
            {
                var service = new EngagementService();

                if (IsEditMode)
                {
                    //Engagement.EngagementsAnterieurs -= Engagement.MontantEngagement;
                    var (success, message) = await service.UpdateEngagementAsync(Engagement);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        NavigateBack();
                    }
                }
                else
                {
                    var (success, message, engagement) = await service.CreateEngagementAsync(Engagement);

                    MessageBox.Show(message,
                        success ? "Succès" : "Erreur",
                        MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                    if (success)
                    {
                        NavigateBack();
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
            NavigateBack();
        }

        private void NavigateBack()
        {
            NavigationService.Instance.NavigateTo(new Views.Pages.EngagementPage());

        }

        private void ChooseFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Sélectionner un fichier",
                    Filter = "Tous les fichiers (*.*)|*.*|Documents PDF (*.pdf)|*.pdf|Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    byte[] fileBytes = File.ReadAllBytes(openFileDialog.FileName);

                    if (fileBytes.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Le fichier est trop volumineux (max 5 MB).",
                            "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    MessageBox.Show("Le fichier est selectionner.",
                            "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Engagement.FichierJoin = fileBytes;
                    Engagement.FichierName = Path.GetFileName(openFileDialog.FileName);
                    FichierName = Engagement.FichierName;

                    OnPropertyChanged(nameof(FichierName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sélection du fichier : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculerDisponible()
        {
            OnPropertyChanged(nameof(DisponibleBudgetaire));
        }

        private async Task LoadEngagementsForSelectedBudgetLineAsync()
        {
            using var context = EngagementService.CreateContext();

            var totalEngage = await context.Engagements
                .Where(e => e.BudgetLineId == _selectedBudgetLine!.Id)
                .SumAsync(e => (decimal?)e.MontantEngagement) ?? 0;

            Engagement.EngagementsAnterieurs = totalEngage;

            OnPropertyChanged(nameof(Engagement));
            OnPropertyChanged(nameof(DisponibleBudgetaire));
        }

        private void ConvertMontantToLettres()
        {
            if (Engagement.MontantEngagement > 0)
            {
                Engagement.MontantLettre = Convertir.ConvertirNombreEnLettres((long)Engagement.MontantEngagement);
                OnPropertyChanged(nameof(Engagement));
            }
        }
        private bool FilterBudgetLines(object obj)
        {
            if (obj is not BudgetLine bl)
                return false;

            if (string.IsNullOrWhiteSpace(BudgetLineSearchText))
                return true;

            return
                (bl.Nommenclature.CodeNomenclature?.Contains(
                    BudgetLineSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                ||
                (bl.Nommenclature.Intitule?.Contains(
                    BudgetLineSearchText, StringComparison.OrdinalIgnoreCase) ?? false);
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
    }
}