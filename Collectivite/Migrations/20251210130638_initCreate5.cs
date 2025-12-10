using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class initCreate5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EcritureComptables_Mouvement_MouvementId",
                table: "EcritureComptables");

            migrationBuilder.DropForeignKey(
                name: "FK_Mouvement_CompteComptables_idCompteComptable",
                table: "Mouvement");

            migrationBuilder.DropForeignKey(
                name: "FK_Mouvement_Mandats_idMandat",
                table: "Mouvement");

            migrationBuilder.DropForeignKey(
                name: "FK_Mouvement_OrdreRecettes_idOrdreRecette",
                table: "Mouvement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Mouvement",
                table: "Mouvement");

            migrationBuilder.RenameTable(
                name: "Mouvement",
                newName: "mouvement");

            migrationBuilder.RenameIndex(
                name: "IX_Mouvement_idOrdreRecette",
                table: "mouvement",
                newName: "IX_mouvement_idOrdreRecette");

            migrationBuilder.RenameIndex(
                name: "IX_Mouvement_idMandat",
                table: "mouvement",
                newName: "IX_mouvement_idMandat");

            migrationBuilder.RenameIndex(
                name: "IX_Mouvement_idCompteComptable",
                table: "mouvement",
                newName: "IX_mouvement_idCompteComptable");

            migrationBuilder.AddColumn<int>(
                name: "CommuneType",
                table: "Communes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_mouvement",
                table: "mouvement",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_EcritureComptables_mouvement_MouvementId",
                table: "EcritureComptables",
                column: "MouvementId",
                principalTable: "mouvement",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_mouvement_CompteComptables_idCompteComptable",
                table: "mouvement",
                column: "idCompteComptable",
                principalTable: "CompteComptables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_mouvement_Mandats_idMandat",
                table: "mouvement",
                column: "idMandat",
                principalTable: "Mandats",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_mouvement_OrdreRecettes_idOrdreRecette",
                table: "mouvement",
                column: "idOrdreRecette",
                principalTable: "OrdreRecettes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EcritureComptables_mouvement_MouvementId",
                table: "EcritureComptables");

            migrationBuilder.DropForeignKey(
                name: "FK_mouvement_CompteComptables_idCompteComptable",
                table: "mouvement");

            migrationBuilder.DropForeignKey(
                name: "FK_mouvement_Mandats_idMandat",
                table: "mouvement");

            migrationBuilder.DropForeignKey(
                name: "FK_mouvement_OrdreRecettes_idOrdreRecette",
                table: "mouvement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_mouvement",
                table: "mouvement");

            migrationBuilder.DropColumn(
                name: "CommuneType",
                table: "Communes");

            migrationBuilder.RenameTable(
                name: "mouvement",
                newName: "Mouvement");

            migrationBuilder.RenameIndex(
                name: "IX_mouvement_idOrdreRecette",
                table: "Mouvement",
                newName: "IX_Mouvement_idOrdreRecette");

            migrationBuilder.RenameIndex(
                name: "IX_mouvement_idMandat",
                table: "Mouvement",
                newName: "IX_Mouvement_idMandat");

            migrationBuilder.RenameIndex(
                name: "IX_mouvement_idCompteComptable",
                table: "Mouvement",
                newName: "IX_Mouvement_idCompteComptable");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Mouvement",
                table: "Mouvement",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_EcritureComptables_Mouvement_MouvementId",
                table: "EcritureComptables",
                column: "MouvementId",
                principalTable: "Mouvement",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mouvement_CompteComptables_idCompteComptable",
                table: "Mouvement",
                column: "idCompteComptable",
                principalTable: "CompteComptables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mouvement_Mandats_idMandat",
                table: "Mouvement",
                column: "idMandat",
                principalTable: "Mandats",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Mouvement_OrdreRecettes_idOrdreRecette",
                table: "Mouvement",
                column: "idOrdreRecette",
                principalTable: "OrdreRecettes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
