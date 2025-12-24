using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ActionTitle = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerformedBy = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerformedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Communes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nom = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Region = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prefecture = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommuneType = table.Column<int>(type: "int", nullable: false),
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
                name: "CompteComptables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroCompte = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntituleCompte = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContrePartieId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompteComptables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompteComptables_CompteComptables_ContrePartieId",
                        column: x => x.ContrePartieId,
                        principalTable: "CompteComptables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Categorie = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Adresse = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prenom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroPieceIdentite = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypePieceIdentite = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RaisonSociale = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rccm = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nif = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroTva = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecteurActivite = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiers", x => x.Id);
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
                    NombrePostesSante = table.Column<int>(type: "int", nullable: false),
                    NombreSanteAmelioree = table.Column<int>(type: "int", nullable: false),
                    NombreEcoles = table.Column<int>(type: "int", nullable: false),
                    NombreEcolesCollege = table.Column<int>(type: "int", nullable: false),
                    NombreEcolesLycee = table.Column<int>(type: "int", nullable: false),
                    NombreEcolesPrimaire = table.Column<int>(type: "int", nullable: false),
                    NombreEcolesPrescolaire = table.Column<int>(type: "int", nullable: false),
                    NombreClassesCollege = table.Column<int>(type: "int", nullable: false),
                    NombreClassesLycee = table.Column<int>(type: "int", nullable: false),
                    NombreClassesPrimaire = table.Column<int>(type: "int", nullable: false),
                    NombreClassesPrescolaire = table.Column<int>(type: "int", nullable: false),
                    NombreElevesCollege = table.Column<int>(type: "int", nullable: false),
                    NombreElevesLycee = table.Column<int>(type: "int", nullable: false),
                    NombreElevesPrimaire = table.Column<int>(type: "int", nullable: false),
                    NombreElevesPrescolaire = table.Column<int>(type: "int", nullable: false),
                    NombreForages = table.Column<int>(type: "int", nullable: false),
                    NombreAssociation = table.Column<int>(type: "int", nullable: false),
                    NombrePointsEau = table.Column<int>(type: "int", nullable: false),
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
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
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
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tel = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommuneId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CompteBancaires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TiersId = table.Column<int>(type: "int", nullable: false),
                    IBAN = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BIC = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Banque = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pays = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompteBancaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompteBancaires_Tiers_TiersId",
                        column: x => x.TiersId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TiersId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    NumeroDocument = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomFichier = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CheminFichier = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Extension = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TailleFichier = table.Column<long>(type: "bigint", nullable: false),
                    DateAjout = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateEmission = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsValide = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTiers_Tiers_TiersId",
                        column: x => x.TiersId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    IdDetailCommune = table.Column<int>(type: "int", nullable: true)
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
                    MontantTotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantDepense = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantRecette = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DateApprobation = table.Column<DateOnly>(type: "date", nullable: true),
                    DateValidation = table.Column<DateOnly>(type: "date", nullable: true),
                    FichierValidation = table.Column<byte[]>(type: "longblob", nullable: true),
                    FileName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetsPrimitifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                        column: x => x.ExerciceId,
                        principalTable: "Exercices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Contrats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroContrat = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateSignature = table.Column<DateOnly>(type: "date", nullable: false),
                    DateEcheance = table.Column<DateOnly>(type: "date", nullable: false),
                    TiersId = table.Column<int>(type: "int", nullable: false),
                    Objet = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantContrat = table.Column<double>(type: "double", nullable: false),
                    FichierJoin = table.Column<byte[]>(type: "longblob", nullable: true),
                    ExerciceId = table.Column<int>(type: "int", nullable: false)
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
                name: "ExpressionBesoins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExerciceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpressionBesoins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpressionBesoins_Exercices_ExerciceId",
                        column: x => x.ExerciceId,
                        principalTable: "Exercices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    MontantPrevu = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantActu = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantRealise = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantEntreSortie = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Factures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroFacture = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateFacture = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MontantHT = table.Column<double>(type: "double", nullable: false),
                    TauxTVA = table.Column<double>(type: "double", nullable: false),
                    MontantTTC = table.Column<double>(type: "double", nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TiersId = table.Column<int>(type: "int", nullable: false),
                    ExerciceId = table.Column<int>(type: "int", nullable: false),
                    ContratId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FichierJoin = table.Column<byte[]>(type: "longblob", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factures_Contrats_ContratId",
                        column: x => x.ContratId,
                        principalTable: "Contrats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Factures_Exercices_ExerciceId",
                        column: x => x.ExerciceId,
                        principalTable: "Exercices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Factures_Tiers_TiersId",
                        column: x => x.TiersId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BonCommandes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpressionBesoinId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonCommandes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonCommandes_ExpressionBesoins_ExpressionBesoinId",
                        column: x => x.ExpressionBesoinId,
                        principalTable: "ExpressionBesoins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetailExpressionBesoins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExpressionBesoinId = table.Column<int>(type: "int", nullable: false),
                    NommenclatureId = table.Column<int>(type: "int", nullable: false),
                    Designation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantite = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailExpressionBesoins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetailExpressionBesoins_ExpressionBesoins_ExpressionBesoinId",
                        column: x => x.ExpressionBesoinId,
                        principalTable: "ExpressionBesoins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetailExpressionBesoins_Nommenclatures_NommenclatureId",
                        column: x => x.NommenclatureId,
                        principalTable: "Nommenclatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrdreRecettes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroOrdre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BudgetLineId = table.Column<int>(type: "int", nullable: false),
                    ExerciceId = table.Column<int>(type: "int", nullable: false),
                    CommuneId = table.Column<int>(type: "int", nullable: false),
                    Comptable = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TiersId = table.Column<int>(type: "int", nullable: true),
                    Motifs = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantOrdre = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantOrdreLettre = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateOrdre = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Etat = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdreRecettes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdreRecettes_BudgetLines_BudgetLineId",
                        column: x => x.BudgetLineId,
                        principalTable: "BudgetLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdreRecettes_Communes_CommuneId",
                        column: x => x.CommuneId,
                        principalTable: "Communes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdreRecettes_Exercices_ExerciceId",
                        column: x => x.ExerciceId,
                        principalTable: "Exercices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdreRecettes_Tiers_TiersId",
                        column: x => x.TiersId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Recensements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BudgetLineId = table.Column<int>(type: "int", nullable: false),
                    ExerciceId = table.Column<int>(type: "int", nullable: false),
                    CommuneId = table.Column<int>(type: "int", nullable: false),
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
                name: "Remaniements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Montant = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Motif = table.Column<string>(type: "text", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TypeRemaniement = table.Column<int>(type: "int", nullable: false),
                    IdBudgetLine = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remaniements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Remaniements_BudgetLines_IdBudgetLine",
                        column: x => x.IdBudgetLine,
                        principalTable: "BudgetLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetailsFactures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FactureId = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantite = table.Column<double>(type: "double", nullable: false),
                    PrixUnitaire = table.Column<double>(type: "double", nullable: false),
                    MontantTotal = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailsFactures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetailsFactures_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetailsBonCommandes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BonCommandeId = table.Column<int>(type: "int", nullable: false),
                    Designation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailsBonCommandes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetailsBonCommandes_BonCommandes_BonCommandeId",
                        column: x => x.BonCommandeId,
                        principalTable: "BonCommandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Engagements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExerciceId = table.Column<int>(type: "int", nullable: false),
                    CommuneId = table.Column<int>(type: "int", nullable: false),
                    BudgetLineId = table.Column<int>(type: "int", nullable: false),
                    TiersId = table.Column<int>(type: "int", nullable: true),
                    Objet = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateEngagement = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreditsBudgetaires = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    EngagementsAnterieurs = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantEngagement = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantLettre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FichierJoin = table.Column<byte[]>(type: "longblob", nullable: true),
                    FichierName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContratId = table.Column<int>(type: "int", nullable: true),
                    FactureId = table.Column<int>(type: "int", nullable: true),
                    Etat = table.Column<int>(type: "int", nullable: false),
                    BonCommandeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Engagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Engagements_BonCommandes_BonCommandeId",
                        column: x => x.BonCommandeId,
                        principalTable: "BonCommandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Engagements_BudgetLines_BudgetLineId",
                        column: x => x.BudgetLineId,
                        principalTable: "BudgetLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Engagements_Communes_CommuneId",
                        column: x => x.CommuneId,
                        principalTable: "Communes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Engagements_Contrats_ContratId",
                        column: x => x.ContratId,
                        principalTable: "Contrats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Engagements_Exercices_ExerciceId",
                        column: x => x.ExerciceId,
                        principalTable: "Exercices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Engagements_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Engagements_Tiers_TiersId",
                        column: x => x.TiersId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Mandats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroMandat = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bordereau = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mois = table.Column<int>(type: "int", nullable: false),
                    EngagementId = table.Column<int>(type: "int", nullable: false),
                    MontantBrut = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Rts = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    AutresPrecomptes = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantNet = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontantLettre = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateEmission = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Objet = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FichierJoin = table.Column<byte[]>(type: "longblob", nullable: true),
                    FichierName = table.Column<sbyte>(type: "tinyint", nullable: true),
                    DatePaiement = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Etat = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mandats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mandats_Engagements_EngagementId",
                        column: x => x.EngagementId,
                        principalTable: "Engagements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mouvement",
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
                    idMandat = table.Column<int>(type: "int", nullable: true),
                    idExercice = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mouvement", x => x.id);
                    table.ForeignKey(
                        name: "FK_mouvement_CompteComptables_idCompteComptable",
                        column: x => x.idCompteComptable,
                        principalTable: "CompteComptables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mouvement_Exercices_idExercice",
                        column: x => x.idExercice,
                        principalTable: "Exercices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_mouvement_Mandats_idMandat",
                        column: x => x.idMandat,
                        principalTable: "Mandats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_mouvement_OrdreRecettes_idOrdreRecette",
                        column: x => x.idOrdreRecette,
                        principalTable: "OrdreRecettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EcritureComptables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DateEcriture = table.Column<DateOnly>(type: "date", nullable: false),
                    CompteDebitId = table.Column<int>(type: "int", nullable: false),
                    CompteCreditId = table.Column<int>(type: "int", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    OrdreRecetteId = table.Column<int>(type: "int", nullable: true),
                    MandatId = table.Column<int>(type: "int", nullable: true),
                    MouvementId = table.Column<int>(type: "int", nullable: true),
                    idExercice = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcritureComptables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EcritureComptables_CompteComptables_CompteCreditId",
                        column: x => x.CompteCreditId,
                        principalTable: "CompteComptables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EcritureComptables_CompteComptables_CompteDebitId",
                        column: x => x.CompteDebitId,
                        principalTable: "CompteComptables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EcritureComptables_Exercices_idExercice",
                        column: x => x.idExercice,
                        principalTable: "Exercices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EcritureComptables_Mandats_MandatId",
                        column: x => x.MandatId,
                        principalTable: "Mandats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EcritureComptables_OrdreRecettes_OrdreRecetteId",
                        column: x => x.OrdreRecetteId,
                        principalTable: "OrdreRecettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EcritureComptables_mouvement_MouvementId",
                        column: x => x.MouvementId,
                        principalTable: "mouvement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BonCommandes_ExpressionBesoinId",
                table: "BonCommandes",
                column: "ExpressionBesoinId");

            migrationBuilder.CreateIndex(
                name: "IX_BonCommandes_Numero",
                table: "BonCommandes",
                column: "Numero",
                unique: true);

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
                name: "IX_CompteBancaires_BIC",
                table: "CompteBancaires",
                column: "BIC",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompteBancaires_IBAN",
                table: "CompteBancaires",
                column: "IBAN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompteBancaires_TiersId",
                table: "CompteBancaires",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_CompteComptables_ContrePartieId",
                table: "CompteComptables",
                column: "ContrePartieId");

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_ExerciceId",
                table: "Contrats",
                column: "ExerciceId");

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_TiersId",
                table: "Contrats",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_DetailCommunes_IdCommune",
                table: "DetailCommunes",
                column: "IdCommune");

            migrationBuilder.CreateIndex(
                name: "IX_DetailExpressionBesoins_ExpressionBesoinId",
                table: "DetailExpressionBesoins",
                column: "ExpressionBesoinId");

            migrationBuilder.CreateIndex(
                name: "IX_DetailExpressionBesoins_NommenclatureId",
                table: "DetailExpressionBesoins",
                column: "NommenclatureId");

            migrationBuilder.CreateIndex(
                name: "IX_DetailsBonCommandes_BonCommandeId",
                table: "DetailsBonCommandes",
                column: "BonCommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_DetailsFactures_FactureId",
                table: "DetailsFactures",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTiers_TiersId",
                table: "DocumentTiers",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_EcritureComptables_CompteCreditId",
                table: "EcritureComptables",
                column: "CompteCreditId");

            migrationBuilder.CreateIndex(
                name: "IX_EcritureComptables_CompteDebitId",
                table: "EcritureComptables",
                column: "CompteDebitId");

            migrationBuilder.CreateIndex(
                name: "IX_EcritureComptables_idExercice",
                table: "EcritureComptables",
                column: "idExercice");

            migrationBuilder.CreateIndex(
                name: "IX_EcritureComptables_MandatId",
                table: "EcritureComptables",
                column: "MandatId");

            migrationBuilder.CreateIndex(
                name: "IX_EcritureComptables_MouvementId",
                table: "EcritureComptables",
                column: "MouvementId");

            migrationBuilder.CreateIndex(
                name: "IX_EcritureComptables_OrdreRecetteId",
                table: "EcritureComptables",
                column: "OrdreRecetteId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_BonCommandeId",
                table: "Engagements",
                column: "BonCommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_BudgetLineId",
                table: "Engagements",
                column: "BudgetLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_CommuneId",
                table: "Engagements",
                column: "CommuneId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_ContratId",
                table: "Engagements",
                column: "ContratId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_ExerciceId",
                table: "Engagements",
                column: "ExerciceId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_FactureId",
                table: "Engagements",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_TiersId",
                table: "Engagements",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercices_IdDetailCommune",
                table: "Exercices",
                column: "IdDetailCommune",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpressionBesoins_ExerciceId",
                table: "ExpressionBesoins",
                column: "ExerciceId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_ContratId",
                table: "Factures",
                column: "ContratId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_ExerciceId",
                table: "Factures",
                column: "ExerciceId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_TiersId",
                table: "Factures",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_Mandats_EngagementId",
                table: "Mandats",
                column: "EngagementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mouvement_idCompteComptable",
                table: "mouvement",
                column: "idCompteComptable");

            migrationBuilder.CreateIndex(
                name: "IX_mouvement_idExercice",
                table: "mouvement",
                column: "idExercice");

            migrationBuilder.CreateIndex(
                name: "IX_mouvement_idMandat",
                table: "mouvement",
                column: "idMandat");

            migrationBuilder.CreateIndex(
                name: "IX_mouvement_idOrdreRecette",
                table: "mouvement",
                column: "idOrdreRecette");

            migrationBuilder.CreateIndex(
                name: "IX_Nommenclatures_ParentId",
                table: "Nommenclatures",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdreRecettes_BudgetLineId",
                table: "OrdreRecettes",
                column: "BudgetLineId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdreRecettes_CommuneId",
                table: "OrdreRecettes",
                column: "CommuneId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdreRecettes_ExerciceId",
                table: "OrdreRecettes",
                column: "ExerciceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdreRecettes_TiersId",
                table: "OrdreRecettes",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

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
                name: "IX_Remaniements_IdBudgetLine",
                table: "Remaniements",
                column: "IdBudgetLine");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tiers_Email",
                table: "Tiers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tiers_Nif",
                table: "Tiers",
                column: "Nif");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CommuneId",
                table: "Users",
                column: "CommuneId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CompteBancaires");

            migrationBuilder.DropTable(
                name: "DetailExpressionBesoins");

            migrationBuilder.DropTable(
                name: "DetailsBonCommandes");

            migrationBuilder.DropTable(
                name: "DetailsFactures");

            migrationBuilder.DropTable(
                name: "DocumentTiers");

            migrationBuilder.DropTable(
                name: "EcritureComptables");

            migrationBuilder.DropTable(
                name: "Recensements");

            migrationBuilder.DropTable(
                name: "Remaniements");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "mouvement");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "CompteComptables");

            migrationBuilder.DropTable(
                name: "Mandats");

            migrationBuilder.DropTable(
                name: "OrdreRecettes");

            migrationBuilder.DropTable(
                name: "Engagements");

            migrationBuilder.DropTable(
                name: "BonCommandes");

            migrationBuilder.DropTable(
                name: "BudgetLines");

            migrationBuilder.DropTable(
                name: "Factures");

            migrationBuilder.DropTable(
                name: "ExpressionBesoins");

            migrationBuilder.DropTable(
                name: "BudgetsPrimitifs");

            migrationBuilder.DropTable(
                name: "Nommenclatures");

            migrationBuilder.DropTable(
                name: "Contrats");

            migrationBuilder.DropTable(
                name: "Exercices");

            migrationBuilder.DropTable(
                name: "Tiers");

            migrationBuilder.DropTable(
                name: "DetailCommunes");

            migrationBuilder.DropTable(
                name: "Communes");
        }
    }
}
