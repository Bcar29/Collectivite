using Collectivite.Models;
using Collectivite.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.ViewModels
{
    // ═══════════════════════════════════════════════════════════════════════════
    // VIEWMODEL BÉNÉFICIAIRES PDL
    // ═══════════════════════════════════════════════════════════════════════════

    public partial class BeneficiairePDLViewModel : ObservableObject
    {
        private readonly IBeneficiairePDLService _service;

        [ObservableProperty] private ObservableCollection<BeneficiairePDL> _items = new();
        [ObservableProperty] private BeneficiairePDL? _selectedItem;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _nom = string.Empty;
        [ObservableProperty] private string _description = string.Empty;

        public int TotalItems => Items.Count;
        public string TitrePage => "Bénéficiaires PDL";

        public BeneficiairePDLViewModel() : this(new BeneficiairePDLService()) { }
        public BeneficiairePDLViewModel(IBeneficiairePDLService service) => _service = service;

        [RelayCommand]
        public async Task InitialiserAsync() => await ChargerDonneesAsync();

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                var items = await _service.GetAllAsync();
                Items = new ObservableCollection<BeneficiairePDL>(items);
                OnPropertyChanged(nameof(TotalItems));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Nouveau()
        {
            SelectedItem = new BeneficiairePDL();
            Nom = string.Empty;
            Description = string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        public void Modifier(BeneficiairePDL? item)
        {
            if (item == null) return;
            SelectedItem = item;
            Nom = item.Nom;
            Description = item.Description ?? string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        public async Task EnregistrerAsync()
        {
            if (string.IsNullOrWhiteSpace(Nom))
            {
                MessageBox.Show("Le nom est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                SelectedItem ??= new BeneficiairePDL();
                SelectedItem.Nom = Nom.Trim();
                SelectedItem.Description = Description;

                if (SelectedItem.Id == 0)
                    await _service.CreateAsync(SelectedItem);
                else
                    await _service.UpdateAsync(SelectedItem);

                IsEditing = false;
                await ChargerDonneesAsync();
                MessageBox.Show("Enregistrement réussi.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Annuler() => IsEditing = false;

        [RelayCommand]
        public async Task SupprimerAsync(BeneficiairePDL? item)
        {
            if (item == null) return;
            if (MessageBox.Show($"Supprimer \"{item.Nom}\" ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                await _service.DeleteAsync(item.Id);
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VIEWMODEL ACTEURS PDL
    // ═══════════════════════════════════════════════════════════════════════════

    public partial class ActeurPDLViewModel : ObservableObject
    {
        private readonly IActeurPDLService _service;

        [ObservableProperty] private ObservableCollection<ActeurPDL> _items = new();
        [ObservableProperty] private ActeurPDL? _selectedItem;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _nom = string.Empty;
        [ObservableProperty] private string _description = string.Empty;

        public int TotalItems => Items.Count;
        public string TitrePage => "Acteurs PDL";

        public ActeurPDLViewModel() : this(new ActeurPDLService()) { }
        public ActeurPDLViewModel(IActeurPDLService service) => _service = service;

        [RelayCommand]
        public async Task InitialiserAsync() => await ChargerDonneesAsync();

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                var items = await _service.GetAllAsync();
                Items = new ObservableCollection<ActeurPDL>(items);
                OnPropertyChanged(nameof(TotalItems));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Nouveau()
        {
            SelectedItem = new ActeurPDL();
            Nom = string.Empty;
            Description = string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        public void Modifier(ActeurPDL? item)
        {
            if (item == null) return;
            SelectedItem = item;
            Nom = item.Nom;
            Description = item.Description ?? string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        public async Task EnregistrerAsync()
        {
            if (string.IsNullOrWhiteSpace(Nom))
            {
                MessageBox.Show("Le nom est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                SelectedItem ??= new ActeurPDL();
                SelectedItem.Nom = Nom.Trim();
                SelectedItem.Description = Description;

                if (SelectedItem.Id == 0)
                    await _service.CreateAsync(SelectedItem);
                else
                    await _service.UpdateAsync(SelectedItem);

                IsEditing = false;
                await ChargerDonneesAsync();
                MessageBox.Show("Enregistrement réussi.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Annuler() => IsEditing = false;

        [RelayCommand]
        public async Task SupprimerAsync(ActeurPDL? item)
        {
            if (item == null) return;
            if (MessageBox.Show($"Supprimer \"{item.Nom}\" ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                await _service.DeleteAsync(item.Id);
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VIEWMODEL STRUCTURES D'EXÉCUTION PDL
    // ═══════════════════════════════════════════════════════════════════════════

    public partial class StructureExecutionPDLViewModel : ObservableObject
    {
        private readonly IStructureExecutionPDLService _service;

        [ObservableProperty] private ObservableCollection<StructureExecutionPDL> _items = new();
        [ObservableProperty] private StructureExecutionPDL? _selectedItem;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _nom = string.Empty;
        [ObservableProperty] private string _description = string.Empty;

        public int TotalItems => Items.Count;
        public string TitrePage => "Structures d'Exécution PDL";

        public StructureExecutionPDLViewModel() : this(new StructureExecutionPDLService()) { }
        public StructureExecutionPDLViewModel(IStructureExecutionPDLService service) => _service = service;

        [RelayCommand]
        public async Task InitialiserAsync() => await ChargerDonneesAsync();

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                var items = await _service.GetAllAsync();
                Items = new ObservableCollection<StructureExecutionPDL>(items);
                OnPropertyChanged(nameof(TotalItems));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Nouveau()
        {
            SelectedItem = new StructureExecutionPDL();
            Nom = string.Empty;
            Description = string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        public void Modifier(StructureExecutionPDL? item)
        {
            if (item == null) return;
            SelectedItem = item;
            Nom = item.Nom;
            Description = item.Description ?? string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        public async Task EnregistrerAsync()
        {
            if (string.IsNullOrWhiteSpace(Nom))
            {
                MessageBox.Show("Le nom est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                SelectedItem ??= new StructureExecutionPDL();
                SelectedItem.Nom = Nom.Trim();
                SelectedItem.Description = Description;

                if (SelectedItem.Id == 0)
                    await _service.CreateAsync(SelectedItem);
                else
                    await _service.UpdateAsync(SelectedItem);

                IsEditing = false;
                await ChargerDonneesAsync();
                MessageBox.Show("Enregistrement réussi.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Annuler() => IsEditing = false;

        [RelayCommand]
        public async Task SupprimerAsync(StructureExecutionPDL? item)
        {
            if (item == null) return;
            if (MessageBox.Show($"Supprimer \"{item.Nom}\" ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                await _service.DeleteAsync(item.Id);
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VIEWMODEL ODD (Objectifs de Développement Durable)
    // ═══════════════════════════════════════════════════════════════════════════

    public partial class ODDViewModel : ObservableObject
    {
        private readonly IODDService _service;

        [ObservableProperty] private ObservableCollection<ODD> _items = new();
        [ObservableProperty] private ODD? _selectedItem;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _numero = string.Empty;
        [ObservableProperty] private string _description = string.Empty;

        public int TotalItems => Items.Count;
        public string TitrePage => "Objectifs de Développement Durable (ODD)";

        public ODDViewModel() : this(new ODDService()) { }
        public ODDViewModel(IODDService service) => _service = service;

        [RelayCommand]
        public async Task InitialiserAsync() => await ChargerDonneesAsync();

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                var items = await _service.GetAllAsync();
                Items = new ObservableCollection<ODD>(items);
                OnPropertyChanged(nameof(TotalItems));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Nouveau()
        {
            SelectedItem = new ODD();
            Numero = string.Empty;
            Description = string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        public void Modifier(ODD? item)
        {
            if (item == null) return;
            SelectedItem = item;
            Numero = item.Numero;
            Description = item.Description;
            IsEditing = true;
        }

        [RelayCommand]
        public async Task EnregistrerAsync()
        {
            if (string.IsNullOrWhiteSpace(Numero) || string.IsNullOrWhiteSpace(Description))
            {
                MessageBox.Show("Le numéro et la description sont obligatoires.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                SelectedItem ??= new ODD();
                SelectedItem.Numero = Numero.Trim();
                SelectedItem.Description = Description.Trim();

                if (SelectedItem.Id == 0)
                    await _service.CreateAsync(SelectedItem);
                else
                    await _service.UpdateAsync(SelectedItem);

                IsEditing = false;
                await ChargerDonneesAsync();
                MessageBox.Show("Enregistrement réussi.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Annuler() => IsEditing = false;

        [RelayCommand]
        public async Task SupprimerAsync(ODD? item)
        {
            if (item == null) return;
            if (MessageBox.Show($"Supprimer l'ODD \"{item.Numero}\" ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                await _service.DeleteAsync(item.Id);
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VIEWMODEL COMPÉTENCES COLLECTIVITÉ
    // ═══════════════════════════════════════════════════════════════════════════

    public partial class CompetenceCollectiviteViewModel : ObservableObject
    {
        private readonly ICompetenceCollectiviteService _service;

        [ObservableProperty] private ObservableCollection<CompetenceCollectivite> _items = new();
        [ObservableProperty] private CompetenceCollectivite? _selectedItem;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _numero = string.Empty;
        [ObservableProperty] private string _description = string.Empty;

        public int TotalItems => Items.Count;
        public string TitrePage => "Compétences des Collectivités";

        public CompetenceCollectiviteViewModel() : this(new CompetenceCollectiviteService()) { }
        public CompetenceCollectiviteViewModel(ICompetenceCollectiviteService service) => _service = service;

        [RelayCommand]
        public async Task InitialiserAsync() => await ChargerDonneesAsync();

        [RelayCommand]
        public async Task ChargerDonneesAsync()
        {
            try
            {
                IsLoading = true;
                var items = await _service.GetAllAsync();
                Items = new ObservableCollection<CompetenceCollectivite>(items);
                OnPropertyChanged(nameof(TotalItems));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Nouveau()
        {
            SelectedItem = new CompetenceCollectivite();
            Numero = string.Empty;
            Description = string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        public void Modifier(CompetenceCollectivite? item)
        {
            if (item == null) return;
            SelectedItem = item;
            Numero = item.Numero;
            Description = item.Description;
            IsEditing = true;
        }

        [RelayCommand]
        public async Task EnregistrerAsync()
        {
            if (string.IsNullOrWhiteSpace(Numero) || string.IsNullOrWhiteSpace(Description))
            {
                MessageBox.Show("Le numéro et la description sont obligatoires.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                SelectedItem ??= new CompetenceCollectivite();
                SelectedItem.Numero = Numero.Trim();
                SelectedItem.Description = Description.Trim();

                if (SelectedItem.Id == 0)
                    await _service.CreateAsync(SelectedItem);
                else
                    await _service.UpdateAsync(SelectedItem);

                IsEditing = false;
                await ChargerDonneesAsync();
                MessageBox.Show("Enregistrement réussi.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public void Annuler() => IsEditing = false;

        [RelayCommand]
        public async Task SupprimerAsync(CompetenceCollectivite? item)
        {
            if (item == null) return;
            if (MessageBox.Show($"Supprimer la compétence \"{item.Numero}\" ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                await _service.DeleteAsync(item.Id);
                await ChargerDonneesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }
    }
}