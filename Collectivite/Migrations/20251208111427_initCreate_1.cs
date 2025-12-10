using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class initCreate_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mouvement",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FichierJoint = table.Column<byte[]>(type: "longblob", nullable: true),
                    FileName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefVirement = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumBanqueBenef = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefChèque = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    idCompteComptable = table.Column<int>(type: "int", nullable: false),
                    idOrdreRecette = table.Column<int>(type: "int", nullable: true),
                    idMandat = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mouvement", x => x.id);
                    table.ForeignKey(
                        name: "FK_Mouvement_CompteComptables_idCompteComptable",
                        column: x => x.idCompteComptable,
                        principalTable: "CompteComptables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mouvement_Mandats_idMandat",
                        column: x => x.idMandat,
                        principalTable: "Mandats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Mouvement_OrdreRecettes_idOrdreRecette",
                        column: x => x.idOrdreRecette,
                        principalTable: "OrdreRecettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Mouvement_idCompteComptable",
                table: "Mouvement",
                column: "idCompteComptable");

            migrationBuilder.CreateIndex(
                name: "IX_Mouvement_idMandat",
                table: "Mouvement",
                column: "idMandat");

            migrationBuilder.CreateIndex(
                name: "IX_Mouvement_idOrdreRecette",
                table: "Mouvement",
                column: "idOrdreRecette");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mouvement");
        }
    }
}
