using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Unnecessary_Fields_In_Db : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "TableWidgetConfigurations");

            migrationBuilder.DropColumn(
                name: "ApplyTo",
                table: "CustomParameters");

            migrationBuilder.DropColumn(
                name: "TimeRange",
                table: "CustomParameters");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "BarChartWidgetConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TableWidgetConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplyTo",
                table: "CustomParameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeRange",
                table: "CustomParameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "BarChartWidgetConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
