using Abp.AspNetCore.Mvc.Views;

namespace PQBI.Web.Views
{
    public abstract class PQBIRazorPage<TModel> : AbpRazorPage<TModel>
    {
        protected PQBIRazorPage()
        {
            LocalizationSourceName = PQBIConsts.LocalizationSourceName;
        }
    }
}
