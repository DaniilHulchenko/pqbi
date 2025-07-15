using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class Regenerated_CustomParameter9673 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "Resolution",
            //    table: "CustomParameters");

            migrationBuilder.AddColumn<string>(
                name: "CustomBaseDataList",
                table: "CustomParameters",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomBaseDataList",
                table: "CustomParameters");

            //migrationBuilder.AddColumn<string>(
            //    name: "Resolution",
            //    table: "CustomParameters",
            //    type: "TEXT",
            //    nullable: false,
            //    defaultValue: "");
        }
    }
}
