using Collectivite.ViewModels;
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
            DataContext = new RecensementViewModel();
        }
    }
}