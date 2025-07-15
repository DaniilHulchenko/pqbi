using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class Regenerated_CustomParameter4646 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomParameterIds",
                table: "CustomParameters");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "CustomParameters");

            migrationBuilder.DropColumn(
                name: "QuantityResolution",
                table: "CustomParameters");

            migrationBuilder.RenameColumn(
                name: "Operator",
                table: "CustomParameters",
                newName: "InnerCustomParameters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InnerCustomParameters",
                table: "CustomParameters",
                newName: "Operator");

            migrationBuilder.AddColumn<string>(
                name: "CustomParameterIds",
                table: "CustomParameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Quantity",
                table: "CustomParameters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuantityResolution",
                table: "CustomParameters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
