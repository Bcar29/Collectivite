using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class PBchamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateApprobation",
                table: "BudgetsPrimitifs",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<byte[]>(
                name: "FichierValidation",
                table: "BudgetsPrimitifs",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "BudgetsPrimitifs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FichierValidation",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "BudgetsPrimitifs");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateApprobation",
                table: "BudgetsPrimitifs",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
