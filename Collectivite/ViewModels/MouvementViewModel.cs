using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    public partial class MouvementViewModel : ObservableObject
    {
        private readonly IMouvementService _mouvementService;
        private readonly string _logPath = Path.Combine(Path.GetTempPath(), "collectivite_mouvement.log");

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

        [ObservableProperty]
        private string _messageErreurSolde = string.Empty;

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
            Log("MouvementViewModel.InitialiserAsync - start");
            try
            {
                await ChargerDonneesAsync();
                Log("MouvementViewModel.InitialiserAsync - done");
            }
            catch (Exception ex)
            {
                Log($"MouvementViewModel.InitialiserAsync - error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Charge toutes les données (mandats et ordres de recette)
        /// MODIFICATION: Meilleure gestion d'erreur avec try-catch séparé par section
        /// </summary>
        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            Debug.WriteLine("=== ChargerDonneesAsync DÉBUT ===");
            Log("MouvementViewModel.ChargerDonneesAsync - start");

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
                    Log("ChargerDonneesAsync - mandats: start");
                    Debug.WriteLine("Chargement des mandats...");
                    var mandats = await _mouvementService.GetMandatsNonPayesAsync();
                    MandatsNonPayes = new ObservableCollection<MandatPaiementDTO>(mandats ?? new());
                    Debug.WriteLine($"✓ Mandats chargés: {MandatsNonPayes.Count}");
                    Log($"ChargerDonneesAsync - mandats: ok ({MandatsNonPayes.Count})");
                }
                catch (Exception exMandats)
                {
                    Debug.WriteLine($"❌ Erreur mandats: {exMandats.Message}");
                    Debug.WriteLine($"StackTrace: {exMandats.StackTrace}");
                    Log($"ChargerDonneesAsync - mandats: error {exMandats.Message}\n{exMandats.StackTrace}");

                    MessageErreur = $"Erreur lors du chargement des mandats : {exMandats.Message}";
                    MandatsNonPayes = new ObservableCollection<MandatPaiementDTO>();
                }

                // ═══════════════════════════════════════
                // Charger les ordres de recette non encaissés
                // ═══════════════════════════════════════
                try
                {
                    Log("ChargerDonneesAsync - ordres: start");
                    Debug.WriteLine("Chargement des ordres de recette...");
                    var ordres = await _mouvementService.GetOrdresRecetteNonEncaissesAsync();
                    OrdresRecetteNonEncaisses = new ObservableCollection<OrdreRecetteEncaissementDTO>(ordres ?? new());
                    Debug.WriteLine($"✓ Ordres chargés: {OrdresRecetteNonEncaisses.Count}");
                    Log($"ChargerDonneesAsync - ordres: ok ({OrdresRecetteNonEncaisses.Count})");
                }
                catch (Exception exOrdres)
                {
                    Debug.WriteLine($"❌ Erreur ordres: {exOrdres.Message}");
                    Debug.WriteLine($"StackTrace: {exOrdres.StackTrace}");
                    Log($"ChargerDonneesAsync - ordres: error {exOrdres.Message}\n{exOrdres.StackTrace}");

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
                Log("MouvementViewModel.ChargerDonneesAsync - done");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ERREUR GÉNÉRALE: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                MessageErreur = $"Erreur générale de chargement : {ex.Message}";
                Log($"MouvementViewModel.ChargerDonneesAsync - error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
                Log("MouvementViewModel.ChargerDonneesAsync - finally");
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
                MessageErreurSolde = string.Empty;

                // Valider les champs selon le mode
                if (!ValiderChampsMouvement())
                {
                    IsLoading = false;
                    return;
                }

                // ═══════════════════════════════════════
                // VÉRIFICATION DU SOLDE DU COMPTE
                // ═══════════════════════════════════════
                try
                {
                    // Déterminer le numéro de compte selon le mode de règlement
                    string numeroCompte = DialogModeReglement switch
                    {
                        ModeReglement.Espece => "55",      // Caisse
                        ModeReglement.Virement => "53",    // Banque
                        ModeReglement.Cheque => "53",      // Banque
                        _ => "55"
                    };

                    // Récupérer le solde actuel du compte (toutes les écritures)
                    decimal solde = await _mouvementService.GetSoldeCompteParNumeroAsync(numeroCompte);

                    // Déterminer le nom du compte pour le message
                    string nomCompte = DialogModeReglement switch
                    {
                        ModeReglement.Espece => "Caisse (55)",
                        ModeReglement.Virement => "Banque (53)",
                        ModeReglement.Cheque => "Banque (53)",
                        _ => "Caisse (55)"
                    };

                    // Vérifier si le solde est suffisant
                    if (solde < DialogMontant)
                    {
                        string messageErreur = $"❌ Paiement bloqué : Solde insuffisant\n\n" +
                                              $"Le solde du compte {nomCompte} est de {solde:N0} GNF, " +
                                              $"ce qui est inférieur au montant à payer ({DialogMontant:N0} GNF).\n\n" +
                                              $"Veuillez vérifier votre trésorerie avant de procéder au paiement.";

                        MessageErreurSolde = messageErreur;
                        NotificationService.ShowWarning(messageErreur);
                        IsLoading = false;
                        return;
                    }
                }
                catch (Exception exSolde)
                {
                    string messageErreur = $"Erreur lors de la vérification du solde : {exSolde.Message}";
                    MessageErreurSolde = messageErreur;
                    NotificationService.ShowError(messageErreur);
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

                    NotificationService.ShowSuccess(message);
                }
                else
                {
                    MessageErreur = message;
                    NotificationService.ShowWarning(message);
                }
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur : {ex.Message}";
                NotificationService.ShowError(MessageErreur);
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

                    NotificationService.ShowSuccess(message);
                }
                else
                {
                    MessageErreur = message;
                    NotificationService.ShowWarning(message);
                }
            }
            catch (Exception ex)
            {
                MessageErreur = $"Erreur : {ex.Message}";
                NotificationService.ShowError(MessageErreur);
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

        #region Commandes - Menu détails

        [RelayCommand]
        public void OuvrirDetailsEngagement(int engagementId)
        {
            if (engagementId <= 0) return;
            NavigationService.Instance.NavigateTo(new Views.Pages.EngagementDetailPage(engagementId));
        }

        [RelayCommand]
        public void OuvrirDetailsFacture(int factureId)
        {
            if (factureId <= 0) return;
            NavigationService.Instance.NavigateTo(new Views.Pages.FactureDetailsPage(factureId));
        }

        [RelayCommand]
        public void OuvrirDetailsBonCommande(int bonCommandeId)
        {
            if (bonCommandeId <= 0) return;
            NavigationService.Instance.NavigateTo(new Views.Pages.BonCommandeDetailsPage(bonCommandeId));
        }

        [RelayCommand]
        public void OuvrirDetailsOrdreRecette(int ordreRecetteId)
        {
            if (ordreRecetteId <= 0) return;
            NavigationService.Instance.NavigateTo(new Views.Pages.OrdreRecetteDetailPage(ordreRecetteId));
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
                NotificationService.ShowWarning(MessageErreur);
                return false;
            }

            if (DialogMontant > DialogMontantRestant)
            {
                MessageErreur = $"Le montant ne peut pas dépasser le montant restant ({DialogMontantRestant:N0} GNF).";
                NotificationService.ShowWarning(MessageErreur);
                return false;
            }

            // Validation spécifique au mode de règlement
            switch (DialogModeReglement)
            {
                case ModeReglement.Virement:
                    if (string.IsNullOrWhiteSpace(DialogRefVirement))
                    {
                        MessageErreur = "La référence du virement est obligatoire.";
                        NotificationService.ShowWarning(MessageErreur);
                        return false;
                    }
                    break;

                case ModeReglement.Cheque:
                    if (string.IsNullOrWhiteSpace(DialogRefCheque))
                    {
                        MessageErreur = "La référence du chèque est obligatoire.";
                        NotificationService.ShowWarning(MessageErreur);
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
            
            // Vérifier le solde uniquement pour les paiements de mandats
            if (IsDialogPaiement)
            {
                _ = VerifierSoldeCompteAsync();
            }
        }

        partial void OnDialogMontantChanged(decimal value)
        {
            // Vérifier le solde uniquement pour les paiements de mandats
            if (IsDialogPaiement && value > 0)
            {
                _ = VerifierSoldeCompteAsync();
            }
        }

        /// <summary>
        /// Vérifie le solde du compte de trésorerie selon le mode de règlement
        /// Uniquement pour les paiements de mandats
        /// </summary>
        private async Task VerifierSoldeCompteAsync()
        {
            // Ne vérifier que pour les paiements de mandats
            if (!IsDialogPaiement || DialogMontant <= 0)
            {
                MessageErreurSolde = string.Empty;
                return;
            }

            try
            {
                // Déterminer le numéro de compte selon le mode de règlement
                string numeroCompte = DialogModeReglement switch
                {
                    ModeReglement.Espece => "55",      // Caisse
                    ModeReglement.Virement => "53",    // Banque
                    ModeReglement.Cheque => "53",      // Banque
                    _ => "55"
                };

                // Récupérer le solde actuel du compte (toutes les écritures)
                decimal solde = await _mouvementService.GetSoldeCompteParNumeroAsync(numeroCompte);

                // Déterminer le nom du compte pour le message
                string nomCompte = DialogModeReglement switch
                {
                    ModeReglement.Espece => "Caisse (55)",
                    ModeReglement.Virement => "Banque (53)",
                    ModeReglement.Cheque => "Banque (53)",
                    _ => "Caisse (55)"
                };

                // Vérifier si le solde est suffisant
                // Pour un paiement, on débite le compte de trésorerie (on crédite)
                // Le solde doit être suffisant pour supporter le paiement
                // Si le compte est débiteur (solde positif), on peut payer
                // Si le compte est créditeur (solde négatif), on doit vérifier que le solde + montant reste acceptable
                
                // Pour un compte de trésorerie (actif), un solde débiteur (positif) est normal
                // Un paiement crédite le compte, donc réduit le solde débiteur
                // On vérifie que le solde débiteur est suffisant pour le paiement
                
                if (solde < DialogMontant)
                {
                    MessageErreurSolde = $"⚠️ Solde insuffisant : Le solde du compte {nomCompte} est de {solde:N0} GNF, " +
                                        $"ce qui est inférieur au montant à payer ({DialogMontant:N0} GNF). " +
                                        $"Veuillez vérifier votre trésorerie avant de procéder au paiement.";
                }
                else
                {
                    MessageErreurSolde = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageErreurSolde = $"Erreur lors de la vérification du solde : {ex.Message}";
            }
        }

        private void Log(string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}";
            Debug.WriteLine(line);
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch
            {
                // ignorer les erreurs d'écriture de log
            }
        }

        #endregion
    }
}