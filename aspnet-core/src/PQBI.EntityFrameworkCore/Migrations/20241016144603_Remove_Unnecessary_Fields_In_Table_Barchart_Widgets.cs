using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Unnecessary_Fields_In_Table_Barchart_Widgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "TableWidgetConfigurations");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "TableWidgetConfigurations");

            migrationBuilder.DropColumn(
                name: "DateEnd",
                table: "BarChartWidgetConfigurations");

            migrationBuilder.DropColumn(
                name: "DateStart",
                table: "BarChartWidgetConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "DateRange",
                table: "TableWidgetConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DateRange",
                table: "BarChartWidgetConfigurations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateRange",
                table: "TableWidgetConfigurations");

            migrationBuilder.DropColumn(
                name: "DateRange",
                table: "BarChartWidgetConfigurations");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
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

            migrationBuilder.AddColumn<DateTime>(
                name: "DateEnd",
                table: "BarChartWidgetConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateStart",
                table: "BarChartWidgetConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
