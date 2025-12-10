using Collectivite.Services;
using System.Windows;

namespace Collectivite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new AppDbContext();

            // Seed strictement fonctionnel (nomenclature budgétaire)
            SeedNomenclature.Seed(db);

            // Seed optionnel des rôles/permissions :
            // - crée un catalogue initial de permissions (CRUD par modèle, Budget.Approve, Budget.Validate, etc.)
            // - crée quelques rôles de base (Maire, Secrétaire Général, Receveur)
            // Le Maire reste libre de modifier/supprimer/ignorer ces éléments via l'UI.
            Utils.SeedRolesPermissions.Seed(db);
        }
    }

}
