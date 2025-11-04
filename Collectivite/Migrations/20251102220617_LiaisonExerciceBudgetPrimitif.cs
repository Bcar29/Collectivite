using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class LiaisonExerciceBudgetPrimitif : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetsPrimitifs_Communes_CommuneId",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercices_Communes_IdCommune",
                table: "Exercices");

            migrationBuilder.DropIndex(
                name: "IX_Exercices_IdCommune",
                table: "Exercices");

            migrationBuilder.DropIndex(
                name: "IX_BudgetsPrimitifs_ExerciceId",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropColumn(
                name: "IdCommune",
                table: "Exercices");

            migrationBuilder.AlterColumn<string>(
                name: "SousParagraphe",
                table: "Nommenclatures",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Paragraphe",
                table: "Nommenclatures",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Intitule",
                table: "Nommenclatures",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Chapitre",
                table: "Nommenclatures",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Article",
                table: "Nommenclatures",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CommuneId",
                table: "Exercices",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CommuneId",
                table: "BudgetsPrimitifs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Exercices_CommuneId",
                table: "Exercices",
                column: "CommuneId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetsPrimitifs_ExerciceId",
                table: "BudgetsPrimitifs",
                column: "ExerciceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetsPrimitifs_Communes_CommuneId",
                table: "BudgetsPrimitifs",
                column: "CommuneId",
                principalTable: "Communes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                table: "BudgetsPrimitifs",
                column: "ExerciceId",
                principalTable: "Exercices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercices_Communes_CommuneId",
                table: "Exercices",
                column: "CommuneId",
                principalTable: "Communes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetsPrimitifs_Communes_CommuneId",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercices_Communes_CommuneId",
                table: "Exercices");

            migrationBuilder.DropIndex(
                name: "IX_Exercices_CommuneId",
                table: "Exercices");

            migrationBuilder.DropIndex(
                name: "IX_BudgetsPrimitifs_ExerciceId",
                table: "BudgetsPrimitifs");

            migrationBuilder.DropColumn(
                name: "CommuneId",
                table: "Exercices");

            migrationBuilder.AlterColumn<string>(
                name: "SousParagraphe",
                table: "Nommenclatures",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Paragraphe",
                table: "Nommenclatures",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Intitule",
                table: "Nommenclatures",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Chapitre",
                table: "Nommenclatures",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Article",
                table: "Nommenclatures",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "IdCommune",
                table: "Exercices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "CommuneId",
                table: "BudgetsPrimitifs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercices_IdCommune",
                table: "Exercices",
                column: "IdCommune");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetsPrimitifs_ExerciceId",
                table: "BudgetsPrimitifs",
                column: "ExerciceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetsPrimitifs_Communes_CommuneId",
                table: "BudgetsPrimitifs",
                column: "CommuneId",
                principalTable: "Communes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetsPrimitifs_Exercices_ExerciceId",
                table: "BudgetsPrimitifs",
                column: "ExerciceId",
                principalTable: "Exercices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercices_Communes_IdCommune",
                table: "Exercices",
                column: "IdCommune",
                principalTable: "Communes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
