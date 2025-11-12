using Collectivite.Models;
using Collectivite.Services;
using System;
using Collectivite.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Collectivite.ViewModels
{
    public class CompteComptableViewModel : ViewModelBase
    {
        private readonly CompteComptableService _compteService;
        private bool _isLoading;
        private CompteComptable? _selectedCompte;
        private bool _isDialogOpen;
        private CompteComptable _dialogCompte;
        private bool _isEditMode;

        public CompteComptableViewModel(CompteComptableService compte)
        {
            _compteService = compte;
            _dialogCompte = new CompteComptable
            {
                NumeroCompte = "",
                IntituleCompte ="",
            };

            //commandes
            LoadCompteCommand = new RelayCommand(async _ => await LoadCompteAsync());
            OppenAddCompteCommand = new RelayCommand(_ => OpenAddCompte());
            OppenEditCompteCommand = new RelayCommand<CompteComptable>(compte => OpenEditCompte(compte));
            SaveCompteCommand = new RelayCommand(async _ => await SaveCompteAsync(), _ => CanSaveCompte());
            CancelCompteCommand = new RelayCommand(_ => CancelCompte());
            DeleteCompteCommand = new RelayCommand<CompteComptable>(async compte => await DeleteCompteAsync(compte));

           
            //charger les données au démarrage
            LoadCompteCommand.Execute(null);
        }


        #region Properties 
        public ObservableCollection<CompteComptable> CompteComptables { get; } = [];

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public CompteComptable? SelectedCommune
        {
            get => _selectedCompte;
            set => SetProperty(ref _selectedCompte, value);
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public CompteComptable DialogCompte
        {
            get => _dialogCompte;
            set => SetProperty(ref _dialogCompte, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string DialogTitle => IsEditMode ? "Modifier le Compte" : "Ajouter un Compte";


        #endregion

        #region Commands
        public ICommand LoadCompteCommand { get; }
        public ICommand OppenAddCompteCommand { get; }
        public ICommand OppenEditCompteCommand { get; }
        public ICommand SaveCompteCommand { get; }
        public ICommand CancelCompteCommand { get; }
        public ICommand DeleteCompteCommand { get; }
        #endregion


        #region Methods

        public async System.Threading.Tasks.Task LoadCompteAsync()
        {
            IsLoading = true;
            try
            {
                
                var comptes = await _compteService.GetCompteComptablesAsync();
                

                CompteComptables.Clear();

                foreach (var compte in comptes)
                {
                    CompteComptables.Add(compte);
                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des comptes : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                
            }
        }

        private void OpenAddCompte()
        {
            IsEditMode = false;
            DialogCompte = new CompteComptable
            {
                NumeroCompte = "",
                IntituleCompte="",
            };

            OnPropertyChanged(nameof(DialogCompte));

            IsDialogOpen = true;
        }

        private void OpenEditCompte(CompteComptable? compte)
        {
            if (compte == null)
                return;

            IsEditMode = true;
            DialogCompte = new CompteComptable
            {
                Id = compte.Id,
                NumeroCompte = compte.NumeroCompte,
                IntituleCompte = compte.IntituleCompte,
                
            };

            OnPropertyChanged(nameof(DialogCompte));

            IsDialogOpen = true;
        }

        private bool CanSaveCompte()
        {
            return !string.IsNullOrWhiteSpace(DialogCompte.IntituleCompte);
        }

        private async System.Threading.Tasks.Task SaveCompteAsync()
        {
            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _compteService.UpdateCompteComptable(DialogCompte);
                    if (success)
                    {
                        MessageBox.Show("Compte mis à jour avec succès",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadCompteAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var (success, message, _) = await _compteService.CreateCompteComptable(DialogCompte);
                    if (success)
                    {
                        MessageBox.Show(message, "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        IsDialogOpen = false;
                        await LoadCompteAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du compte : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelCompte()
        {
            IsDialogOpen = false;
        }

        private async System.Threading.Tasks.Task DeleteCompteAsync(CompteComptable? compte)
        {
            if (compte == null)
                return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le compte {compte.NumeroCompte} ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                var (success, message) = await _compteService.DeleteCompteComptableAsync(compte.Id);

                if (success)
                {
                    MessageBox.Show(message, "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCompteAsync();
                }
                else
                {
                    MessageBox.Show(message, "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                IsLoading = false;
            }
        }

        #endregion
    }
}
