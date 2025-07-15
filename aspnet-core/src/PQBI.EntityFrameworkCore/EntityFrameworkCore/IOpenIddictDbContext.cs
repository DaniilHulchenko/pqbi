using Microsoft.EntityFrameworkCore;
using PQBI.OpenIddict.Applications;
using PQBI.OpenIddict.Authorizations;
using PQBI.OpenIddict.Scopes;
using PQBI.OpenIddict.Tokens;

namespace PQBI.EntityFrameworkCore
{
    public interface IOpenIddictDbContext
    {
        DbSet<OpenIddictApplication> Applications { get; }

        DbSet<OpenIddictAuthorization> Authorizations { get; }

        DbSet<OpenIddictScope> Scopes { get; }

        DbSet<OpenIddictToken> Tokens { get; }
    }

}