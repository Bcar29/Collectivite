using Collectivite.ViewModels;
using Collectivite.Services;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour RecensementPage.xaml
    /// </summary>
    public partial class RecensementPage : Page
    {
        public RecensementPage()
        {
            InitializeComponent();
            var service = new RecensementService();
            DataContext = new RecensementViewModel(service);
        }
    }
}