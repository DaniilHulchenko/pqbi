using Abp.Domain.Services;

namespace PQBI
{
    public abstract class PQBIDomainServiceBase : DomainService
    {
        /* Add your common members for all your domain services. */

        protected PQBIDomainServiceBase()
        {
            LocalizationSourceName = PQBIConsts.LocalizationSourceName;
        }
    }
}
