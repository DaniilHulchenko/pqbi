using PQBI.EntityFrameworkCore;

namespace PQBI.Migrations.Seed.Host
{
    public class InitialHostDbBuilder
    {
        private readonly PQBIDbContext _context;

        public InitialHostDbBuilder(PQBIDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            new DefaultEditionCreator(_context).Create();
            new DefaultLanguagesCreator(_context).Create();
            new HostRoleAndUserCreator(_context).Create();
            new DefaultSettingsCreator(_context).Create();

            _context.SaveChanges();
        }
    }
}
