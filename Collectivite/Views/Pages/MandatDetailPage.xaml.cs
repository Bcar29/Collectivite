using Collectivite.ViewModels;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour MandatDetailPage.xaml
    /// </summary>
    public partial class MandatDetailPage : Page
    {
        public MandatDetailPage(int mandatId)
        {
            InitializeComponent();
            DataContext = new MandatDetailViewModel(mandatId);
        }
    }
}