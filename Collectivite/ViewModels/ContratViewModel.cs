using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class ContratViewModel : ViewModelBase
    {
        private readonly ContratService _contratService;
        private string _accessDeniedMessage = "Vous n'avez pas la permission pour cette action.";
        private bool _isLoading;
        private Contrats? _selectedContrat;
        private bool _isDialogOpen;
        private Contrats _dialogContrat;
        private bool _isEditMode;

        // ═══════════════════════════════════════════════════════════
        // FILTRES
        // ═══════════════════════════════════════════════════════════
        private string _rechercheTexte = string.Empty;
        private Tiers? _tiersFiltre;
        private StatutContrat? _statutFiltre;
        private DateTime? _dateSignatureDebut;
        private DateTime? _dateSignatureFin;
        private DateTime? _dateEcheanceDebut;
        private DateTime? _dateEcheanceFin;
        private double? _montantMin;
        private double? _montantMax;

        // Liste complète des contrats (non filtrée)
        private List<Contrats> _tousLesContrats = new();

        public ContratViewModel(ContratService contrat)
        {
            ExerciceService.Instance.ExerciceChanged += OnExerciceChanged;
            _contratService = contrat;
            _dialogContrat = new Contrats
            {
                NumeroContrat = "",
                DateSignature = DateOnly.FromDateTime(DateTime.Now),
                DateEcheance = DateOnly.FromDateTime(DateTime.Now),
            };

            // Commandes CRUD
            LoadContratCommand = new RelayCommand(async _ => await LoadContratAsync());
            OppenAddContratCommand = new RelayCommand(async _ => await OpenAddContrat());
            OppenEditContratCommand = new RelayCommand<Contrats>(contrat => OppenEditContrat(contrat));
            SaveContratCommand = new RelayCommand(async _ => await SaveContratAsync(), _ => CanSaveContrat());
            CancelContratCommand = new RelayCommand(_ => CancelContrat());
            DeleteContratCommand = new RelayCommand<Contrats>(async contrat => await DeleteContratAsync(contrat));

            // Commandes de filtrage
            AppliquerFiltresCommand = new RelayCommand(_ => AppliquerFiltres());
            ReinitialiserFiltresCommand = new RelayCommand(_ => ReinitialiserFiltres());

            // Initialiser le statut sélectionné
            _statutContratItemSelectionne = StatutsDisponibles.FirstOrDefault();

            // Charger les données au démarrage
            LoadContratCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Contrats> Contrats { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Tiers> TiersList { get; } = new();

        // Permissions dynamiques
        public bool CanViewContrat => SessionManager.HasPermission("Contrats.View");
        public bool CanCreateContrat => SessionManager.HasPermission("Contrats.Create");
        public bool CanEditContrat => SessionManager.HasPermission("Contrats.Edit");
        public bool CanDeleteContrat => SessionManager.HasPermission("Contrats.Delete");

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Contrats? SelectedContrat
        {
            get => _selectedContrat;
            set => SetProperty(ref _selectedContrat, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public Contrats DialogContrat
        {
            get => _dialogContrat;
            set => SetProperty(ref _dialogContrat, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // Propriétés pour les dates avec conversion DateTime <-> DateOnly
        public DateTime DialogContratDateSignature
        {
            get => DialogContrat.DateSignature == default
                   ? DateTime.Now
                   : DialogContrat.DateSignature.ToDateTime(TimeOnly.MinValue);

            set
            {
                DialogContrat.DateSignature = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        public DateTime DialogContratDateEcheance
        {
            get => DialogContrat.DateEcheance == default
                   ? DateTime.Now
                   : DialogContrat.DateEcheance.ToDateTime(TimeOnly.MinValue);

            set
            {
                DialogContrat.DateEcheance = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        public string DialogTitle => IsEditMode ? "Modifier le contrat" : "Nouveau contrat";

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS DE FILTRAGE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Texte de recherche (numéro contrat, objet, tiers)
        /// </summary>
        public string RechercheTexte
        {
            get => _rechercheTexte;
            set
            {
                if (SetProperty(ref _rechercheTexte, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Filtre par tiers
        /// </summary>
        public Tiers? TiersFiltre
        {
            get => _tiersFiltre;
            set
            {
                if (SetProperty(ref _tiersFiltre, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Filtre par statut du contrat
        /// </summary>
        public StatutContrat? StatutFiltre
        {
            get => _statutFiltre;
            set
            {
                if (SetProperty(ref _statutFiltre, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Date signature début
        /// </summary>
        public DateTime? DateSignatureDebut
        {
            get => _dateSignatureDebut;
            set
            {
                if (SetProperty(ref _dateSignatureDebut, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Date signature fin
        /// </summary>
        public DateTime? DateSignatureFin
        {
            get => _dateSignatureFin;
            set
            {
                if (SetProperty(ref _dateSignatureFin, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Date échéance début
        /// </summary>
        public DateTime? DateEcheanceDebut
        {
            get => _dateEcheanceDebut;
            set
            {
                if (SetProperty(ref _dateEcheanceDebut, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Date échéance fin
        /// </summary>
        public DateTime? DateEcheanceFin
        {
            get => _dateEcheanceFin;
            set
            {
                if (SetProperty(ref _dateEcheanceFin, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Montant minimum
        /// </summary>
        public double? MontantMin
        {
            get => _montantMin;
            set
            {
                if (SetProperty(ref _montantMin, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Montant maximum
        /// </summary>
        public double? MontantMax
        {
            get => _montantMax;
            set
            {
                if (SetProperty(ref _montantMax, value))
                {
                    AppliquerFiltres();
                }
            }
        }

        /// <summary>
        /// Liste des statuts disponibles pour le filtre
        /// </summary>
        public List<StatutContratItem> StatutsDisponibles { get; } = new()
        {
            new StatutContratItem { Statut = null, Libelle = "Tous les statuts" },
            new StatutContratItem { Statut = StatutContrat.EnCours, Libelle = "En cours" },
            new StatutContratItem { Statut = StatutContrat.Expire, Libelle = "Expiré" },
            new StatutContratItem { Statut = StatutContrat.ProchainementExpire, Libelle = "Expire bientôt (30j)" }
        };

        /// <summary>
        /// Item sélectionné pour le filtre statut
        /// </summary>
        private StatutContratItem? _statutContratItemSelectionne;
        public StatutContratItem? StatutContratItemSelectionne
        {
            get => _statutContratItemSelectionne;
            set
            {
                if (SetProperty(ref _statutContratItemSelectionne, value))
                {
                    StatutFiltre = value?.Statut;
                }
            }
        }

        /// <summary>
        /// Nombre total de contrats (avant filtrage)
        /// </summary>
        public int NombreTotalContrats => _tousLesContrats.Count;

        /// <summary>
        /// Nombre de contrats filtrés
        /// </summary>
        public int NombreContratsFiltres => Contrats.Count;

        /// <summary>
        /// Indique si des filtres sont actifs
        /// </summary>
        public bool FiltresActifs =>
            !string.IsNullOrWhiteSpace(RechercheTexte) ||
            (TiersFiltre != null && TiersFiltre.Id > 0) ||
            StatutFiltre != null ||
            DateSignatureDebut != null ||
            DateSignatureFin != null ||
            DateEcheanceDebut != null ||
            DateEcheanceFin != null ||
            MontantMin != null ||
            MontantMax != null;

        /// <summary>
        /// Montant total des contrats filtrés
        /// </summary>
        public double MontantTotalFiltres => Contrats.Sum(c => c.MontantContrat);

        /// <summary>
        /// Nombre de contrats expirés
        /// </summary>
        public int NombreContratsExpires => _tousLesContrats.Count(c =>
            c.DateEcheance < DateOnly.FromDateTime(DateTime.Now));

        /// <summary>
        /// Nombre de contrats expirant bientôt (30 jours)
        /// </summary>
        public int NombreContratsExpirantBientot => _tousLesContrats.Count(c =>
        {
            var aujourdHui = DateOnly.FromDateTime(DateTime.Now);
            var dans30Jours = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            return c.DateEcheance >= aujourdHui && c.DateEcheance <= dans30Jours;
        });

        /// <summary>
        /// Nombre de contrats en cours
        /// </summary>
        public int NombreContratsEnCours => _tousLesContrats.Count(c =>
            c.DateEcheance >= DateOnly.FromDateTime(DateTime.Now));

        #endregion

        #region Commands

        public ICommand LoadContratCommand { get; }
        public ICommand OppenAddContratCommand { get; }
        public ICommand OppenEditContratCommand { get; }
        public ICommand SaveContratCommand { get; }
        public ICommand CancelContratCommand { get; }
        public ICommand DeleteContratCommand { get; }

        // Commandes de filtrage
        public ICommand AppliquerFiltresCommand { get; }
        public ICommand ReinitialiserFiltresCommand { get; }

        #endregion

        #region Methods

        public void Cleanup()
        {
            ExerciceService.Instance.ExerciceChanged -= OnExerciceChanged;
        }

        public async Task LoadContratAsync()
        {
            IsLoading = true;
            try
            {
                if (!CanViewContrat)
                {
                    MessageBox.Show(
                        "Accès refusé : vous n'avez pas la permission de consulter les contrats.",
                        "Accès refusé",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    Contrats.Clear();
                    _tousLesContrats.Clear();
                    return;
                }

                // Charger tous les contrats
                var contrats = await _contratService.GetAllContratsAsync();
                _tousLesContrats = contrats;

                // Charger les tiers pour le filtre
                var tiersService = new TiersService();
                var tiers = await tiersService.GetTiersActifsAsync();

                TiersList.Clear();
                
                foreach (var t in tiers)
                {
                    TiersList.Add(t);
                }

                // Charger les exercices
                var exerciceService = new ExerciceService();
                var exercices = await exerciceService.GetAllExerciceAsync();

                Exercices.Clear();
                foreach (var ex in exercices.Where(e => !e.EstCloture))
                {
                    Exercices.Add(ex);
                }

                // Appliquer les filtres
                AppliquerFiltres();

                // Notifier les statistiques
                NotifierStatistiques();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des contrats : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Applique les filtres sur la liste des contrats
        /// </summary>
        private void AppliquerFiltres()
        {
            var resultats = _tousLesContrats.AsEnumerable();

            // ═══════════════════════════════════════════════════════════
            // FILTRE PAR TEXTE DE RECHERCHE
            // ═══════════════════════════════════════════════════════════
            if (!string.IsNullOrWhiteSpace(RechercheTexte))
            {
                var recherche = RechercheTexte.ToLower().Trim();
                resultats = resultats.Where(c =>
                    (c.NumeroContrat?.ToLower().Contains(recherche) ?? false) ||
                    (c.Objet?.ToLower().Contains(recherche) ?? false) ||
                    (c.Tiers?.Nom?.ToLower().Contains(recherche) ?? false));
            }

            // ═══════════════════════════════════════════════════════════
            // FILTRE PAR TIERS
            // ═══════════════════════════════════════════════════════════
            if (TiersFiltre != null && TiersFiltre.Id > 0)
            {
                resultats = resultats.Where(c => c.TiersId == TiersFiltre.Id);
            }

            // ═══════════════════════════════════════════════════════════
            // FILTRE PAR STATUT
            // ═══════════════════════════════════════════════════════════
            if (StatutFiltre != null)
            {
                var aujourdHui = DateOnly.FromDateTime(DateTime.Now);
                var dans30Jours = DateOnly.FromDateTime(DateTime.Now.AddDays(30));

                switch (StatutFiltre)
                {
                    case StatutContrat.EnCours:
                        resultats = resultats.Where(c => c.DateEcheance >= aujourdHui);
                        break;

                    case StatutContrat.Expire:
                        resultats = resultats.Where(c => c.DateEcheance < aujourdHui);
                        break;

                    case StatutContrat.ProchainementExpire:
                        resultats = resultats.Where(c =>
                            c.DateEcheance >= aujourdHui && c.DateEcheance <= dans30Jours);
                        break;
                }
            }

            // ═══════════════════════════════════════════════════════════
            // FILTRE PAR DATE DE SIGNATURE
            // ═══════════════════════════════════════════════════════════
            if (DateSignatureDebut.HasValue)
            {
                var dateDebut = DateOnly.FromDateTime(DateSignatureDebut.Value);
                resultats = resultats.Where(c => c.DateSignature >= dateDebut);
            }

            if (DateSignatureFin.HasValue)
            {
                var dateFin = DateOnly.FromDateTime(DateSignatureFin.Value);
                resultats = resultats.Where(c => c.DateSignature <= dateFin);
            }

            // ═══════════════════════════════════════════════════════════
            // FILTRE PAR DATE D'ÉCHÉANCE
            // ═══════════════════════════════════════════════════════════
            if (DateEcheanceDebut.HasValue)
            {
                var dateDebut = DateOnly.FromDateTime(DateEcheanceDebut.Value);
                resultats = resultats.Where(c => c.DateEcheance >= dateDebut);
            }

            if (DateEcheanceFin.HasValue)
            {
                var dateFin = DateOnly.FromDateTime(DateEcheanceFin.Value);
                resultats = resultats.Where(c => c.DateEcheance <= dateFin);
            }

            // ═══════════════════════════════════════════════════════════
            // FILTRE PAR MONTANT
            // ═══════════════════════════════════════════════════════════
            if (MontantMin.HasValue)
            {
                resultats = resultats.Where(c => c.MontantContrat >= MontantMin.Value);
            }

            if (MontantMax.HasValue)
            {
                resultats = resultats.Where(c => c.MontantContrat <= MontantMax.Value);
            }

            // ═══════════════════════════════════════════════════════════
            // METTRE À JOUR LA COLLECTION
            // ═══════════════════════════════════════════════════════════
            Contrats.Clear();
            foreach (var c in resultats.OrderByDescending(c => c.DateSignature))
            {
                Contrats.Add(c);
            }

            // Notifier les propriétés liées
            NotifierStatistiques();
        }

        /// <summary>
        /// Réinitialise tous les filtres
        /// </summary>
        private void ReinitialiserFiltres()
        {
            // Réinitialiser les valeurs
            _rechercheTexte = string.Empty;
            _tiersFiltre = null;
            _statutFiltre = null;
            _statutContratItemSelectionne = StatutsDisponibles.FirstOrDefault();
            _dateSignatureDebut = null;
            _dateSignatureFin = null;
            _dateEcheanceDebut = null;
            _dateEcheanceFin = null;
            _montantMin = null;
            _montantMax = null;

            // Notifier tous les changements
            OnPropertyChanged(nameof(RechercheTexte));
            OnPropertyChanged(nameof(TiersFiltre));
            OnPropertyChanged(nameof(StatutFiltre));
            OnPropertyChanged(nameof(StatutContratItemSelectionne));
            OnPropertyChanged(nameof(DateSignatureDebut));
            OnPropertyChanged(nameof(DateSignatureFin));
            OnPropertyChanged(nameof(DateEcheanceDebut));
            OnPropertyChanged(nameof(DateEcheanceFin));
            OnPropertyChanged(nameof(MontantMin));
            OnPropertyChanged(nameof(MontantMax));
            OnPropertyChanged(nameof(FiltresActifs));

            // Réappliquer les filtres (affichera tout)
            AppliquerFiltres();
        }

        /// <summary>
        /// Notifie les propriétés de statistiques
        /// </summary>
        private void NotifierStatistiques()
        {
            OnPropertyChanged(nameof(NombreTotalContrats));
            OnPropertyChanged(nameof(NombreContratsFiltres));
            OnPropertyChanged(nameof(MontantTotalFiltres));
            OnPropertyChanged(nameof(NombreContratsExpires));
            OnPropertyChanged(nameof(NombreContratsExpirantBientot));
            OnPropertyChanged(nameof(NombreContratsEnCours));
            OnPropertyChanged(nameof(FiltresActifs));
        }

        public async Task OpenAddContrat()
        {
            if (!CanCreateContrat)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Contrats.Create",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var exercices = await _contratService.GetAllExercie();
                Exercices.Clear();
                foreach (var exercice in exercices)
                {
                    Exercices.Add(exercice);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des exercices : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Utiliser l'exercice courant du service
            var defaultExerciceId = ExerciceService.Instance.CurrentExercice?.Id ??
                                    Exercices.FirstOrDefault(e => !e.EstCloture)?.Id ??
                                    Exercices.FirstOrDefault()?.Id ?? 0;
            var defaultTiersId = TiersList.FirstOrDefault(t => t.Id > 0)?.Id ?? 0;

            string numero = string.Empty;
            try
            {
                numero = await _contratService.GenerateNextNumeroAsync();
            }
            catch
            {
                numero = string.Empty;
            }

            DialogContrat = new Contrats
            {
                NumeroContrat = numero,
                DateSignature = DateOnly.FromDateTime(DateTime.Now),
                DateEcheance = DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
                ExerciceId = defaultExerciceId,
                TiersId = defaultTiersId
            };

            IsEditMode = false;
            OnPropertyChanged(nameof(DialogContrat));
            OnPropertyChanged(nameof(DialogContratDateSignature));
            OnPropertyChanged(nameof(DialogContratDateEcheance));
            OnPropertyChanged(nameof(DialogTitle));
            IsDialogOpen = true;
        }

        private void OppenEditContrat(Contrats? contrats)
        {
            if (contrats == null)
                return;

            if (!CanEditContrat)
            {
                MessageBox.Show(
                    _accessDeniedMessage + "\nPermission requise : Contrats.Edit",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsEditMode = true;
            DialogContrat = new Contrats
            {
                Id = contrats.Id,
                NumeroContrat = contrats.NumeroContrat,
                DateSignature = contrats.DateSignature,
                DateEcheance = contrats.DateEcheance,
                TiersId = contrats.TiersId,
                Tiers = contrats.Tiers,
                Objet = contrats.Objet,
                MontantContrat = contrats.MontantContrat,
                FichierJoin = contrats.FichierJoin,
                ExerciceId = contrats.ExerciceId,
                Exercice = contrats.Exercice,
            };

            OnPropertyChanged(nameof(DialogContrat));
            OnPropertyChanged(nameof(DialogContratDateSignature));
            OnPropertyChanged(nameof(DialogContratDateEcheance));
            OnPropertyChanged(nameof(DialogTitle));
            IsDialogOpen = true;
        }

        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadContratAsync();
            });
        }

        private bool CanSaveContrat()
        {
            return !string.IsNullOrWhiteSpace(DialogContrat.NumeroContrat) &&
                   !string.IsNullOrWhiteSpace(DialogContrat.Objet) &&
                   DialogContrat.MontantContrat > 0 &&
                   DialogContrat.TiersId > 0 &&
                   DialogContrat.ExerciceId > 0;
        }

        private async Task SaveContratAsync()
        {
            try
            {
                IsLoading = true;

                if (IsEditMode)
                {
                    if (!CanEditContrat)
                    {
                        MessageBox.Show(
                            _accessDeniedMessage + "\nPermission requise : Contrats.Edit",
                            "Accès refusé",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    var (success, message) = await _contratService.UpdateContratsAsync(DialogContrat);

                    if (success)
                    {
                        MessageBox.Show("Contrat mis à jour avec succès.",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadContratAsync();
                        IsDialogOpen = false;
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    if (!CanCreateContrat)
                    {
                        MessageBox.Show(
                            _accessDeniedMessage + "\nPermission requise : Contrats.Create",
                            "Accès refusé",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    DialogContrat.DateSignature = DateOnly.FromDateTime(DialogContratDateSignature);
                    DialogContrat.DateEcheance = DateOnly.FromDateTime(DialogContratDateEcheance);

                    var (successCreate, messageCreate, _) = await _contratService.CreateContratAsync(DialogContrat);
                    if (successCreate)
                    {
                        MessageBox.Show(messageCreate,
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadContratAsync();
                        IsDialogOpen = false;
                    }
                    else
                    {
                        MessageBox.Show(messageCreate, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du contrat : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelContrat()
        {
            IsDialogOpen = false;
        }

        private async Task DeleteContratAsync(Contrats? contrats)
        {
            if (contrats == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le contrat '{contrats.NumeroContrat}' ?\n\n" +
                $"Tiers : {contrats.Tiers?.Nom}\n" +
                $"Montant : {contrats.MontantContrat:N0} GNF",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (!CanDeleteContrat)
                {
                    MessageBox.Show(
                        _accessDeniedMessage + "\nPermission requise : Contrats.Delete",
                        "Accès refusé",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                var (success, message) = await _contratService.DeleteContratAsync(contrats.Id);
                if (success)
                {
                    MessageBox.Show("Contrat supprimé avec succès.",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadContratAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                IsLoading = false;
            }
        }

        #endregion
    }

    #region Classes Helper

    /// <summary>
    /// Énumération des statuts de contrat
    /// </summary>
    public enum StatutContrat
    {
        EnCours,
        Expire,
        ProchainementExpire
    }

    /// <summary>
    /// Item pour le ComboBox des statuts
    /// </summary>
    public class StatutContratItem
    {
        public StatutContrat? Statut { get; set; }
        public string Libelle { get; set; } = string.Empty;
    }

    #endregion
}