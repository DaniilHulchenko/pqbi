using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class CardWidgetConfigurationAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardWidgetConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateRange = table.Column<string>(type: "TEXT", nullable: false),
                    Parameters = table.Column<string>(type: "TEXT", nullable: false),
                    StyleType = table.Column<int>(type: "INTEGER", nullable: false),
                    RefreshRate = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardWidgetConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardWidgetConfigurations_TenantId",
                table: "CardWidgetConfigurations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardWidgetConfigurations");
        }
    }
}
