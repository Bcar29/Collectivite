using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class detailsCommuneOptionnelDansExercice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                table: "BudgetsPrimitifs");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                table: "BudgetsPrimitifs",
                column: "ExerciceId",
                principalTable: "Exercices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                table: "BudgetsPrimitifs");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                table: "BudgetsPrimitifs",
                column: "ExerciceId",
                principalTable: "Exercices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
