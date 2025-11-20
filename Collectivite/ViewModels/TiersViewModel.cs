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
    /// ViewModel pour la gestion des tiers (Contribuables, Fournisseurs, Salariés)
    /// </summary>
    public class TiersViewModel : ViewModelBase
    {
        private bool _isLoading;
        private Tiers? _selectedTiers;
        private bool _isTiersDialogOpen;
        private Tiers _dialogTiers;
        private bool _isEditMode;
        private string _searchText;
        private int _selectedTabIndex;

        // Comptes bancaires
        private CompteBancaire? _selectedCompte;
        private bool _isCompteDialogOpen;
        private CompteBancaire _dialogCompte;
        private bool _isCompteEditMode;

        // Documents
        private DocumentTiers? _selectedDocument;
        private bool _isDocumentDialogOpen;
        private DocumentTiers _dialogDocument;

        public TiersViewModel()
        {
            try
            {
                _dialogTiers = new Tiers
                {
                    IsActif = true,
                    Type = TiersType.Contribuable,
                    Categorie = CategorieJuridique.PersonnePhysique
                };
                _dialogCompte = new CompteBancaire();
                _dialogDocument = new DocumentTiers();
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

                // Commandes Documents
                OpenAddDocumentDialogCommand = new RelayCommand(_ => OpenAddDocumentDialog());
                AddDocumentCommand = new RelayCommand<TypeDocument>(async type => await AddDocumentAsync(type));
                DeleteDocumentCommand = new RelayCommand<DocumentTiers>(async doc => await DeleteDocumentAsync(doc));
                OpenDocumentCommand = new RelayCommand<DocumentTiers>(doc => OpenDocument(doc));
                ToggleDocumentValiditeCommand = new RelayCommand<DocumentTiers>(async doc => await ToggleDocumentValiditeAsync(doc));

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

        // Collections filtrées par type
        public ObservableCollection<Tiers> TiersContribuables { get; } = new();
        public ObservableCollection<Tiers> TiersFournisseurs { get; } = new();
        public ObservableCollection<Tiers> TiersSalaries { get; } = new();

        public ObservableCollection<CompteBancaire> ComptesOfSelectedTiers { get; } = new();
        public ObservableCollection<DocumentTiers> DocumentsOfSelectedTiers { get; } = new();

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
                    LoadDocumentsOfSelectedTiers();
                }
            }
        }

        public bool IsTiersDialogOpen
        {
            get => _isTiersDialogOpen;
            set => SetProperty(ref _isTiersDialogOpen, value);
        }

        public Tiers DialogTiers
        {
            get => _dialogTiers;
            set
            {
                if (SetProperty(ref _dialogTiers, value))
                {
                    // S'abonner aux changements de propriété
                    if (_dialogTiers != null)
                    {
                        _dialogTiers.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(DialogTiers.Categorie))
                            {
                                OnPropertyChanged(nameof(IsPersonnePhysique));
                                OnPropertyChanged(nameof(IsPersonneMorale));
                            }
                        };
                    }

                    // Notifier les changements de visibilité
                    OnPropertyChanged(nameof(IsPersonnePhysique));
                    OnPropertyChanged(nameof(IsPersonneMorale));
                }
            }
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

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS POUR L'AFFICHAGE CONDITIONNEL
        // ═══════════════════════════════════════════════════════════

        public bool IsPersonnePhysique => DialogTiers?.Categorie == CategorieJuridique.PersonnePhysique;
        public bool IsPersonneMorale => DialogTiers?.Categorie == CategorieJuridique.PersonneMorale;

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

        #region Properties - Documents

        public DocumentTiers? SelectedDocument
        {
            get => _selectedDocument;
            set => SetProperty(ref _selectedDocument, value);
        }

        public bool IsDocumentDialogOpen
        {
            get => _isDocumentDialogOpen;
            set => SetProperty(ref _isDocumentDialogOpen, value);
        }

        public DocumentTiers DialogDocument
        {
            get => _dialogDocument;
            set => SetProperty(ref _dialogDocument, value);
        }

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

        // Commandes Documents
        public ICommand OpenAddDocumentDialogCommand { get; }
        public ICommand AddDocumentCommand { get; }
        public ICommand DeleteDocumentCommand { get; }
        public ICommand OpenDocumentCommand { get; }
        public ICommand ToggleDocumentValiditeCommand { get; }

        #endregion

        #region Methods - Tiers

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var tiersService = new TiersService();
                var tiers = await tiersService.GetAllTiersAsync();

                Tiers.Clear();
                foreach (var t in tiers)
                {
                    Tiers.Add(t);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors du chargement :\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            TiersContribuables.Clear();
            TiersFournisseurs.Clear();
            TiersSalaries.Clear();

            foreach (var tiers in Tiers)
            {
                switch (tiers.Type)
                {
                    case TiersType.Contribuable:
                        TiersContribuables.Add(tiers);
                        break;
                    case TiersType.Fournisseur:
                        TiersFournisseurs.Add(tiers);
                        break;
                    case TiersType.Salarie:
                        TiersSalaries.Add(tiers);
                        break;
                }
            }
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
                IsEditMode = false;
                DialogTiers = new Tiers
                {
                    Type = TiersType.Contribuable,
                    Categorie = CategorieJuridique.PersonnePhysique,
                    IsActif = true,
                    Email = "",
                    Telephone = "",
                    Adresse = ""
                };

                IsTiersDialogOpen = true;
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
                    Categorie = tiers.Categorie,
                    Email = tiers.Email,
                    Telephone = tiers.Telephone,
                    Adresse = tiers.Adresse,
                    IsActif = tiers.IsActif,

                    // Personne Physique
                    Nom = tiers.Nom,
                    Prenom = tiers.Prenom,
                    NumeroPieceIdentite = tiers.NumeroPieceIdentite,
                    TypePieceIdentite = tiers.TypePieceIdentite,

                    // Personne Morale
                    RaisonSociale = tiers.RaisonSociale,
                    Rccm = tiers.Rccm,
                    Nif = tiers.Nif,
                    NumeroTva = tiers.NumeroTva,
                    SecteurActivite = tiers.SecteurActivite
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
            if (DialogTiers == null || string.IsNullOrWhiteSpace(DialogTiers.Email))
                return false;

            // Validation conditionnelle selon la catégorie
            if (DialogTiers.Categorie == CategorieJuridique.PersonnePhysique)
            {
                return !string.IsNullOrWhiteSpace(DialogTiers.Nom) &&
                       !string.IsNullOrWhiteSpace(DialogTiers.Prenom);
            }
            else // Personne Morale
            {
                return !string.IsNullOrWhiteSpace(DialogTiers.RaisonSociale) &&
                       !string.IsNullOrWhiteSpace(DialogTiers.Rccm) &&
                       !string.IsNullOrWhiteSpace(DialogTiers.Nif);
            }
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
                $"Êtes-vous sûr de vouloir supprimer le tiers '{tiers.NomComplet}' ?\n\n" +
                "⚠️ Cette action supprimera également tous les documents et comptes bancaires associés.",
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
                $"Voulez-vous {action} le tiers '{tiers.NomComplet}' ?",
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

        #region Methods - Documents

        private async void LoadDocumentsOfSelectedTiers()
        {
            DocumentsOfSelectedTiers.Clear();

            if (SelectedTiers == null) return;

            try
            {
                var documentService = new DocumentTiersService();
                var documents = await documentService.GetDocumentsByTiersAsync(SelectedTiers.Id);

                foreach (var doc in documents)
                {
                    DocumentsOfSelectedTiers.Add(doc);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des documents : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAddDocumentDialog()
        {
            if (SelectedTiers == null)
            {
                MessageBox.Show("Veuillez sélectionner un tiers d'abord.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsDocumentDialogOpen = true;
        }

        private async System.Threading.Tasks.Task AddDocumentAsync(TypeDocument? type)
        {
            if (SelectedTiers == null || type == null) return;

            IsLoading = true;
            IsDocumentDialogOpen = false;

            try
            {
                var documentService = new DocumentTiersService();
                var (success, message, document) = await documentService.AddDocumentAsync(
                    SelectedTiers.Id,
                    type.Value);

                if (success)
                {
                    MessageBox.Show(message, "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDocumentsOfSelectedTiers();
                }
                else
                {
                    MessageBox.Show(message, "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async System.Threading.Tasks.Task DeleteDocumentAsync(DocumentTiers? document)
        {
            if (document == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer ce document ?\n\n" +
                $"Type : {document.TypeDisplay}\n" +
                $"Fichier : {document.NomFichier}",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var documentService = new DocumentTiersService();
                var (success, message) = await documentService.DeleteDocumentAsync(document.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Erreur",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (success)
                {
                    LoadDocumentsOfSelectedTiers();
                }

                IsLoading = false;
            }
        }

        private void OpenDocument(DocumentTiers? document)
        {
            if (document == null) return;

            var documentService = new DocumentTiersService();
            var (success, message) = documentService.OpenDocument(document);

            if (!success)
            {
                MessageBox.Show(message, "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async System.Threading.Tasks.Task ToggleDocumentValiditeAsync(DocumentTiers? document)
        {
            if (document == null) return;

            IsLoading = true;

            var documentService = new DocumentTiersService();
            var (success, message) = await documentService.ToggleValiditeAsync(document.Id);

            if (success)
            {
                LoadDocumentsOfSelectedTiers();
            }
            else
            {
                MessageBox.Show(message, "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            IsLoading = false;
        }

        #endregion
    }
}