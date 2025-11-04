using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class initialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Communes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nom = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DistanceChefLieuProvince = table.Column<double>(type: "double", nullable: false),
                    DistanceChefLieuRegion = table.Column<double>(type: "double", nullable: false),
                    DistanceCapitale = table.Column<double>(type: "double", nullable: false),
                    DateCreation = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Nommenclatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Chapitre = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Article = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Paragraphe = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SousParagraphe = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Intitule = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nature = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nommenclatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nommenclatures_Nommenclatures_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Nommenclatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetailCommunes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NombreConseillers = table.Column<int>(type: "int", nullable: false),
                    NombreDelegationSpeciale = table.Column<int>(type: "int", nullable: false),
                    EffectifTotalPersonnel = table.Column<int>(type: "int", nullable: false),
                    EffectifPermanent = table.Column<int>(type: "int", nullable: false),
                    EffectifTemporaire = table.Column<int>(type: "int", nullable: false),
                    NombreQuartiers = table.Column<int>(type: "int", nullable: false),
                    NombreDistricts = table.Column<int>(type: "int", nullable: false),
                    NombreSecteurs = table.Column<int>(type: "int", nullable: false),
                    PopulationTotale = table.Column<int>(type: "int", nullable: false),
                    PopulationFemmes = table.Column<int>(type: "int", nullable: false),
                    PopulationHommes = table.Column<int>(type: "int", nullable: false),
                    Superficie = table.Column<double>(type: "double", nullable: false),
                    Densite = table.Column<double>(type: "double", nullable: false),
                    NombreCentresSante = table.Column<int>(type: "int", nullable: false),
                    NombreEcoles = table.Column<int>(type: "int", nullable: false),
                    NombreEcolesPrimaires = table.Column<int>(type: "int", nullable: false),
                    NombreEcolesSecondaires = table.Column<int>(type: "int", nullable: false),
                    NombreClassesPrimaires = table.Column<int>(type: "int", nullable: false),
                    NombreClassesSecondaires = table.Column<int>(type: "int", nullable: false),
                    NombreElevesPrimaires = table.Column<int>(type: "int", nullable: false),
                    NombreElevesSecondaires = table.Column<int>(type: "int", nullable: false),
                    NombreForages = table.Column<int>(type: "int", nullable: false),
                    NombreOng = table.Column<int>(type: "int", nullable: false),
                    NombreOngNationales = table.Column<int>(type: "int", nullable: false),
                    NombreOngEtrangeres = table.Column<int>(type: "int", nullable: false),
                    NombreGroupements = table.Column<int>(type: "int", nullable: false),
                    NombreCooperatives = table.Column<int>(type: "int", nullable: false),
                    NombreDetenteursArmesFeu = table.Column<int>(type: "int", nullable: false),
                    NombreMarches = table.Column<int>(type: "int", nullable: false),
                    NombreMarchesJournaliers = table.Column<int>(type: "int", nullable: false),
                    NombreMarchesHebdomadaires = table.Column<int>(type: "int", nullable: false),
                    IdCommune = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailCommunes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetailCommunes_Communes_IdCommune",
                        column: x => x.IdCommune,
                        principalTable: "Communes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tel = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommuneId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Communes_CommuneId",
                        column: x => x.CommuneId,
                        principalTable: "Communes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Exercices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Libelle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateDebut = table.Column<DateOnly>(type: "date", nullable: false),
                    DateFin = table.Column<DateOnly>(type: "date", nullable: false),
                    EstCloture = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IdDetailCommune = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercices_DetailCommunes_IdDetailCommune",
                        column: x => x.IdDetailCommune,
                        principalTable: "DetailCommunes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BudgetsPrimitifs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExerciceId = table.Column<int>(type: "int", nullable: false),
                    Montant = table.Column<int>(type: "int", nullable: false),
                    DateVote = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetsPrimitifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                        column: x => x.ExerciceId,
                        principalTable: "Exercices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BudgetPrimitifId = table.Column<int>(type: "int", nullable: false),
                    NommenclatureId = table.Column<int>(type: "int", nullable: false),
                    MontantPrevu = table.Column<int>(type: "int", nullable: false),
                    MontantActu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetLines_BudgetsPrimitifs_BudgetPrimitifId",
                        column: x => x.BudgetPrimitifId,
                        principalTable: "BudgetsPrimitifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Nommenclatures_NommenclatureId",
                        column: x => x.NommenclatureId,
                        principalTable: "Nommenclatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetPrimitifId",
                table: "BudgetLines",
                column: "BudgetPrimitifId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_NommenclatureId",
                table: "BudgetLines",
                column: "NommenclatureId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetsPrimitifs_ExerciceId",
                table: "BudgetsPrimitifs",
                column: "ExerciceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetailCommunes_IdCommune",
                table: "DetailCommunes",
                column: "IdCommune");

            migrationBuilder.CreateIndex(
                name: "IX_Exercices_IdDetailCommune",
                table: "Exercices",
                column: "IdDetailCommune",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nommenclatures_ParentId",
                table: "Nommenclatures",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CommuneId",
                table: "Users",
                column: "CommuneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetLines");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "BudgetsPrimitifs");

            migrationBuilder.DropTable(
                name: "Nommenclatures");

            migrationBuilder.DropTable(
                name: "Exercices");

            migrationBuilder.DropTable(
                name: "DetailCommunes");

            migrationBuilder.DropTable(
                name: "Communes");
        }
    }
}
