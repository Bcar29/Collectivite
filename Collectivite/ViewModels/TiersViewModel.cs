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
    /// <summary>
    /// ViewModel pour la gestion des tiers et de leurs comptes bancaires
    /// </summary>
    public class TiersViewModel : ViewModelBase
    {
        private bool _isLoading;
        private Tiers? _selectedTiers;
        private bool _isTiersDialogOpen;
        private Tiers _dialogTiers;
        private bool _isEditMode;
        private string _searchText;
        private TiersType? _selectedTypeFilter;
        private int _selectedTabIndex;

        // Comptes bancaires
        private CompteBancaire? _selectedCompte;
        private bool _isCompteDialogOpen;
        private CompteBancaire _dialogCompte;
        private bool _isCompteEditMode;

        public TiersViewModel()
        {
            try
            {
                _dialogTiers = new Tiers { IsActif = true };
                _dialogCompte = new CompteBancaire();
                _searchText = string.Empty;

                // Commandes Tiers
                LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
                OpenAddTiersDialogCommand = new RelayCommand(_ => OpenAddTiersDialog());
                OpenEditTiersDialogCommand = new RelayCommand<Tiers>(tiers => OpenEditTiersDialog(tiers));
                SaveTiersCommand = new RelayCommand(async _ => await SaveTiersAsync(), _ => CanSaveTiers());
                CancelTiersCommand = new RelayCommand(_ => CancelTiersDialog());
                DeleteTiersCommand = new RelayCommand<Tiers>(async tiers => await DeleteTiersAsync(tiers));
                ToggleActifCommand = new RelayCommand<Tiers>(async tiers => await ToggleActifAsync(tiers));
                SearchCommand = new RelayCommand(async _ => await SearchAsync());

                // Commandes Comptes
                OpenAddCompteDialogCommand = new RelayCommand(_ => OpenAddCompteDialog());
                OpenEditCompteDialogCommand = new RelayCommand<CompteBancaire>(compte => OpenEditCompteDialog(compte));
                SaveCompteCommand = new RelayCommand(async _ => await SaveCompteAsync(), _ => CanSaveCompte());
                CancelCompteCommand = new RelayCommand(_ => CancelCompteDialog());
                DeleteCompteCommand = new RelayCommand<CompteBancaire>(async compte => await DeleteCompteAsync(compte));

                // Charger les données
                System.Threading.Tasks.Task.Run(async () => await LoadDataAsync()).Wait();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ ERREUR D'INITIALISATION :\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Properties - Tiers

        public ObservableCollection<Tiers> Tiers { get; } = new();

        // ✅ Collections filtrées par type
        public ObservableCollection<Tiers> TiersFournisseurs { get; } = new();
        public ObservableCollection<Tiers> TiersEntreprises { get; } = new();
        public ObservableCollection<Tiers> TiersRedevables { get; } = new();
        public ObservableCollection<Tiers> TiersContribuables { get; } = new();
        public ObservableCollection<Tiers> TiersAssociations { get; } = new();

        public ObservableCollection<CompteBancaire> ComptesOfSelectedTiers { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Tiers? SelectedTiers
        {
            get => _selectedTiers;
            set
            {
                if (SetProperty(ref _selectedTiers, value))
                {
                    LoadComptesOfSelectedTiers();
                }
            }
        }

        public bool IsTiersDialogOpen
        {
            get => _isTiersDialogOpen;
            set
            {
                SetProperty(ref _isTiersDialogOpen, value);
                System.Diagnostics.Debug.WriteLine($"IsTiersDialogOpen = {value}");
            }
        }

        public Tiers DialogTiers
        {
            get => _dialogTiers;
            set => SetProperty(ref _dialogTiers, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public TiersType? SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string TiersDialogTitle => IsEditMode ? "Modifier le tiers" : "Nouveau tiers";

        #endregion

        #region Properties - Comptes

        public CompteBancaire? SelectedCompte
        {
            get => _selectedCompte;
            set => SetProperty(ref _selectedCompte, value);
        }

        public bool IsCompteDialogOpen
        {
            get => _isCompteDialogOpen;
            set => SetProperty(ref _isCompteDialogOpen, value);
        }

        public CompteBancaire DialogCompte
        {
            get => _dialogCompte;
            set => SetProperty(ref _dialogCompte, value);
        }

        public bool IsCompteEditMode
        {
            get => _isCompteEditMode;
            set => SetProperty(ref _isCompteEditMode, value);
        }

        public string CompteDialogTitle => IsCompteEditMode ? "Modifier le compte" : "Nouveau compte bancaire";

        #endregion

        #region Commands

        // Commandes Tiers
        public ICommand LoadDataCommand { get; }
        public ICommand OpenAddTiersDialogCommand { get; }
        public ICommand OpenEditTiersDialogCommand { get; }
        public ICommand SaveTiersCommand { get; }
        public ICommand CancelTiersCommand { get; }
        public ICommand DeleteTiersCommand { get; }
        public ICommand ToggleActifCommand { get; }
        public ICommand SearchCommand { get; }

        // Commandes Comptes
        public ICommand OpenAddCompteDialogCommand { get; }
        public ICommand OpenEditCompteDialogCommand { get; }
        public ICommand SaveCompteCommand { get; }
        public ICommand CancelCompteCommand { get; }
        public ICommand DeleteCompteCommand { get; }

        #endregion

        #region Methods - Tiers

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Début du chargement des tiers...");

                var tiersService = new TiersService();
                var tiers = await tiersService.GetAllTiersAsync();

                System.Diagnostics.Debug.WriteLine($"✅ {tiers.Count} tiers chargés");

                Tiers.Clear();
                foreach (var t in tiers)
                {
                    Tiers.Add(t);
                }

                ApplyFilter();

                System.Diagnostics.Debug.WriteLine($"📊 Total dans ObservableCollection : {Tiers.Count}");
            }
            catch (Exception ex)
            {
                var errorMsg = $"❌ ERREUR CHARGEMENT :\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMsg += $"\n\nDétails : {ex.InnerException.Message}";
                }

                MessageBox.Show(errorMsg, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ {errorMsg}\n{ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            // Vider les collections filtrées
            TiersFournisseurs.Clear();
            TiersEntreprises.Clear();
            TiersRedevables.Clear();
            TiersContribuables.Clear();
            TiersAssociations.Clear();

            // Remplir les collections filtrées
            foreach (var tiers in Tiers)
            {
                switch (tiers.Type)
                {
                    case TiersType.Fournisseur:
                        TiersFournisseurs.Add(tiers);
                        break;
                    case TiersType.Entreprise:
                        TiersEntreprises.Add(tiers);
                        break;
                    case TiersType.Redevable:
                        TiersRedevables.Add(tiers);
                        break;
                    case TiersType.Contribuable:
                        TiersContribuables.Add(tiers);
                        break;
                    case TiersType.Association:
                        TiersAssociations.Add(tiers);
                        break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"📊 Filtre appliqué : " +
                $"Fournisseurs={TiersFournisseurs.Count}, " +
                $"Entreprises={TiersEntreprises.Count}, " +
                $"Redevables={TiersRedevables.Count}, " +
                $"Contribuables={TiersContribuables.Count}, " +
                $"Associations={TiersAssociations.Count}");
        }

        private async System.Threading.Tasks.Task SearchAsync()
        {
            IsLoading = true;

            try
            {
                var tiersService = new TiersService();
                var results = await tiersService.SearchTiersAsync(SearchText);

                Tiers.Clear();
                foreach (var t in results)
                {
                    Tiers.Add(t);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenAddTiersDialog()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔓 OpenAddTiersDialog appelé");

                IsEditMode = false;
                DialogTiers = new Tiers
                {
                    Type = TiersType.Fournisseur,
                    IsActif = true,
                    Nom = "",
                    Email = "",
                    Adresse = ""
                };

                System.Diagnostics.Debug.WriteLine($"📝 DialogTiers créé : {DialogTiers != null}");

                IsTiersDialogOpen = true;

                System.Diagnostics.Debug.WriteLine($"✅ IsTiersDialogOpen = {IsTiersDialogOpen}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dialog :\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEditTiersDialog(Tiers? tiers)
        {
            if (tiers == null) return;

            try
            {
                IsEditMode = true;
                DialogTiers = new Tiers
                {
                    Id = tiers.Id,
                    Type = tiers.Type,
                    Rccm = tiers.Rccm,
                    Nom = tiers.Nom,
                    Prenom = tiers.Prenom,
                    Adresse = tiers.Adresse,
                    Nif = tiers.Nif,
                    Email = tiers.Email,
                    IsActif = tiers.IsActif
                };
                IsTiersDialogOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveTiers()
        {
            return DialogTiers != null &&
                   !string.IsNullOrWhiteSpace(DialogTiers.Nom) &&
                   !string.IsNullOrWhiteSpace(DialogTiers.Email) &&
                   !string.IsNullOrWhiteSpace(DialogTiers.Adresse);
        }

        private async System.Threading.Tasks.Task SaveTiersAsync()
        {
            IsLoading = true;

            try
            {
                var tiersService = new TiersService();

                if (IsEditMode)
                {
                    var (success, message) = await tiersService.UpdateTiersAsync(DialogTiers);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsTiersDialogOpen = false;
                        await LoadDataAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    var (success, message, tiers) = await tiersService.CreateTiersAsync(DialogTiers);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsTiersDialogOpen = false;
                        await LoadDataAsync();
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
                MessageBox.Show($"Erreur : {ex.Message}\n\n{ex.InnerException?.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelTiersDialog()
        {
            IsTiersDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteTiersAsync(Tiers? tiers)
        {
            if (tiers == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le tiers '{tiers.Nom}' ?\n\n" +
                "⚠️ Cette action supprimera également tous les comptes bancaires associés.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var tiersService = new TiersService();
                var (success, message) = await tiersService.DeleteTiersAsync(tiers.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    await LoadDataAsync();
                }

                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task ToggleActifAsync(Tiers? tiers)
        {
            if (tiers == null) return;

            var action = tiers.IsActif ? "désactiver" : "activer";
            var result = MessageBox.Show(
                $"Voulez-vous {action} le tiers '{tiers.Nom}' ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var tiersService = new TiersService();
                var (success, message) = await tiersService.ToggleActifAsync(tiers.Id);

                if (success)
                {
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                IsLoading = false;
            }
        }

        #endregion

        #region Methods - Comptes

        private async void LoadComptesOfSelectedTiers()
        {
            ComptesOfSelectedTiers.Clear();

            if (SelectedTiers == null) return;

            try
            {
                var compteService = new CompteBancaireService();
                var comptes = await compteService.GetComptesByTiersAsync(SelectedTiers.Id);

                foreach (var compte in comptes)
                {
                    ComptesOfSelectedTiers.Add(compte);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des comptes : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAddCompteDialog()
        {
            if (SelectedTiers == null)
            {
                MessageBox.Show("Veuillez sélectionner un tiers d'abord.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsCompteEditMode = false;
            DialogCompte = new CompteBancaire
            {
                TiersId = SelectedTiers.Id,
                Pays = "Guinée",
                IBAN = "",
                Banque = ""
            };
            IsCompteDialogOpen = true;
        }

        private void OpenEditCompteDialog(CompteBancaire? compte)
        {
            if (compte == null) return;

            IsCompteEditMode = true;
            DialogCompte = new CompteBancaire
            {
                Id = compte.Id,
                TiersId = compte.TiersId,
                IBAN = compte.IBAN,
                BIC = compte.BIC,
                Banque = compte.Banque,
                Pays = compte.Pays
            };
            IsCompteDialogOpen = true;
        }

        private bool CanSaveCompte()
        {
            return DialogCompte != null &&
                   DialogCompte.TiersId > 0 &&
                   !string.IsNullOrWhiteSpace(DialogCompte.IBAN) &&
                   !string.IsNullOrWhiteSpace(DialogCompte.Banque) &&
                   !string.IsNullOrWhiteSpace(DialogCompte.Pays);
        }

        private async System.Threading.Tasks.Task SaveCompteAsync()
        {
            IsLoading = true;

            try
            {
                var compteService = new CompteBancaireService();

                if (IsCompteEditMode)
                {
                    var (success, message) = await compteService.UpdateCompteAsync(DialogCompte);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsCompteDialogOpen = false;
                        LoadComptesOfSelectedTiers();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    var (success, message, compte) = await compteService.CreateCompteAsync(DialogCompte);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsCompteDialogOpen = false;
                        LoadComptesOfSelectedTiers();
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

        private void CancelCompteDialog()
        {
            IsCompteDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteCompteAsync(CompteBancaire? compte)
        {
            if (compte == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer ce compte bancaire ?\n\n" +
                $"Banque : {compte.Banque}\n" +
                $"IBAN : {compte.IBAN}",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var compteService = new CompteBancaireService();
                var (success, message) = await compteService.DeleteCompteAsync(compte.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    LoadComptesOfSelectedTiers();
                }

                IsLoading = false;
            }
        }

        #endregion
    }
}