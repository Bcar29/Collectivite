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
    public class OrdreRecetteFormViewModel : ViewModelBase
    {
        private bool _isLoading;
        private OrdreRecette _ordreRecette;
        private bool _isEditMode;
        private bool _hasValidationErrors;

        public OrdreRecetteFormViewModel(int? ordreRecetteId = null)
        {
            _ordreRecette = new OrdreRecette
            {
                DateOrdre = DateTime.Now,
                NumeroOrdre = "",
                MontantOrdre = 0,
                MontantOrdreLettre = "",
                Comptable = ""
            };

            // Commandes
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            NavigateBackCommand = new RelayCommand(_ => NavigateBack());
            ConvertMontantToLettresCommand = new RelayCommand(_ => ConvertMontantToLettres());

            // Charger les données
            LoadDataCommand.Execute(null);

            // Si ID fourni, mode édition
            if (ordreRecetteId.HasValue)
            {
                _isEditMode = true;
                LoadOrdreRecetteAsync(ordreRecetteId.Value);
            }
        }

        #region Properties

        public ObservableCollection<BudgetLine> BudgetLines { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Commune> Communes { get; } = new();
        public ObservableCollection<Tiers> TiersList { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public OrdreRecette OrdreRecette
        {
            get => _ordreRecette;
            set => SetProperty(ref _ordreRecette, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }

        public string PageTitle => IsEditMode ? "Modifier l'ordre de recette" : "Nouvel ordre de recette";

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand NavigateBackCommand { get; }
        public ICommand ConvertMontantToLettresCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var ordreRecetteService = new OrdreRecetteService();

                // Charger les lignes budgétaires
                var budgetLines = await ordreRecetteService.GetBudgetLinesSansEnfantsAsync();

                BudgetLines.Clear();
                foreach (var bl in budgetLines)
                {
                    BudgetLines.Add(bl);
                }

                // Charger les exercices
                using (var context = new AppDbContext())
                {
                    var exerciceService = new ExerciceService();
                    var exercices = await exerciceService.GetAllExerciceAsync();

                    Exercices.Clear();
                    foreach (var ex in exercices.Where(e => !e.EstCloture))
                    {
                        Exercices.Add(ex);
                    }
                }

                // Charger les communes
                using (var context = new AppDbContext())
                {
                    var communeService = new CommuneService();
                    var communes = await communeService.GetAllCommuneAsync();

                    Communes.Clear();
                    foreach (var c in communes)
                    {
                        Communes.Add(c);
                    }
                }

                // Charger les tiers
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
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void LoadOrdreRecetteAsync(int ordreRecetteId)
        {
            IsLoading = true;

            try
            {
                var ordreRecetteService = new OrdreRecetteService();
                var ordreRecette = await ordreRecetteService.GetOrdreRecetteByIdAsync(ordreRecetteId);

                if (ordreRecette != null)
                {
                    OrdreRecette = new OrdreRecette
                    {
                        Id = ordreRecette.Id,
                        NumeroOrdre = ordreRecette.NumeroOrdre,
                        BudgetLineId = ordreRecette.BudgetLineId,
                        ExerciceId = ordreRecette.ExerciceId,
                        CommuneId = ordreRecette.CommuneId,
                        Comptable = ordreRecette.Comptable,
                        TiersId = ordreRecette.TiersId,
                        Motifs = ordreRecette.Motifs,
                        MontantOrdre = ordreRecette.MontantOrdre,
                        MontantOrdreLettre = ordreRecette.MontantOrdreLettre,
                        DateOrdre = ordreRecette.DateOrdre
                    };
                }
                else
                {
                    MessageBox.Show("Ordre de recette introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigateBack();
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
            bool isValid = OrdreRecette != null &&
                   !string.IsNullOrWhiteSpace(OrdreRecette.NumeroOrdre) &&
                   OrdreRecette.BudgetLineId > 0 &&
                   OrdreRecette.ExerciceId > 0 &&
                   OrdreRecette.CommuneId > 0 &&
                   !string.IsNullOrWhiteSpace(OrdreRecette.Comptable) &&
                   OrdreRecette.MontantOrdre > 0 &&
                   !string.IsNullOrWhiteSpace(OrdreRecette.MontantOrdreLettre);

            HasValidationErrors = !isValid;
            return isValid;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            IsLoading = true;
            try
            {
                var ordreRecetteService = new OrdreRecetteService();

                if (IsEditMode)
                {
                    // ========== MODE MODIFICATION ==========
                    var (success, message) = await ordreRecetteService.UpdateOrdreRecetteAsync(OrdreRecette);

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
                    // ========== MODE CRÉATION ==========

                    // 1️⃣ Créer l'ordre de recette
                    var (success, message, ordreRecette) = await ordreRecetteService.CreateOrdreRecetteAsync(OrdreRecette);

                    // 2️⃣ Si l'ordre est créé avec succès, générer l'écriture comptable
                    if (success && ordreRecette != null)
                    {
                        // Récupérer la ligne budgétaire sélectionnée avec sa nomenclature
                        var budgetLineSelectionnee = BudgetLines
                            .FirstOrDefault(bl => bl.Id == OrdreRecette.BudgetLineId);
                        
                        // Vérifier que la ligne et la nomenclature existent
                        if (budgetLineSelectionnee?.Nommenclature != null)
                        {
                            // 🎯 APPELER LA FONCTION UTILITAIRE
                            var (ecritureSuccess, ecritureMessage, ecriture) =
                                await EcritureComptableHelper.GenererEcritureComptableAsync(
                                    budgetLineSelectionnee, // La ligne budgétaire complète
                                    ordreRecette,// L'ordre de recette créé
                                    null // Pas de mandat dans ce cas
                                );

                            // Ajouter le résultat au message principal
                            if (ecritureSuccess)
                            {
                                message += "\n\n" + ecritureMessage;
                            }
                            else
                            {
                                // L'ordre est créé mais pas l'écriture
                                message += "\n\n⚠️ " + ecritureMessage;
                            }
                        }
                        else
                        {
                            message += "\n\n⚠️ Écriture non générée : ligne budgétaire ou nomenclature invalide.";
                        }
                    }

                    // 3️⃣ Afficher le message final
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

        private void NavigateBack()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var frame = mainWindow.FindName("MainContentFrame") as System.Windows.Controls.Frame;
                if (frame != null && frame.CanGoBack)
                {
                    frame.GoBack();
                }
            }
        }

        private void ConvertMontantToLettres()
        {
            if (OrdreRecette.MontantOrdre > 0)
            {
                OrdreRecette.MontantOrdreLettre = ConvertirNombreEnLettres((long)OrdreRecette.MontantOrdre);
                OnPropertyChanged(nameof(OrdreRecette));
            }
        }

        private string ConvertirNombreEnLettres(long nombre, bool ajouterDevise = true)
        {
            if (nombre == 0) return "zéro" + (ajouterDevise ? " franc guinéen" : "");

            string[] unites = { "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf" };
            string[] dizaines = { "", "dix", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante-dix", "quatre-vingt", "quatre-vingt-dix" };
            string[] speciales = { "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf" };

            string resultat = "";

            if (nombre >= 1000000000)
            {
                long milliards = nombre / 1000000000;
                resultat += ConvertirNombreEnLettres(milliards, false) + " milliard" + (milliards > 1 ? "s" : "") + " ";
                nombre %= 1000000000;
            }

            if (nombre >= 1000000)
            {
                long millions = nombre / 1000000;
                resultat += ConvertirNombreEnLettres(millions, false) + " million" + (millions > 1 ? "s" : "") + " ";
                nombre %= 1000000;
            }

            if (nombre >= 1000)
            {
                long milliers = nombre / 1000;
                if (milliers == 1)
                    resultat += "mille ";
                else
                    resultat += ConvertirNombreEnLettres(milliers, false) + " mille ";
                nombre %= 1000;
            }

            if (nombre >= 100)
            {
                long centaines = nombre / 100;
                if (centaines == 1)
                    resultat += "cent ";
                else
                    resultat += ConvertirNombreEnLettres(centaines, false) + " cent" + (nombre % 100 == 0 ? "s" : "") + " ";
                nombre %= 100;
            }

            if (nombre >= 20)
            {
                long diz = nombre / 10;
                long unit = nombre % 10;

                if (diz == 7 || diz == 9)
                {
                    resultat += dizaines[diz - 1];
                    resultat += "-" + ConvertirNombreEnLettres(10 + unit, false);
                }
                else if (diz == 8)
                {
                    resultat += "quatre-vingt";
                    if (unit == 0)
                        resultat += "s";
                    else
                        resultat += "-" + unites[unit];
                }
                else
                {
                    resultat += dizaines[diz];
                    if (unit == 1 && diz != 8)
                        resultat += "-et-un";
                    else if (unit > 0)
                        resultat += "-" + unites[unit];
                }
            }
            else if (nombre >= 10)
            {
                resultat += speciales[nombre - 10];
            }
            else if (nombre > 0)
            {
                resultat += unites[nombre];
            }

            return resultat.Trim() + (ajouterDevise ? " francs guinéens" : "");
        }

        #endregion
    }
}