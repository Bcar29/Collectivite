using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class ajoutFichierNameDansMandat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Mandats",
                keyColumn: "Motif",
                keyValue: null,
                column: "Motif",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Motif",
                table: "Mandats",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<sbyte>(
                name: "FichierName",
                table: "Mandats",
                type: "tinyint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FichierName",
                table: "Mandats");

            migrationBuilder.AlterColumn<string>(
                name: "Motif",
                table: "Mandats",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
