using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class BarChartConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "CustomParameters");

            migrationBuilder.RenameColumn(
                name: "Events",
                table: "BarChartWidgetConfigurations",
                newName: "Configuration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Configuration",
                table: "BarChartWidgetConfigurations",
                newName: "Events");

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "CustomParameters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
