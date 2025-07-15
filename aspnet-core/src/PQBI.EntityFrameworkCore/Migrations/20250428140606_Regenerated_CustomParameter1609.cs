using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQBI.Migrations
{
    /// <inheritdoc />
    public partial class Regenerated_CustomParameter1609 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE TABLE CustomParameters_temp (
                Id INTEGER NOT NULL PRIMARY KEY,
                TenantId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                AggregationFunction TEXT NOT NULL,
                STDPQSParametersList TEXT,
                Type TEXT NOT NULL,
                InnerCustomParameters TEXT,
                ResolutionInSeconds INTEGER
            );
        ");

            // 2. Copy data from the old table to the temp table
            migrationBuilder.Sql(@"
            INSERT INTO CustomParameters_temp (Id, TenantId, Name, AggregationFunction, STDPQSParametersList, Type, InnerCustomParameters, ResolutionInSeconds)
            SELECT Id, TenantId, Name, AggregationFunction, STDPQSParametersList, Type, InnerCustomParameters, ResolutionInSeconds
            FROM CustomParameters;
        ");

            // 3. Drop the old table
            migrationBuilder.Sql(@"DROP TABLE CustomParameters;");

            // 4. Rename temp table to old table name
            migrationBuilder.Sql(@"ALTER TABLE CustomParameters_temp RENAME TO CustomParameters;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the process if needed
            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "CustomParameters",
                type: "TEXT",
                nullable: true);
        }
    }
}
