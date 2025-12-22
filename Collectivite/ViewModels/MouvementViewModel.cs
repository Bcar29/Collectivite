using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    public partial class MouvementViewModel : ObservableObject
    {
        private readonly IMouvementService _mouvementService;

        #region Propriétés observables - Listes principales

        [ObservableProperty]
        private ObservableCollection<MandatPaiementDTO> _mandatsNonPayes = new();

        [ObservableProperty]
        private ObservableCollection<OrdreRecetteEncaissementDTO> _ordresRecetteNonEncaisses = new();

        [ObservableProperty]
        private ObservableCollection<MouvementHistoriqueDTO> _historiqueMouvements = new();

        #endregion

        #region Propriétés observables - Sélection

        [ObservableProperty]
        private MandatPaiementDTO? _mandatSelectionne;

        [ObservableProperty]
        private OrdreRecetteEncaissementDTO? _ordreRecetteSelectionne;

        [ObservableProperty]
        private int _ongletActif = 0;

        #endregion

        #region Propriétés observables - Formulaire de mouvement

        [ObservableProperty]
        private bool _isDialogOpen;

        [ObservableProperty]
        private bool _isDialogPaiement; // true = Paiement, false = Encaissement

        [ObservableProperty]
        private string _dialogTitre = string.Empty;

        [ObservableProperty]
        private string _dialogNumero = string.Empty;

        [ObservableProperty]
        private string _dialogBeneficiaire = string.Empty;

        [ObservableProperty]
        private decimal _dialogMontantTotal;

        [ObservableProperty]
        private decimal _dialogMontantRestant;

        [ObservableProperty]
        private DateTime _dialogDate = DateTime.Today;

        [ObservableProperty]
        private decimal _dialogMontant;

        [ObservableProperty]
        private ModeReglement _dialogModeReglement = ModeReglement.Espece;

        [ObservableProperty]
        private string? _dialogRefVirement;

        [ObservableProperty]
        private string? _dialogNumBanque;

        [ObservableProperty]
        private string? _dialogRefCheque;

        // Visibilité des champs selon le mode
        public bool IsVirementVisible => DialogModeReglement == ModeReglement.Virement;
        public bool IsChequeVisible => DialogModeReglement == ModeReglement.Cheque;

        #endregion

        #region Propriétés observables - État

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _messageErreur = string.Empty;

        [ObservableProperty]
        private string _messageSucces = string.Empty;

        #endregion

        #region Propriétés formatées

        public string DialogMontantTotalFormate => DialogMontantTotal.ToString("N0") + " GNF";
        public string DialogMontantRestantFormate => DialogMontantRestant.ToString("N0") + " GNF";

        #endregion

        public MouvementViewModel(IMouvementService mouvementService)
        {
            _mouvementService = mouvementService;
            ExerciceService.Instance.ExerciceChanged += OnExerciceChanged;
        }

        #region Commandes - Initialisation

        /// <summary>
        /// Initialise le ViewModel et charge les données
        /// </summary>
        [RelayCommand]
        public async Task InitialiserAsync()
        {
            await ChargerDonneesAsync();
        }

        /// <summary>
        /// Charge toutes les données (mandats et ordres de recette)
        /// MODIFICATION: Meilleure gestion d'erreur avec try-catch séparé par section
        /// </summary>
        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            Debug.WriteLine("=== ChargerDonneesAsync DÉBUT ===");

            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;
                MessageSucces = string.Empty;

                // ═══════════════════════════════════════
                // Charger les mandats non payés
                // ═══════════════════════════════════════
                try
                {
                    Debug.WriteLine("Chargement des mandats...");
                    var mandats = await _mouvementService.GetMandatsNonPayesAsync();
                    MandatsNonPayes = new ObservableCollection<MandatPaiementDTO>(mandats ?? new());
                    Debug.WriteLine($"✓ Mandats chargés: {MandatsNonPayes.Count}");
                }
                catch (Exception exMandats)
                {
                    Debug.WriteLine($"❌ Erreur mandats: {exMandats.Message}");
                    Debug.WriteLine($"StackTrace: {exMandats.StackTrace}");

                    MessageErreur = $"Erreur lors du chargement des mandats : {exMandats.Message}";
                    MandatsNonPayes = new ObservableCollection<MandatPaiementDTO>();
                }

                // ═══════════════════════════════════════
                // Charger les ordres de recette non encaissés
                // ═══════════════════════════════════════
                try
                {
                    Debug.WriteLine("Chargement des ordres de recette...");
                    var ordres = await _mouvementService.GetOrdresRecetteNonEncaissesAsync();
                    OrdresRecetteNonEncaisses = new ObservableCollection<OrdreRecetteEncaissementDTO>(ordres ?? new());
                    Debug.WriteLine($"✓ Ordres chargés: {OrdresRecetteNonEncaisses.Count}");
                }
                catch (Exception exOrdres)
                {
                    Debug.WriteLine($"❌ Erreur ordres: {exOrdres.Message}");
                    Debug.WriteLine($"StackTrace: {exOrdres.StackTrace}");

                    // Seulement mettre à jour MessageErreur si pas déjà d'erreur de mandats
                    if (string.IsNullOrEmpty(MessageErreur))
                    {
                        MessageErreur = $"Erreur lors du chargement des ordres : {exOrdres.Message}";
                    }
                    else
                    {
                        MessageErreur += $"\nErreur ordres : {exOrdres.Message}";
                    }

                    OrdresRecetteNonEncaisses = new ObservableCollection<OrdreRecetteEncaissementDTO>();
                }

                Debug.WriteLine("=== ChargerDonneesAsync COMPLÉTÉ ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ERREUR GÉNÉRALE: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                MessageErreur = $"Erreur générale de chargement : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Commandes - Paiement de mandat

        /// <summary>
        /// Ouvre le dialogue de paiement pour un mandat
        /// </summary>
        [RelayCommand]
        public void OuvrirDialoguePaiement(MandatPaiementDTO? mandat)
        {
            if (mandat == null) return;

            MandatSelectionne = mandat;
            IsDialogPaiement = true;

            // Initialiser les champs du dialogue
            DialogTitre = "Paiement de mandat";
            DialogNumero = mandat.NumeroMandat;
            DialogBeneficiaire = mandat.Beneficiaire;
            DialogMontantTotal = mandat.MontantNet;
            DialogMontantRestant = mandat.MontantRestant;
            DialogDate = DateTime.Today;
            DialogMontant = mandat.MontantRestant; // Propose le montant restant par défaut
            DialogModeReglement = ModeReglement.Espece;
            DialogRefVirement = null;
            DialogNumBanque = null;
            DialogRefCheque = null;

            // Notifier les changements de visibilité
            OnPropertyChanged(nameof(IsVirementVisible));
            OnPropertyChanged(nameof(IsChequeVisible));
            OnPropertyChanged(nameof(DialogMontantTotalFormate));
            OnPropertyChanged(nameof(DialogMontantRestantFormate));

            IsDialogOpen = true;
        }

        /// <summary>
        /// Valide le paiement du mandat
        /// </summary>
        [RelayCommand]
        public async Task ValiderPaiementAsync()
        {
            if (MandatSelectionne == null) return;

            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                // Valider les champs selon le mode
                if (!ValiderChampsMouvement())
                {
                    IsLoading = false;
                    return;
                }

                var dto = new MouvementCreationDTO
                {
                    Date = DateOnly.FromDateTime(DialogDate),
                    Montant = DialogMontant,
                    ModeReglement = DialogModeReglement,
                    RefVirement = DialogRefVirement,
                    NumBanqueBenef = DialogNumBanque,
                    RefCheque = DialogRefCheque,
                    IdMandat = MandatSelectionne.Id
                };

                var (success, message, _) = await _mouvementService.PayerMandatAsync(dto);

                if (success)
                {
                    // Fonction qui incremente l'attribut MontantEntreSortie du BugetLine Cas Mandat
                    await new BudgetLineEntreSortieService().IncrémenterPourMandatAsync(MandatSelectionne.Id, DialogMontant);

                    MessageSucces = message;
                    IsDialogOpen = false;
                    await ChargerDonneesAsync();

                    MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageErreur = message;
                    MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur : {ex.Message}";
                MessageBox.Show(MessageErreur, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Affiche l'historique des paiements d'un mandat
        /// </summary>
        [RelayCommand]
        public async Task VoirHistoriquePaiementsAsync(MandatPaiementDTO? mandat)
        {
            if (mandat == null) return;

            try
            {
                var historique = await _mouvementService.GetHistoriquePaiementsMandatAsync(mandat.Id);
                HistoriqueMouvements = new ObservableCollection<MouvementHistoriqueDTO>(historique);
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur : {ex.Message}";
            }
        }

        #endregion

        #region Commandes - Encaissement d'ordre de recette

        /// <summary>
        /// Ouvre le dialogue d'encaissement pour un ordre de recette
        /// </summary>
        [RelayCommand]
        public void OuvrirDialogueEncaissement(OrdreRecetteEncaissementDTO? ordre)
        {
            if (ordre == null) return;

            OrdreRecetteSelectionne = ordre;
            IsDialogPaiement = false;

            // Initialiser les champs du dialogue
            DialogTitre = "Encaissement d'ordre de recette";
            DialogNumero = ordre.NumeroOrdre;
            DialogBeneficiaire = ordre.Debiteur;
            DialogMontantTotal = ordre.MontantOrdre;
            DialogMontantRestant = ordre.MontantRestant;
            DialogDate = DateTime.Today;
            DialogMontant = ordre.MontantRestant; // Propose le montant restant par défaut
            DialogModeReglement = ModeReglement.Espece;
            DialogRefVirement = null;
            DialogNumBanque = null;
            DialogRefCheque = null;

            // Notifier les changements de visibilité
            OnPropertyChanged(nameof(IsVirementVisible));
            OnPropertyChanged(nameof(IsChequeVisible));
            OnPropertyChanged(nameof(DialogMontantTotalFormate));
            OnPropertyChanged(nameof(DialogMontantRestantFormate));

            IsDialogOpen = true;
        }

        /// <summary>
        /// Valide l'encaissement de l'ordre de recette
        /// </summary>
        [RelayCommand]
        public async Task ValiderEncaissementAsync()
        {
            if (OrdreRecetteSelectionne == null) return;

            try
            {
                IsLoading = true;
                MessageErreur = string.Empty;

                // Valider les champs selon le mode
                if (!ValiderChampsMouvement())
                {
                    IsLoading = false;
                    return;
                }

                var dto = new MouvementCreationDTO
                {
                    Date = DateOnly.FromDateTime(DialogDate),
                    Montant = DialogMontant,
                    ModeReglement = DialogModeReglement,
                    RefVirement = DialogRefVirement,
                    NumBanqueBenef = DialogNumBanque,
                    RefCheque = DialogRefCheque,
                    IdOrdreRecette = OrdreRecetteSelectionne.Id
                };

                var (success, message, _) = await _mouvementService.EncaisserOrdreRecetteAsync(dto);

                if (success)
                {
                    // Fonction qui incremente l'attribut MontantEntreSortie du BugetLine Cas OrdreRecette
                    await new BudgetLineEntreSortieService().IncrémenterPourOrdreRecetteAsync(OrdreRecetteSelectionne.Id, DialogMontant);

                    MessageSucces = message;
                    IsDialogOpen = false;
                    await ChargerDonneesAsync();

                    MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageErreur = message;
                    MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur : {ex.Message}";
                MessageBox.Show(MessageErreur, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Affiche l'historique des encaissements d'un ordre de recette
        /// </summary>
        [RelayCommand]
        public async Task VoirHistoriqueEncaissementsAsync(OrdreRecetteEncaissementDTO? ordre)
        {
            if (ordre == null) return;

            try
            {
                var historique = await _mouvementService.GetHistoriqueEncaissementsOrdreRecetteAsync(ordre.Id);
                HistoriqueMouvements = new ObservableCollection<MouvementHistoriqueDTO>(historique);
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur : {ex.Message}";
            }
        }

        #endregion

        #region Commandes - Dialogue

        /// <summary>
        /// Ferme le dialogue
        /// </summary>
        [RelayCommand]
        public void FermerDialogue()
        {
            IsDialogOpen = false;
            MandatSelectionne = null;
            OrdreRecetteSelectionne = null;
        }

        /// <summary>
        /// Valide le mouvement (paiement ou encaissement)
        /// </summary>
        [RelayCommand]
        public async Task ValiderMouvementAsync()
        {
            if (IsDialogPaiement)
            {
                await ValiderPaiementAsync();
            }
            else
            {
                await ValiderEncaissementAsync();
            }
        }

        #endregion

        #region Méthodes privées

        // ════════════════════════════════════════════════════════════
        // MÉTHODE APPELÉE QUAND L'EXERCICE CHANGE
        // ════════════════════════════════════════════════════════════
        private async void OnExerciceChanged(object? sender, Exercice exercice)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await ChargerDonneesAsync();
            });
        }
        // ════════════════════════════════════════════════════════════
        // MÉTHODE POUR SE DÉSABONNER (éviter les fuites mémoire)
        // ════════════════════════════════════════════════════════════
        public void Cleanup()
        {
            ExerciceService.Instance.ExerciceChanged -= OnExerciceChanged;
        }
        /// <summary>
        /// Valide les champs du formulaire de mouvement
        /// </summary>
        private bool ValiderChampsMouvement()
        {
            if (DialogMontant <= 0)
            {
                MessageErreur = "Le montant doit être supérieur à zéro.";
                MessageBox.Show(MessageErreur, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (DialogMontant > DialogMontantRestant)
            {
                MessageErreur = $"Le montant ne peut pas dépasser le montant restant ({DialogMontantRestant:N0} GNF).";
                MessageBox.Show(MessageErreur, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validation spécifique au mode de règlement
            switch (DialogModeReglement)
            {
                case ModeReglement.Virement:
                    if (string.IsNullOrWhiteSpace(DialogRefVirement))
                    {
                        MessageErreur = "La référence du virement est obligatoire.";
                        MessageBox.Show(MessageErreur, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    break;

                case ModeReglement.Cheque:
                    if (string.IsNullOrWhiteSpace(DialogRefCheque))
                    {
                        MessageErreur = "La référence du chèque est obligatoire.";
                        MessageBox.Show(MessageErreur, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    break;
            }

            return true;
        }

        #endregion

        #region Handlers de changement

        partial void OnDialogModeReglementChanged(ModeReglement value)
        {
            OnPropertyChanged(nameof(IsVirementVisible));
            OnPropertyChanged(nameof(IsChequeVisible));
        }

        #endregion
    }
}