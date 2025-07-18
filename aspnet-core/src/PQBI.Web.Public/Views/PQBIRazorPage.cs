﻿using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace PQBI.Web.Public.Views
{
    public abstract class PQBIRazorPage<TModel> : AbpRazorPage<TModel>
    {
        [RazorInject]
        public IAbpSession AbpSession { get; set; }

        protected PQBIRazorPage()
        {
            LocalizationSourceName = PQBIConsts.LocalizationSourceName;
        }
    }
}
