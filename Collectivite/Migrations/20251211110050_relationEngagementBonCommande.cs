using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class relationEngagementBonCommande : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonCommandes_Engagements_EngagementId",
                table: "BonCommandes");

            migrationBuilder.DropColumn(
                name: "FichierJoin",
                table: "BonCommandes");

            migrationBuilder.RenameColumn(
                name: "EngagementId",
                table: "BonCommandes",
                newName: "ExpressionBesoinId");

            migrationBuilder.RenameIndex(
                name: "IX_BonCommandes_EngagementId",
                table: "BonCommandes",
                newName: "IX_BonCommandes_ExpressionBesoinId");

            migrationBuilder.AddColumn<int>(
                name: "BonCommandeId",
                table: "Engagements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_BonCommandeId",
                table: "Engagements",
                column: "BonCommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_BonCommandes_Numero",
                table: "BonCommandes",
                column: "Numero",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BonCommandes_ExpressionBesoins_ExpressionBesoinId",
                table: "BonCommandes",
                column: "ExpressionBesoinId",
                principalTable: "ExpressionBesoins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Engagements_BonCommandes_BonCommandeId",
                table: "Engagements",
                column: "BonCommandeId",
                principalTable: "BonCommandes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonCommandes_ExpressionBesoins_ExpressionBesoinId",
                table: "BonCommandes");

            migrationBuilder.DropForeignKey(
                name: "FK_Engagements_BonCommandes_BonCommandeId",
                table: "Engagements");

            migrationBuilder.DropIndex(
                name: "IX_Engagements_BonCommandeId",
                table: "Engagements");

            migrationBuilder.DropIndex(
                name: "IX_BonCommandes_Numero",
                table: "BonCommandes");

            migrationBuilder.DropColumn(
                name: "BonCommandeId",
                table: "Engagements");

            migrationBuilder.RenameColumn(
                name: "ExpressionBesoinId",
                table: "BonCommandes",
                newName: "EngagementId");

            migrationBuilder.RenameIndex(
                name: "IX_BonCommandes_ExpressionBesoinId",
                table: "BonCommandes",
                newName: "IX_BonCommandes_EngagementId");

            migrationBuilder.AddColumn<byte[]>(
                name: "FichierJoin",
                table: "BonCommandes",
                type: "longblob",
                nullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_BonCommandes_Engagements_EngagementId",
                table: "BonCommandes",
                column: "EngagementId",
                principalTable: "Engagements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
