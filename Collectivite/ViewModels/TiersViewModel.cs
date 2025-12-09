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
        private bool _isDocumentEditDialogOpen;
        private string? _numeroDocument;
        private DateTime? _dateExpiration;
        private DateTime? _dateEmission;
        private string? _descriptionDocument;

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
                ValidateIBANCommand = new RelayCommand(_ => ValidateIBAN());

                // Commandes Documents
                OpenAddDocumentDialogCommand = new RelayCommand(_ => OpenAddDocumentDialog());
                AddDocumentCommand = new RelayCommand<TypeDocument>(async type => await AddDocumentAsync(type));
                DeleteDocumentCommand = new RelayCommand<DocumentTiers>(async doc => await DeleteDocumentAsync(doc));
                OpenDocumentCommand = new RelayCommand<DocumentTiers>(doc => OpenDocument(doc));
                ToggleDocumentValiditeCommand = new RelayCommand<DocumentTiers>(async doc => await ToggleDocumentValiditeAsync(doc));

                // NOUVELLES COMMANDES
                OpenEditDocumentDialogCommand = new RelayCommand<DocumentTiers>(doc => OpenEditDocumentDialog(doc));
                SaveDocumentInfoCommand = new RelayCommand(async _ => await SaveDocumentInfoAsync(), _ => CanSaveDocumentInfo());
                CancelDocumentEditCommand = new RelayCommand(_ => CancelDocumentEdit());
                ReplaceDocumentCommand = new RelayCommand<DocumentTiers>(async doc => await ReplaceDocumentAsync(doc));
                CheckDocumentsObligatoiresCommand = new RelayCommand(async _ => await CheckDocumentsObligatoiresAsync());
                ViewDocumentsExpiresCommand = new RelayCommand(async _ => await ViewDocumentsExpiresAsync());

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
                    OnPropertyChanged(nameof(DocumentsObligatoires));
                    OnPropertyChanged(nameof(HasDocumentsManquants));
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

        // ══════════════════════════════════════════════════════════════
        // ✅ PROPRIÉTÉS PERMISSIONS
        // ══════════════════════════════════════════════════════════════

        public bool CanViewTiers => SessionManager.HasPermission("Tiers.View");
        public bool CanCreateTiers => SessionManager.HasPermission("Tiers.Create");
        public bool CanEditTiers => SessionManager.HasPermission("Tiers.Edit");
        public bool CanDeleteTiers => SessionManager.HasPermission("Tiers.Delete");

        // Permissions for CompteBancaire (actions are exposed in Tiers page)
        public bool CanViewCompteBancaire => SessionManager.HasPermission("CompteBancaire.View");
        public bool CanCreateCompteBancaire => SessionManager.HasPermission("CompteBancaire.Create");
        public bool CanEditCompteBancaire => SessionManager.HasPermission("CompteBancaire.Edit");
        public bool CanDeleteCompteBancaire => SessionManager.HasPermission("CompteBancaire.Delete");

        // Permissions for DocumentTiers (actions are exposed in Tiers page)
        public bool CanViewDocumentTiers => SessionManager.HasPermission("DocumentTiers.View");
        public bool CanCreateDocumentTiers => SessionManager.HasPermission("DocumentTiers.Create");
        public bool CanEditDocumentTiers => SessionManager.HasPermission("DocumentTiers.Edit");
        public bool CanDeleteDocumentTiers => SessionManager.HasPermission("DocumentTiers.Delete");

        private readonly string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";

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

        // NOUVELLES PROPRIÉTÉS POUR L'ÉDITION DES DOCUMENTS
        public bool IsDocumentEditDialogOpen
        {
            get => _isDocumentEditDialogOpen;
            set => SetProperty(ref _isDocumentEditDialogOpen, value);
        }

        public string? NumeroDocument
        {
            get => _numeroDocument;
            set => SetProperty(ref _numeroDocument, value);
        }

        public DateTime? DateExpiration
        {
            get => _dateExpiration;
            set => SetProperty(ref _dateExpiration, value);
        }

        public DateTime? DateEmission
        {
            get => _dateEmission;
            set => SetProperty(ref _dateEmission, value);
        }

        public string? DescriptionDocument
        {
            get => _descriptionDocument;
            set => SetProperty(ref _descriptionDocument, value);
        }

        // PROPRIÉTÉS CALCULÉES POUR LES DOCUMENTS OBLIGATOIRES
        public ObservableCollection<TypeDocument> DocumentsObligatoires
        {
            get
            {
                var collection = new ObservableCollection<TypeDocument>();

                if (SelectedTiers == null) return collection;

                var documentService = new DocumentTiersService();
                var obligatoires = documentService.GetDocumentsObligatoires(SelectedTiers);

                foreach (var doc in obligatoires)
                {
                    collection.Add(doc);
                }

                return collection;
            }
        }

        public bool HasDocumentsManquants
        {
            get
            {
                if (SelectedTiers == null) return false;

                var documentsPresents = DocumentsOfSelectedTiers.Select(d => d.Type).ToList();
                var documentsObligatoires = DocumentsObligatoires.ToList();

                return documentsObligatoires.Any(obligatoire => !documentsPresents.Contains(obligatoire));
            }
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
        public ICommand ValidateIBANCommand { get; }

        // Commandes Documents
        public ICommand OpenAddDocumentDialogCommand { get; }
        public ICommand AddDocumentCommand { get; }
        public ICommand DeleteDocumentCommand { get; }
        public ICommand OpenDocumentCommand { get; }
        public ICommand ToggleDocumentValiditeCommand { get; }

        // NOUVELLES COMMANDES
        public ICommand OpenEditDocumentDialogCommand { get; }
        public ICommand SaveDocumentInfoCommand { get; }
        public ICommand CancelDocumentEditCommand { get; }
        public ICommand ReplaceDocumentCommand { get; }
        public ICommand CheckDocumentsObligatoiresCommand { get; }
        public ICommand ViewDocumentsExpiresCommand { get; }

        #endregion

        #region Methods - Tiers

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!CanViewTiers)
            {
                MessageBox.Show(
                    "Accès refusé : vous n'avez pas la permission de consulter les tiers.",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Tiers.Clear();
                return;
            }

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
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Accès refusé : vous n'avez pas la permission de consulter les tiers.",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Tiers.Clear();
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
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Accès refusé : vous n'avez pas la permission de consulter les tiers.",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Tiers.Clear();
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

        private void ValidateIBAN()
        {
            if (DialogCompte == null || string.IsNullOrWhiteSpace(DialogCompte.IBAN))
            {
                MessageBox.Show("Veuillez saisir un IBAN.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string iban = DialogCompte.IBAN.Replace(" ", "").ToUpper();

            // Validation basique du format
            if (iban.Length >= 15 && iban.Length <= 34 &&
                System.Text.RegularExpressions.Regex.IsMatch(iban, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$"))
            {
                MessageBox.Show("✓ Format d'IBAN valide", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "✗ Format d'IBAN invalide\n\n" +
                    "Format attendu : 2 lettres (pays) + 2 chiffres + code banque et compte\n" +
                    "Longueur : 15-34 caractères",
                    "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenAddTiersDialog()
        {
            // ✅ VÉRIFICATION PERMISSION
            if (!CanCreateTiers)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Tiers.Create",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

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

            // ✅ VÉRIFICATION PERMISSION
            if (!CanEditTiers)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Tiers.Edit",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

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
            // ✅ VÉRIFICATION PERMISSION
            if (IsEditMode && !CanEditTiers)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Tiers.Edit",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!IsEditMode && !CanCreateTiers)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Tiers.Create",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

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

            // ✅ VÉRIFICATION PERMISSION
            if (!CanDeleteTiers)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Tiers.Delete",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

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

            // ✅ VÉRIFICATION PERMISSION
            if (!CanEditTiers)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Tiers.Edit",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

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

            if (!CanViewCompteBancaire)
            {
                // Option: keep the list empty and show a notice
                MessageBox.Show("Accès refusé : vous n'avez pas la permission de consulter les comptes bancaires.", "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            if (!CanCreateCompteBancaire)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            if (!CanEditCompteBancaire)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
            if (DialogCompte == null) return false;

            // Vérification des champs obligatoires
            if (DialogCompte.TiersId <= 0) return false;
            if (string.IsNullOrWhiteSpace(DialogCompte.IBAN)) return false;
            if (string.IsNullOrWhiteSpace(DialogCompte.Banque)) return false;
            if (string.IsNullOrWhiteSpace(DialogCompte.Pays)) return false;

            // Validation de la longueur de l'IBAN
            string iban = DialogCompte.IBAN.Replace(" ", "");
            if (iban.Length < 15 || iban.Length > 34) return false;

            return true;
        }

        private async System.Threading.Tasks.Task SaveCompteAsync()
        {
            if (IsCompteEditMode)
            {
                if (!CanEditCompteBancaire)
                {
                    MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                if (!CanCreateCompteBancaire)
                {
                    MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            IsLoading = true;

            try
            {
                // Normaliser l'IBAN (enlever les espaces et mettre en majuscules)
                if (!string.IsNullOrWhiteSpace(DialogCompte.IBAN))
                {
                    DialogCompte.IBAN = DialogCompte.IBAN.Replace(" ", "").ToUpper();
                }

                // Validation supplémentaire
                string iban = DialogCompte.IBAN;
                if (iban.Length < 15 || iban.Length > 34)
                {
                    MessageBox.Show("L'IBAN doit contenir entre 15 et 34 caractères.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    return;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(iban, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$"))
                {
                    MessageBox.Show(
                        "Format d'IBAN invalide.\n\n" +
                        "Le format doit être : 2 lettres (pays) + 2 chiffres + code banque et compte.",
                        "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    return;
                }

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

            if (!CanDeleteCompteBancaire)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            if (!CanViewDocumentTiers)
            {
                // Do not attempt to load documents if user has no view permission
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var documentService = new DocumentTiersService();
                var documents = await documentService.GetDocumentsByTiersAsync(SelectedTiers.Id);

                foreach (var doc in documents)
                {
                    DocumentsOfSelectedTiers.Add(doc);
                }

                OnPropertyChanged(nameof(DocumentsObligatoires));
                OnPropertyChanged(nameof(HasDocumentsManquants));
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

            if (!CanCreateDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsDocumentDialogOpen = true;
        }

        private async System.Threading.Tasks.Task AddDocumentAsync(TypeDocument? type)
        {
            if (SelectedTiers == null || type == null) return;

            if (!CanCreateDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

        // NOUVELLES MÉTHODES POUR L'ÉDITION DES DOCUMENTS

        private void OpenEditDocumentDialog(DocumentTiers? document)
        {
            if (document == null) return;

            SelectedDocument = document;
            NumeroDocument = document.NumeroDocument;
            DateExpiration = document.DateExpiration;
            DateEmission = document.DateEmission;
            DescriptionDocument = document.Description;

            IsDocumentEditDialogOpen = true;
        }

        private bool CanSaveDocumentInfo()
        {
            return SelectedDocument != null;
        }

        private async System.Threading.Tasks.Task SaveDocumentInfoAsync()
        {
            if (SelectedDocument == null) return;

            if (!CanEditDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var documentService = new DocumentTiersService();
                var (success, message) = await documentService.UpdateDocumentInfoAsync(
                    SelectedDocument.Id,
                    NumeroDocument,
                    DateExpiration,
                    DateEmission,
                    DescriptionDocument);

                if (success)
                {
                    MessageBox.Show(message, "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    IsDocumentEditDialogOpen = false;
                    LoadDocumentsOfSelectedTiers();
                }
                else
                {
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void CancelDocumentEdit()
        {
            IsDocumentEditDialogOpen = false;
            SelectedDocument = null;
            NumeroDocument = null;
            DateExpiration = null;
            DateEmission = null;
            DescriptionDocument = null;
        }

        private async System.Threading.Tasks.Task ReplaceDocumentAsync(DocumentTiers? document)
        {
            if (document == null) return;
            if (!CanEditDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous remplacer le fichier du document '{document.TypeDisplay}' ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var documentService = new DocumentTiersService();
                var (success, message) = await documentService.ReplaceDocumentAsync(document.Id);

                MessageBox.Show(message,
                    success ? "Succès" : "Information",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Information);

                if (success)
                {
                    LoadDocumentsOfSelectedTiers();
                }

                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task CheckDocumentsObligatoiresAsync()
        {
            if (SelectedTiers == null) return;

            if (!CanViewDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var documentService = new DocumentTiersService();
                var (allPresent, missingDocuments) = await documentService.CheckDocumentsObligatoiresAsync(SelectedTiers.Id);

                if (allPresent)
                {
                    MessageBox.Show(
                        "✅ Tous les documents obligatoires sont présents !",
                        "Documents complets",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    var message = "⚠️ Documents obligatoires manquants :\n\n";
                    foreach (var docType in missingDocuments)
                    {
                        message += $"• {GetDocumentTypeDisplay(docType)}\n";
                    }

                    MessageBox.Show(message, "Documents manquants",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private async System.Threading.Tasks.Task ViewDocumentsExpiresAsync()
        {
            if (SelectedTiers == null) return;

            if (!CanViewDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var documentService = new DocumentTiersService();
                var documentsExpires = await documentService.GetDocumentsExpiresAsync(SelectedTiers.Id);
                var documentsExpireBientot = await documentService.GetDocumentsExpireBientotAsync(SelectedTiers.Id);

                var message = "";

                if (documentsExpires.Any())
                {
                    message += "🔴 Documents expirés :\n\n";
                    foreach (var doc in documentsExpires)
                    {
                        message += $"• {doc.TypeDisplay} - Expiré le {doc.DateExpiration:dd/MM/yyyy}\n";
                    }
                    message += "\n";
                }

                if (documentsExpireBientot.Any())
                {
                    message += "🟠 Documents qui expirent bientôt (30 jours) :\n\n";
                    foreach (var doc in documentsExpireBientot)
                    {
                        message += $"• {doc.TypeDisplay} - Expire le {doc.DateExpiration:dd/MM/yyyy}\n";
                    }
                }

                if (string.IsNullOrEmpty(message))
                {
                    message = "✅ Aucun document expiré ou proche de l'expiration.";
                }

                MessageBox.Show(message, "État des documents",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (!CanDeleteDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            if (!CanViewDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            if (!CanEditDocumentTiers)
            {
                MessageBox.Show(_accessDeniedMessage, "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

        // Méthode utilitaire
        private string GetDocumentTypeDisplay(TypeDocument type)
        {
            return type switch
            {
                TypeDocument.CarteIdentite => "CNI / Carte d'Identité",
                TypeDocument.Passeport => "Passeport",
                TypeDocument.RCCM => "RCCM",
                TypeDocument.NIF => "NIF",
                TypeDocument.QuitusFiscal => "Quitus Fiscal",
                TypeDocument.AttestationTVA => "Attestation TVA",
                TypeDocument.ContratTravail => "Contrat de travail",
                TypeDocument.Autre => "Autre",
                _ => "Inconnu"
            };
        }

        #endregion
    }
}