using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class initCreate6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Prefecture",
                table: "Communes",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Communes",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefecture",
                table: "Communes");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Communes");
        }
    }
}
