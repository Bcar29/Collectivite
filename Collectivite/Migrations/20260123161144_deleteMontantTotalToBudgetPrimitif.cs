using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class deleteMontantTotalToBudgetPrimitif : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cette migration n'a jamais été appliquée avant (le démarrage de l'app n'appelle
            // que EnsureCreatedAsync(), jamais Migrate()) : sur certaines bases, cette colonne a
            // déjà été retirée manuellement (ou n'a jamais existé). "DROP COLUMN IF EXISTS"
            // n'étant pas supporté par toutes les versions de MySQL, on vérifie via
            // information_schema avant d'exécuter le DROP, pour ne pas échouer si elle est déjà
            // absente.
            migrationBuilder.Sql(@"
                SET @col_exists = (
                    SELECT COUNT(*) FROM information_schema.columns
                    WHERE table_schema = DATABASE() AND table_name = 'BudgetsPrimitifs' AND column_name = 'MontantTotal'
                );
                SET @stmt = IF(@col_exists > 0,
                    'ALTER TABLE `BudgetsPrimitifs` DROP COLUMN `MontantTotal`',
                    'SELECT 1');
                PREPARE stmt FROM @stmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontantTotal",
                table: "BudgetsPrimitifs",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
