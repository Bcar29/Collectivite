using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class initCreate_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MouvementId",
                table: "EcritureComptables",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EcritureComptables_MouvementId",
                table: "EcritureComptables",
                column: "MouvementId");

            migrationBuilder.AddForeignKey(
                name: "FK_EcritureComptables_Mouvement_MouvementId",
                table: "EcritureComptables",
                column: "MouvementId",
                principalTable: "Mouvement",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EcritureComptables_Mouvement_MouvementId",
                table: "EcritureComptables");

            migrationBuilder.DropIndex(
                name: "IX_EcritureComptables_MouvementId",
                table: "EcritureComptables");

            migrationBuilder.DropColumn(
                name: "MouvementId",
                table: "EcritureComptables");
        }
    }
}
