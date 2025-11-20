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
    /// ViewModel pour la gestion des comptes bancaires
    /// </summary>
    public class CompteBancaireViewModel : ViewModelBase
    {
        // ✅ CORRECTION : NE PAS stocker les services
        // private readonly CompteBancaireService _compteService; ❌ SUPPRIMÉ
        // private readonly TiersService _tiersService; ❌ SUPPRIMÉ

        private bool _isLoading;
        private CompteBancaire? _selectedCompte;
        private bool _isDialogOpen;
        private CompteBancaire _dialogCompte;
        private bool _isEditMode;
        private Tiers? _selectedTiers;
        private string _searchText;

        public CompteBancaireViewModel()
        {
            _dialogCompte = new CompteBancaire();
            _searchText = string.Empty;

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            LoadTiersCommand = new RelayCommand(async _ => await LoadTiersAsync());
            OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
            OpenEditDialogCommand = new RelayCommand<CompteBancaire>(compte => OpenEditDialog(compte));
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelDialog());
            DeleteCommand = new RelayCommand<CompteBancaire>(async compte => await DeleteAsync(compte));
            FilterByTiersCommand = new RelayCommand(async _ => await FilterByTiersAsync());
            ValidateIBANCommand = new RelayCommand(_ => ValidateIBAN());
            SearchCommand = new RelayCommand(async _ => await SearchAsync());

            // Charger les données au démarrage
            LoadDataCommand.Execute(null);
            LoadTiersCommand.Execute(null);
        }

        #region Properties

        /// <summary>
        /// Collection de tous les comptes bancaires
        /// </summary>
        public ObservableCollection<CompteBancaire> Comptes { get; } = new();

        /// <summary>
        /// Collection des tiers pour le ComboBox
        /// </summary>
        public ObservableCollection<Tiers> TiersList { get; } = new();

        /// <summary>
        /// Indicateur de chargement
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Compte bancaire sélectionné dans le DataGrid
        /// </summary>
        public CompteBancaire? SelectedCompte
        {
            get => _selectedCompte;
            set => SetProperty(ref _selectedCompte, value);
        }

        /// <summary>
        /// Indique si le dialog est ouvert
        /// </summary>
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        /// <summary>
        /// Compte bancaire en cours d'édition dans le dialog
        /// </summary>
        public CompteBancaire DialogCompte
        {
            get => _dialogCompte;
            set => SetProperty(ref _dialogCompte, value);
        }

        /// <summary>
        /// Indique si on est en mode édition
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        /// <summary>
        /// Tiers sélectionné pour le filtre
        /// </summary>
        public Tiers? SelectedTiers
        {
            get => _selectedTiers;
            set
            {
                if (SetProperty(ref _selectedTiers, value))
                {
                    FilterByTiersCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Texte de recherche
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Titre du dialog
        /// </summary>
        public string DialogTitle => IsEditMode ? "Modifier le compte bancaire" : "Nouveau compte bancaire";

        /// <summary>
        /// Nombre total de comptes
        /// </summary>
        public int TotalComptes => Comptes.Count;

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand LoadTiersCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand OpenEditDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand FilterByTiersCommand { get; }
        public ICommand ValidateIBANCommand { get; }
        public ICommand SearchCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Charge tous les comptes bancaires
        /// </summary>
        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // ✅ CORRECTION : Créer un nouveau service pour chaque opération
                var service = new CompteBancaireService();
                var comptes = await service.GetAllComptesAsync();

                Comptes.Clear();
                foreach (var compte in comptes)
                {
                    Comptes.Add(compte);
                }

                OnPropertyChanged(nameof(TotalComptes));
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
        /// Charge la liste des tiers
        /// </summary>
        private async System.Threading.Tasks.Task LoadTiersAsync()
        {
            try
            {
                // ✅ CORRECTION : Créer un nouveau service
                var tiersService = new TiersService();
                var tiers = await tiersService.GetTiersActifsAsync();

                TiersList.Clear();
                foreach (var t in tiers)
                {
                    TiersList.Add(t);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des tiers : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Filtre les comptes par tiers sélectionné
        /// </summary>
        private async System.Threading.Tasks.Task FilterByTiersAsync()
        {
            if (SelectedTiers == null)
            {
                await LoadDataAsync();
                return;
            }

            IsLoading = true;

            try
            {
                // ✅ CORRECTION : Créer un nouveau service
                var service = new CompteBancaireService();
                var comptes = await service.GetComptesByTiersAsync(SelectedTiers.Id);

                Comptes.Clear();
                foreach (var compte in comptes)
                {
                    Comptes.Add(compte);
                }

                OnPropertyChanged(nameof(TotalComptes));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du filtrage : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Recherche dans les comptes
        /// </summary>
        private async System.Threading.Tasks.Task SearchAsync()
        {
            IsLoading = true;

            try
            {
                // ✅ CORRECTION : Créer un nouveau service
                var service = new CompteBancaireService();
                var allComptes = await service.GetAllComptesAsync();

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    Comptes.Clear();
                    foreach (var compte in allComptes)
                    {
                        Comptes.Add(compte);
                    }
                }
                else
                {
                    var filtered = allComptes.Where(c =>
                        c.IBAN.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        c.Banque.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        c.Tiers.Nom.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        (c.Tiers.Prenom != null && c.Tiers.Prenom.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    Comptes.Clear();
                    foreach (var compte in filtered)
                    {
                        Comptes.Add(compte);
                    }
                }

                OnPropertyChanged(nameof(TotalComptes));
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

        /// <summary>
        /// Ouvre le dialog pour créer un nouveau compte
        /// </summary>
        private void OpenAddDialog()
        {
            IsEditMode = false;
            DialogCompte = new CompteBancaire
            {
                Pays = "Guinée",
                IBAN = "",
                BIC = "",
                Banque = ""
            };
            IsDialogOpen = true;
        }

        /// <summary>
        /// Ouvre le dialog pour modifier un compte existant
        /// </summary>
        private void OpenEditDialog(CompteBancaire? compte)
        {
            if (compte == null) return;

            IsEditMode = true;
            DialogCompte = new CompteBancaire
            {
                Id = compte.Id,
                TiersId = compte.TiersId,
                IBAN = compte.IBAN,
                BIC = compte.BIC,
                Banque = compte.Banque,
                Pays = compte.Pays
                // ✅ Ne pas copier Tiers (navigation)
            };
            IsDialogOpen = true;
        }

        /// <summary>
        /// Vérifie si le compte peut être enregistré
        /// </summary>
        private bool CanSave()
        {
            return DialogCompte != null &&
                   DialogCompte.TiersId > 0 &&
                   !string.IsNullOrWhiteSpace(DialogCompte.IBAN) &&
                   !string.IsNullOrWhiteSpace(DialogCompte.Banque) &&
                   !string.IsNullOrWhiteSpace(DialogCompte.Pays);
        }

        /// <summary>
        /// Enregistre le compte bancaire
        /// </summary>
        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;

            try
            {
                // ✅ CORRECTION : Créer un nouveau service
                var service = new CompteBancaireService();

                // Validation de l'IBAN
                if (!service.ValidateIBAN(DialogCompte.IBAN))
                {
                    MessageBox.Show("Le format de l'IBAN n'est pas valide.\n\n" +
                        "L'IBAN doit contenir entre 15 et 34 caractères alphanumériques.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    return;
                }

                if (IsEditMode)
                {
                    // Modification
                    var (success, message) = await service.UpdateCompteAsync(DialogCompte);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
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
                    // Création
                    var (success, message, compte) = await service.CreateCompteAsync(DialogCompte);

                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
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
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Annule et ferme le dialog
        /// </summary>
        private void CancelDialog()
        {
            IsDialogOpen = false;
            DialogCompte = new CompteBancaire();
        }

        /// <summary>
        /// Supprime un compte bancaire
        /// </summary>
        private async System.Threading.Tasks.Task DeleteAsync(CompteBancaire? compte)
        {
            if (compte == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer ce compte bancaire ?\n\n" +
                $"Tiers : {compte.Tiers?.Nom ?? "Inconnu"}\n" +
                $"Banque : {compte.Banque}\n" +
                $"IBAN : {compte.IBAN}\n\n" +
                "Cette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                // ✅ CORRECTION : Créer un nouveau service
                var service = new CompteBancaireService();
                var (success, message) = await service.DeleteCompteAsync(compte.Id);

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

        /// <summary>
        /// Valide le format de l'IBAN saisi
        /// </summary>
        private void ValidateIBAN()
        {
            if (string.IsNullOrWhiteSpace(DialogCompte?.IBAN))
                return;

            // ✅ CORRECTION : Créer un nouveau service
            var service = new CompteBancaireService();
            var isValid = service.ValidateIBAN(DialogCompte.IBAN);

            if (!isValid)
            {
                MessageBox.Show("Le format de l'IBAN n'est pas valide.\n\n" +
                    "L'IBAN doit contenir entre 15 et 34 caractères alphanumériques\n" +
                    "et commencer par 2 lettres (code pays).",
                    "Validation IBAN",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("L'IBAN est valide ! ✓",
                    "Validation IBAN",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Obtient les statistiques des comptes
        /// </summary>
        public async System.Threading.Tasks.Task<string> GetStatistiquesAsync()
        {
            try
            {
                // ✅ CORRECTION : Créer un nouveau service
                var service = new CompteBancaireService();
                var allComptes = await service.GetAllComptesAsync();

                var comptesParBanque = allComptes.GroupBy(c => c.Banque).Select(g => new
                {
                    Banque = g.Key,
                    Count = g.Count()
                }).OrderByDescending(x => x.Count);

                var stats = $"📊 STATISTIQUES DES COMPTES BANCAIRES\n\n";
                stats += $"Total de comptes : {allComptes.Count}\n\n";
                stats += "Répartition par banque :\n";

                foreach (var item in comptesParBanque)
                {
                    stats += $"• {item.Banque} : {item.Count} compte(s)\n";
                }

                return stats;
            }
            catch
            {
                return "Erreur lors du calcul des statistiques.";
            }
        }

        #endregion
    }
}