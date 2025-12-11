using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class AjoutStatutMandat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_mouvement_Mandats_MandatId",
                table: "mouvement");

            migrationBuilder.DropIndex(
                name: "IX_mouvement_MandatId",
                table: "mouvement");

            migrationBuilder.DropColumn(
                name: "MandatId",
                table: "mouvement");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MandatId",
                table: "mouvement",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_mouvement_MandatId",
                table: "mouvement",
                column: "MandatId");

            migrationBuilder.AddForeignKey(
                name: "FK_mouvement_Mandats_MandatId",
                table: "mouvement",
                column: "MandatId",
                principalTable: "Mandats",
                principalColumn: "Id");
        }
    }
}
