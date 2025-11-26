using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class modificationBudgetPrimitif : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Montant",
                table: "BudgetsPrimitifs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "DateVote",
                table: "BudgetsPrimitifs",
                newName: "DateValidation");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateApprobation",
                table: "BudgetsPrimitifs",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "MontantDepense",
                table: "BudgetsPrimitifs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MontantRecette",
                table: "BudgetsPrimitifs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MontantTotal",
                table: "BudgetsPrimitifs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateApprobation",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropColumn(
                name: "MontantDepense",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropColumn(
                name: "MontantRecette",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropColumn(
                name: "MontantTotal",
                table: "BudgetsPrimitifs");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "BudgetsPrimitifs",
                newName: "Montant");

            migrationBuilder.RenameColumn(
                name: "DateValidation",
                table: "BudgetsPrimitifs",
                newName: "DateVote");
        }
    }
}
