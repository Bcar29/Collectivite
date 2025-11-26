using Collectivite.Models;
using Collectivite.Services;
using Collectivite.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Logique d'interaction pour EngagementFormPage.xaml
    /// </summary>
    public partial class EngagementFormPage : Page
    {
        private readonly BudgetLineService _service;
        private readonly EngagementFormViewModel _viewModel;
        public EngagementFormPage(int? engagementId = null)
        {
            InitializeComponent();

            _service = new BudgetLineService();
            _viewModel = new EngagementFormViewModel();
            // On attend que la Page soit entièrement chargée
           DataContext = _viewModel;
        }

        
    }
}