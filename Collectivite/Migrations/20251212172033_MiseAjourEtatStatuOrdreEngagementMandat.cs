using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class MiseAjourEtatStatuOrdreEngagementMandat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "status",
                table: "OrdreRecettes",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "etat",
                table: "OrdreRecettes",
                newName: "Etat");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Mandats",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "etat",
                table: "Mandats",
                newName: "Etat");

            migrationBuilder.RenameColumn(
                name: "etat",
                table: "Engagements",
                newName: "Etat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "OrdreRecettes",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Etat",
                table: "OrdreRecettes",
                newName: "etat");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Mandats",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Etat",
                table: "Mandats",
                newName: "etat");

            migrationBuilder.RenameColumn(
                name: "Etat",
                table: "Engagements",
                newName: "etat");
        }
    }
}
