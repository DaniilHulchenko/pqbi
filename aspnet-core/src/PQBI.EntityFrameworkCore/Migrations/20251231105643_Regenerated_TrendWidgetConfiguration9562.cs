using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class Regenerated_TrendWidgetConfiguration9562 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThresholdSettings",
                table: "TrendWidgetConfigurations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThresholdSettings",
                table: "TrendWidgetConfigurations");
        }
    }
}
