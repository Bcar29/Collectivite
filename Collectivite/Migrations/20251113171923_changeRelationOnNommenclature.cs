using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectivite.Migrations
{
    /// <inheritdoc />
    public partial class changeRelationOnNommenclature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nommenclatures_Nommenclatures_ParentId",
                table: "Nommenclatures");

            migrationBuilder.AddForeignKey(
                name: "FK_Nommenclatures_Nommenclatures_ParentId",
                table: "Nommenclatures",
                column: "ParentId",
                principalTable: "Nommenclatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nommenclatures_Nommenclatures_ParentId",
                table: "Nommenclatures");

            migrationBuilder.AddForeignKey(
                name: "FK_Nommenclatures_Nommenclatures_ParentId",
                table: "Nommenclatures",
                column: "ParentId",
                principalTable: "Nommenclatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
