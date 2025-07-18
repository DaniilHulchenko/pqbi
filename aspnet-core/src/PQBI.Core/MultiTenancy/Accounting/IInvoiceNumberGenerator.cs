﻿using System.Threading.Tasks;
using Abp.Dependency;

namespace PQBI.MultiTenancy.Accounting
{
    public interface IInvoiceNumberGenerator : ITransientDependency
    {
        Task<string> GetNewInvoiceNumber();
    }
}