dotnet ef migrations add <MigrationName>






dotnet ef database update -c PQBIDbContext





dotnet ef migrations add Initial_Postgres -c PQBIDbContext -o Migrations/Postgres/
dotnet ef migrations add Initial_SQLite   -c PQBIDbContext -o Migrations/SQLite/

