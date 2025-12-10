
using Collectivite.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Collectivite.Views.Pages
{
    /// <summary>
    /// Page de gestion des Droits au Comptant
    /// </summary>
    public partial class DroitAuComptantPage : Page
    {
        private readonly DroitAuComptantViewModel _viewModel;

        public DroitAuComptantPage()
        {
            InitializeComponent();

            _viewModel = new DroitAuComptantViewModel();
            DataContext = _viewModel;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.ChargerDonneesAsync();
        }
    }

    
}