using Collectivite.Models;
using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public  class ContratViewModel : ViewModelBase
    {
        private readonly ContratService _contratService;
        private bool _isLoading;
        private Contrats? _selectedContrat;
        private bool _isDialogOpen;
        private Contrats _dialogContrat;
        private bool _isEditMode;

        public ContratViewModel(ContratService contrat)
        {
            _contratService = contrat;
            _dialogContrat = new Contrats
            {

                NumeroContrat = "",
                DateSignature= DateOnly.FromDateTime(DateTime.Now),
                DateEcheance= DateOnly.FromDateTime(DateTime.Now),

            };

            //Commandes

            LoadContratCommand = new RelayCommand(async _ => await LoadContratAsync());
            OppenAddContratCommand = new RelayCommand(async _ => await OpenAddContrat());
            OppenEditContratCommand = new RelayCommand<Contrats>(contrat => OppenEditContrat(contrat));
            SaveContratCommand = new RelayCommand(async _ => await SaveContratAsync(), _ => CanSaveContrat());
            CancelContratCommand = new RelayCommand(_ => CancelContrat());
            DeleteContratCommand = new RelayCommand<Contrats>(async contrat => await DeleteContratAsync(contrat));

            // Charger les données au démarrage
            LoadContratCommand.Execute(null);
        }
        #region Properties
        public ObservableCollection<Contrats> Contrats { get; } = new();
        public ObservableCollection<Exercice> Exercices { get; } = new();
        public ObservableCollection<Tiers> TiersList { get; } = new();
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

        // ✅ CORRECTION : Propriétés pour les dates avec conversion DateTime <-> DateOnly
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

        public string DialogTitle => IsEditMode ? "Modifier contrat" : "Ajouter un contrat";

        #endregion

        #region Commands
        public ICommand LoadContratCommand { get; }
        public ICommand OppenAddContratCommand { get; }
        public ICommand OppenEditContratCommand { get; }
        public ICommand SaveContratCommand { get; }
        public ICommand CancelContratCommand { get; }
        public ICommand DeleteContratCommand { get; }
        #endregion

        #region Methods
        public async System.Threading.Tasks.Task LoadContratAsync()
        {
            IsLoading = true;
            try
            {
                var contrat = await _contratService.GetAllContratsAsync();
                Contrats.Clear();
                foreach (var _contrat in contrat)
                {
                    Contrats.Add(_contrat);
                }

                // Charger les tiers
                var tiersService = new TiersService();
                var tiers = await tiersService.GetTiersActifsAsync();

                TiersList.Clear();

                foreach (var t in tiers)
                {
                    TiersList.Add(t);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des contrats : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async System.Threading.Tasks.Task OpenAddContrat()
        {
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
                MessageBox.Show($"Erreur lors du chargement des exercices : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            DialogContrat = new Contrats();
            IsEditMode = false;
            IsDialogOpen = true;
        }

        private void OppenEditContrat(Contrats? contrats)
        {
            if (contrats == null)
                return;
            IsEditMode = true;
            DialogContrat = new Contrats
            {
                Id = contrats.Id,
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
            IsDialogOpen = true;

        }

        private bool CanSaveContrat()
        {
            return !string.IsNullOrWhiteSpace(DialogContrat.MontantContrat.ToString());
        }

        private async System.Threading.Tasks.Task SaveContratAsync()
        {
            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _contratService.UpdateContratsAsync(DialogContrat);
                    if (success)
                    {
                        MessageBox.Show("Contrat mis à jour avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    // 1. Mettre à jour les vraies valeurs du modèle :
                    DialogContrat.DateSignature = DateOnly.FromDateTime(DialogContratDateSignature);
                    DialogContrat.DateEcheance = DateOnly.FromDateTime(DialogContratDateEcheance);

                    // 2. Affichage
                          //MessageBox.Show($"{DialogContrat.NumeroContrat} {DialogContrat.MontantContrat} {DialogContratDateEcheance} {DialogContrat.ExerciceId} {DialogContratDateSignature} {DialogContrat.Objet}", "info");

                    //creation
                    var (success, message, _) = await _contratService.CreateContratAsync(DialogContrat);
                    if (success)
                    {
                        MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadContratAsync();
                        IsDialogOpen = false;
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du contrat : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async System.Threading.Tasks.Task DeleteContratAsync(Contrats? contrats)
        {
            if (contrats == null) return;
            var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer ce contrat de  '{contrats.Exercice.Libelle}' ?", "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                var (success, message) = await _contratService.DeleteContratAsync(contrats.Id);
                if (success)
                {
                    MessageBox.Show("Contrat supprimé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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

}

