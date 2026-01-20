using Collectivite.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    // ═══════════════════════════════════════════════════════════════════════════
    // PAGE BÉNÉFICIAIRES PDL
    // ═══════════════════════════════════════════════════════════════════════════
    public partial class BeneficiairePDLPage : UserControl
    {
        private readonly BeneficiairePDLViewModel _viewModel;

        public BeneficiairePDLPage()
        {
            InitializeComponent();
            _viewModel = new BeneficiairePDLViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PAGE ACTEURS PDL
    // ═══════════════════════════════════════════════════════════════════════════
    public partial class ActeurPDLPage : UserControl
    {
        private readonly ActeurPDLViewModel _viewModel;

        public ActeurPDLPage()
        {
            InitializeComponent();
            _viewModel = new ActeurPDLViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PAGE STRUCTURES D'EXÉCUTION PDL
    // ═══════════════════════════════════════════════════════════════════════════
    public partial class StructureExecutionPDLPage : UserControl
    {
        private readonly StructureExecutionPDLViewModel _viewModel;

        public StructureExecutionPDLPage()
        {
            InitializeComponent();
            _viewModel = new StructureExecutionPDLViewModel();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiserAsync();
        }
    }
}