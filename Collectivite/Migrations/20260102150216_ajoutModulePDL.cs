using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class ajoutModulePDL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercices_DetailCommunes_IdDetailCommune",
                table: "Exercices");

            migrationBuilder.DropIndex(
                name: "IX_Exercices_IdDetailCommune",
                table: "Exercices");

            migrationBuilder.RenameColumn(
                name: "IdDetailCommune",
                table: "Exercices",
                newName: "PDLId");

            migrationBuilder.AddColumn<int>(
                name: "ExerciceId",
                table: "DetailCommunes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActeursPDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nom = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
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
                    Nom = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
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
                    Numero = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencesCollectivite", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ODDs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
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
                    Libelle = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammesPDL", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StructureExecutionsPDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nom = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
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
                    Libelle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProgrammePDLId = table.Column<int>(type: "int", nullable: false)
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
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Resultat = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateDebut = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinancementInterne = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FinancementExterne = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PDLId = table.Column<int>(type: "int", nullable: false),
                    SecteurPDLId = table.Column<int>(type: "int", nullable: false),
                    CompetenceCollectiviteId = table.Column<int>(type: "int", nullable: false),
                    ODDId = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_Exercices_PDLId",
                table: "Exercices",
                column: "PDLId");

            migrationBuilder.CreateIndex(
                name: "IX_DetailCommunes_ExerciceId",
                table: "DetailCommunes",
                column: "ExerciceId",
                unique: true);

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
                name: "IX_SecteursPDL_ProgrammePDLId",
                table: "SecteursPDL",
                column: "ProgrammePDLId");

            migrationBuilder.AddForeignKey(
                name: "FK_DetailCommunes_Exercices_ExerciceId",
                table: "DetailCommunes",
                column: "ExerciceId",
                principalTable: "Exercices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercices_PDL_PDLId",
                table: "Exercices",
                column: "PDLId",
                principalTable: "PDL",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetailCommunes_Exercices_ExerciceId",
                table: "DetailCommunes");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercices_PDL_PDLId",
                table: "Exercices");

            migrationBuilder.DropTable(
                name: "ActiviteActeurs");

            migrationBuilder.DropTable(
                name: "ActiviteBeneficiaires");

            migrationBuilder.DropTable(
                name: "ActiviteStructureExecutions");

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
                name: "IX_Exercices_PDLId",
                table: "Exercices");

            migrationBuilder.DropIndex(
                name: "IX_DetailCommunes_ExerciceId",
                table: "DetailCommunes");

            migrationBuilder.DropColumn(
                name: "ExerciceId",
                table: "DetailCommunes");

            migrationBuilder.RenameColumn(
                name: "PDLId",
                table: "Exercices",
                newName: "IdDetailCommune");

            migrationBuilder.CreateIndex(
                name: "IX_Exercices_IdDetailCommune",
                table: "Exercices",
                column: "IdDetailCommune",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercices_DetailCommunes_IdDetailCommune",
                table: "Exercices",
                column: "IdDetailCommune",
                principalTable: "DetailCommunes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
