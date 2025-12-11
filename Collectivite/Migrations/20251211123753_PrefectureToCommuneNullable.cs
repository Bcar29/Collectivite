using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class PrefectureToCommuneNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Prefecture",
                table: "Communes",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Communes",
                keyColumn: "Prefecture",
                keyValue: null,
                column: "Prefecture",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Prefecture",
                table: "Communes",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
