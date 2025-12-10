using Collectivite.Models;
using Collectivite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour ExpressionBesoinFormPage.xaml
    /// </summary>
    public partial class ExpressionBesoinFormPage : Page
    {
        public ExpressionBesoinFormPage(int? expressionBesoinId = null)
        {
            InitializeComponent();
            DataContext = new ExpressionBesoinFormViewModel(expressionBesoinId);
        }
    }
}
