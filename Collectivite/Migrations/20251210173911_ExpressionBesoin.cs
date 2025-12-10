using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class ExpressionBesoin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpressionBesoins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_DetailExpressionBesoins_ExpressionBesoinId",
                table: "DetailExpressionBesoins",
                column: "ExpressionBesoinId");

            migrationBuilder.CreateIndex(
                name: "IX_DetailExpressionBesoins_NommenclatureId",
                table: "DetailExpressionBesoins",
                column: "NommenclatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressionBesoins_ExerciceId",
                table: "ExpressionBesoins",
                column: "ExerciceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetailExpressionBesoins");

            migrationBuilder.DropTable(
                name: "ExpressionBesoins");
        }
    }
}
