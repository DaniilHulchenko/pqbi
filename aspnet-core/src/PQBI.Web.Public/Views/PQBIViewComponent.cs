using Abp.AspNetCore.Mvc.ViewComponents;

namespace PQBI.Web.Public.Views
{
    public abstract class PQBIViewComponent : AbpViewComponent
    {
        protected PQBIViewComponent()
        {
            LocalizationSourceName = PQBIConsts.LocalizationSourceName;
        }
    }
}