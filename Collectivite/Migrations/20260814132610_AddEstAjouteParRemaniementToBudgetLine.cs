using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class AddEstAjouteParRemaniementToBudgetLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Engagements_Contrats_ContratId",
                table: "Engagements");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercices_PDL_PDLId",
                table: "Exercices");

            migrationBuilder.DropForeignKey(
                name: "FK_Factures_Contrats_ContratId",
                table: "Factures");

            migrationBuilder.DropTable(
                name: "ActiviteActeurs");

            migrationBuilder.DropTable(
                name: "ActiviteBeneficiaires");

            migrationBuilder.DropTable(
                name: "ActiviteStructureExecutions");

            migrationBuilder.DropTable(
                name: "Contrats");

            migrationBuilder.DropTable(
                name: "Recensements");

            migrationBuilder.DropTable(
                name: "ActeursPDL");

            migrationBuilder.DropTable(
                name: "BeneficiairesPDL");

            migrationBuilder.DropTable(
                name: "ActivitesPDL");

            migrationBuilder.DropTable(
                name: "StructureExecutionsPDL");

            migrationBuilder.DropTable(
                name: "CompetencesCollectivite");

            migrationBuilder.DropTable(
                name: "ODDs");

            migrationBuilder.DropTable(
                name: "PDL");

            migrationBuilder.DropTable(
                name: "SecteursPDL");

            migrationBuilder.DropTable(
                name: "ProgrammesPDL");

            migrationBuilder.DropIndex(
                name: "IX_Factures_ContratId",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Exercices_PDLId",
                table: "Exercices");

            migrationBuilder.DropIndex(
                name: "IX_Engagements_ContratId",
                table: "Engagements");

            migrationBuilder.DropColumn(
                name: "ContratId",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "PDLId",
                table: "Exercices");

            migrationBuilder.DropColumn(
                name: "ContratId",
                table: "Engagements");

            migrationBuilder.AddColumn<bool>(
                name: "EstAjouteParRemaniement",
                table: "BudgetLines",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstAjouteParRemaniement",
                table: "BudgetLines");

            migrationBuilder.AddColumn<int>(
                name: "ContratId",
                table: "Factures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PDLId",
                table: "Exercices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContratId",
                table: "Engagements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActeursPDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nom = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActeursPDL", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BeneficiairesPDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nom = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiairesPDL", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CompetencesCollectivite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Numero = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencesCollectivite", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Contrats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExerciceId = table.Column<int>(type: "int", nullable: false),
                    TiersId = table.Column<int>(type: "int", nullable: false),
                    DateEcheance = table.Column<DateOnly>(type: "date", nullable: false),
                    DateSignature = table.Column<DateOnly>(type: "date", nullable: false),
                    FichierJoin = table.Column<byte[]>(type: "longblob", nullable: true),
                    MontantContrat = table.Column<double>(type: "double", nullable: false),
                    NumeroContrat = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Objet = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contrats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contrats_Exercices_ExerciceId",
                        column: x => x.ExerciceId,
                        principalTable: "Exercices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contrats_Tiers_TiersId",
                        column: x => x.TiersId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ODDs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Numero = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ODDs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DateDebut = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FicName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FickierJoin = table.Column<byte[]>(type: "longblob", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDL", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProgrammesPDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammesPDL", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Recensements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BudgetLineId = table.Column<int>(type: "int", nullable: false),
                    CommuneId = table.Column<int>(type: "int", nullable: false),
                    ExerciceId = table.Column<int>(type: "int", nullable: false),
                    TiersId = table.Column<int>(type: "int", nullable: false),
                    MontantRecense = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recensements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recensements_BudgetLines_BudgetLineId",
                        column: x => x.BudgetLineId,
                        principalTable: "BudgetLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recensements_Communes_CommuneId",
                        column: x => x.CommuneId,
                        principalTable: "Communes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recensements_Exercices_ExerciceId",
                        column: x => x.ExerciceId,
                        principalTable: "Exercices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recensements_Tiers_TiersId",
                        column: x => x.TiersId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StructureExecutionsPDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nom = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructureExecutionsPDL", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SecteursPDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProgrammePDLId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecteursPDL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecteursPDL_ProgrammesPDL_ProgrammePDLId",
                        column: x => x.ProgrammePDLId,
                        principalTable: "ProgrammesPDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActivitesPDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompetenceCollectiviteId = table.Column<int>(type: "int", nullable: false),
                    ODDId = table.Column<int>(type: "int", nullable: false),
                    PDLId = table.Column<int>(type: "int", nullable: false),
                    SecteurPDLId = table.Column<int>(type: "int", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FinancementExterne = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FinancementInterne = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Resultat = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitesPDL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivitesPDL_CompetencesCollectivite_CompetenceCollectiviteId",
                        column: x => x.CompetenceCollectiviteId,
                        principalTable: "CompetencesCollectivite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivitesPDL_ODDs_ODDId",
                        column: x => x.ODDId,
                        principalTable: "ODDs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivitesPDL_PDL_PDLId",
                        column: x => x.PDLId,
                        principalTable: "PDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivitesPDL_SecteursPDL_SecteurPDLId",
                        column: x => x.SecteurPDLId,
                        principalTable: "SecteursPDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActiviteActeurs",
                columns: table => new
                {
                    ActeursId = table.Column<int>(type: "int", nullable: false),
                    ActivitesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiviteActeurs", x => new { x.ActeursId, x.ActivitesId });
                    table.ForeignKey(
                        name: "FK_ActiviteActeurs_ActeursPDL_ActeursId",
                        column: x => x.ActeursId,
                        principalTable: "ActeursPDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActiviteActeurs_ActivitesPDL_ActivitesId",
                        column: x => x.ActivitesId,
                        principalTable: "ActivitesPDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActiviteBeneficiaires",
                columns: table => new
                {
                    ActivitesId = table.Column<int>(type: "int", nullable: false),
                    BeneficiairesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiviteBeneficiaires", x => new { x.ActivitesId, x.BeneficiairesId });
                    table.ForeignKey(
                        name: "FK_ActiviteBeneficiaires_ActivitesPDL_ActivitesId",
                        column: x => x.ActivitesId,
                        principalTable: "ActivitesPDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActiviteBeneficiaires_BeneficiairesPDL_BeneficiairesId",
                        column: x => x.BeneficiairesId,
                        principalTable: "BeneficiairesPDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActiviteStructureExecutions",
                columns: table => new
                {
                    ActivitesId = table.Column<int>(type: "int", nullable: false),
                    StructureExecutionsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiviteStructureExecutions", x => new { x.ActivitesId, x.StructureExecutionsId });
                    table.ForeignKey(
                        name: "FK_ActiviteStructureExecutions_ActivitesPDL_ActivitesId",
                        column: x => x.ActivitesId,
                        principalTable: "ActivitesPDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActiviteStructureExecutions_StructureExecutionsPDL_Structure~",
                        column: x => x.StructureExecutionsId,
                        principalTable: "StructureExecutionsPDL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_ContratId",
                table: "Factures",
                column: "ContratId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercices_PDLId",
                table: "Exercices",
                column: "PDLId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_ContratId",
                table: "Engagements",
                column: "ContratId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiviteActeurs_ActivitesId",
                table: "ActiviteActeurs",
                column: "ActivitesId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiviteBeneficiaires_BeneficiairesId",
                table: "ActiviteBeneficiaires",
                column: "BeneficiairesId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitesPDL_CompetenceCollectiviteId",
                table: "ActivitesPDL",
                column: "CompetenceCollectiviteId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitesPDL_ODDId",
                table: "ActivitesPDL",
                column: "ODDId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitesPDL_PDLId",
                table: "ActivitesPDL",
                column: "PDLId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitesPDL_SecteurPDLId",
                table: "ActivitesPDL",
                column: "SecteurPDLId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiviteStructureExecutions_StructureExecutionsId",
                table: "ActiviteStructureExecutions",
                column: "StructureExecutionsId");

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_ExerciceId",
                table: "Contrats",
                column: "ExerciceId");

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_TiersId",
                table: "Contrats",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_Recensements_BudgetLineId",
                table: "Recensements",
                column: "BudgetLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Recensements_CommuneId",
                table: "Recensements",
                column: "CommuneId");

            migrationBuilder.CreateIndex(
                name: "IX_Recensements_ExerciceId",
                table: "Recensements",
                column: "ExerciceId");

            migrationBuilder.CreateIndex(
                name: "IX_Recensements_TiersId",
                table: "Recensements",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_SecteursPDL_ProgrammePDLId",
                table: "SecteursPDL",
                column: "ProgrammePDLId");

            migrationBuilder.AddForeignKey(
                name: "FK_Engagements_Contrats_ContratId",
                table: "Engagements",
                column: "ContratId",
                principalTable: "Contrats",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercices_PDL_PDLId",
                table: "Exercices",
                column: "PDLId",
                principalTable: "PDL",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Factures_Contrats_ContratId",
                table: "Factures",
                column: "ContratId",
                principalTable: "Contrats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
