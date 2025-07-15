using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace PQBI.EntityFrameworkCore
{
    public static class PQBIDbContextConfigurer
    {
        public static void Configure(DbContextOptionsBuilder<PQBIDbContext> builder, string connectionString, bool isPostgres)
        {
            if (isPostgres)
            {
                builder.UseNpgsql(connectionString);
            }
            else
            {

                builder.UseSqlite(connectionString);
            }
        }

        public static void Configure(DbContextOptionsBuilder<PQBIDbContext> builder, DbConnection connection, bool isPostgres)
        {

            if (isPostgres)
            {

                builder.UseNpgsql(connection);
            }
            else
            {
                builder.UseSqlite(connection);
            }
        }
    }
}