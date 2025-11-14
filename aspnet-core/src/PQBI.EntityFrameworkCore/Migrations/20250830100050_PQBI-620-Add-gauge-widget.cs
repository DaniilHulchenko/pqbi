using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class PQBI620Addgaugewidget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GaugeWidgetConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateRange = table.Column<string>(type: "TEXT", nullable: false),
                    Parameter = table.Column<string>(type: "TEXT", nullable: false),
                    Style = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshRate = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GaugeWidgetConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GaugeWidgetConfigurations_TenantId",
                table: "GaugeWidgetConfigurations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GaugeWidgetConfigurations");
        }
    }
}
