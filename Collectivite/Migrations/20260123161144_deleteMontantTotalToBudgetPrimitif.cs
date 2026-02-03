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
            migrationBuilder.DropColumn(
                name: "MontantTotal",
                table: "BudgetsPrimitifs");
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
