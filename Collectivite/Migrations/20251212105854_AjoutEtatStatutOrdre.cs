using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEtatStatutOrdre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "etat",
                table: "OrdreRecettes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "OrdreRecettes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "etat",
                table: "OrdreRecettes");

            migrationBuilder.DropColumn(
                name: "status",
                table: "OrdreRecettes");
        }
    }
}
