using Collectivite.Services;
using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour DetailCommunePage.xaml
    /// </summary>
    public partial class DetailCommunePage : Page
    {
        /// <summary>
        /// Constructeur par défaut (affiche tous les détails)
        /// </summary>
        public DetailCommunePage() : this(null)
        {
        }

        /// <summary>
        /// Constructeur avec filtre de commune
        /// </summary>
        /// <param name="communeId">ID de la commune à filtrer (null = tous)</param>
        public DetailCommunePage(int? communeId)
        {
            InitializeComponent();

            // Initialiser le contexte et les services
            var context = new AppDbContext();
            var detailCommuneService = new DetailCommuneService(context);
            var communeService = new CommuneService(context);

            // Créer le ViewModel avec le filtre de commune
            var viewModel = new DetailCommuneViewModel(
                detailCommuneService,
                communeService,
                communeId);

            // Définir le DataContext
            DataContext = viewModel;

            // Debug
            if (communeId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"DetailCommunePage ouverte pour la commune ID: {communeId.Value}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DetailCommunePage ouverte pour toutes les communes");
            }
        }
    }
}