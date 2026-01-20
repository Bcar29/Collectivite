using Collectivite.Services;
using Collectivite.Utils;
using System;
using System.Windows;

namespace Collectivite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Vérification de la licence AVANT toute initialisation lourde
            var licenseManager = new LicenseManager();
            if (!licenseManager.CheckLicense(out var _))
            {
                // Message volontairement simple, sans détailler la raison exacte
                MessageBox.Show(
                    "Erreur inatendue.",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            try
            {
                using var db = new AppDbContext();

                // Initialisation de la base de données
                await db.Database.EnsureCreatedAsync();

                // Seeds dans l'ordre
                SeedNomenclature.Seed(db);
                Utils.SeedRolesPermissions.Seed(db);

                // Seed du plan comptable
                var seedPlanComptable = new SeedPlanComptable(db);
                await seedPlanComptable.SeedCompteComptablesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur d'initialisation : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}