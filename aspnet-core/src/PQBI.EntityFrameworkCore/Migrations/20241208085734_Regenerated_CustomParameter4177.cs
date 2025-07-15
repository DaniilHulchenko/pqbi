using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class Regenerated_CustomParameter4177 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InternaParameters",
                table: "CustomParameters",
                newName: "InternalParameters");

            migrationBuilder.RenameColumn(
                name: "InternaParameterType",
                table: "CustomParameters",
                newName: "InternalParameterType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InternalParameters",
                table: "CustomParameters",
                newName: "InternaParameters");

            migrationBuilder.RenameColumn(
                name: "InternalParameterType",
                table: "CustomParameters",
                newName: "InternaParameterType");
        }
    }
}
