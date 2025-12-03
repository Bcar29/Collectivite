using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
        //private readonly AppDbContext _context;
        private readonly ExerciceService _exerciceService;
        private readonly int _budgetPrimitifId;
        private bool _isDisposed;

        public EngagementFormViewModel(int? engagementId = null)
        {
            _exerciceService = ExerciceService.Instance;
            _exerciceService.ExerciceChanged += OnExerciceChanged;
            //_budgetPrimitifId = bpId;
            _engagement = new Engagement
            {
                DateEngagement = DateTime.Now,
                //CreditsBudgetaires = SelectedBudgetLine?.MontantActu ?? 0
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
        public ObservableCollection<Contrats> Contrats { get; } = new();
        public BudgetLine? _selectedBudgetLine;

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
                        Engagement.CreditsBudgetaires = _selectedBudgetLine.MontantActu;

                        // 🔥 Lancer la méthode async
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
                //System.Diagnostics.Debug.WriteLine($"Rechargement des budgets pour l'exercice : {exercice.Libelle}");
                await LoadDataAsync();
            });
        }
        public async System.Threading.Tasks.Task LoadDataAsync()
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
                var exercice = ExerciceService.Instance;

                BudgetLines.Clear();
                
                    
                var budgetLines = await budgetLineService.GetDepenseForEngagement();
                foreach (var bl in budgetLines)
                {
                    BudgetLines.Add(bl);
                }
                


                // Charger les contrats
                //var contratService = new ContratService(_context);
                //var contrats = await contratService.GetAllContratsAsync();
                //Contrats.Clear();
                //foreach (var c in contrats)
                //{
                //    Contrats.Add(c);
                //}
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
                        ContratId = engagement.ContratId,
                        FactureId = engagement.FactureId
                    };

                    if (engagement.FichierJoin != null)
                    {
                        FichierName = "Fichier existant";
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
                   //Engagement.TiersId > 0 &&

                   !string.IsNullOrWhiteSpace(Engagement.Objet);
                   //Engagement.MontantEngagement > 0;
        }

        private async System.Threading.Tasks.Task SaveAsync()
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

            IsLoading = true;

            try
            {
                var service = new EngagementService();

                if (IsEditMode)
                {
                    var (success, message) = await service.UpdateEngagementAsync(Engagement);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Retour à la page principale
                        NavigateBack();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    var (success, message, engagement) = await service.CreateEngagementAsync(Engagement);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Retour à la page principale
                        NavigateBack();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var result = MessageBox.Show(
                "Voulez-vous vraiment annuler ? Les modifications non enregistrées seront perdues.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NavigateBack();
            }
        }

        private void NavigateBack()
        {
            // Navigation vers la page principale
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null)
                {
                    frame.GoBack();
                }
            }
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
                    // Lire le fichier
                    byte[] fileBytes = File.ReadAllBytes(openFileDialog.FileName);

                    // Vérifier la taille (max 5 MB)
                    if (fileBytes.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Le fichier est trop volumineux (max 5 MB).",
                            "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Engagement.FichierJoin = fileBytes;
                    Engagement.FichierName = Path.GetFileName(openFileDialog.FileName);

                    MessageBox.Show($"Fichier '{Engagement.FichierName}' sélectionné avec succès.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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

            // 🔔 Notifier l’UI
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