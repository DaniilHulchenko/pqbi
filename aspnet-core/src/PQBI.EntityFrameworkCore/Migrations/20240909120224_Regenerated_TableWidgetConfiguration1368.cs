using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class Regenerated_TableWidgetConfiguration1368 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeRange",
                table: "TableWidgetConfigurations");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateEnd",
                table: "TableWidgetConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "TableWidgetConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateEnd",
                table: "TableWidgetConfigurations");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "TableWidgetConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "TimeRange",
                table: "TableWidgetConfigurations",
                type: "TEXT",
                nullable: true);
        }
    }
}
