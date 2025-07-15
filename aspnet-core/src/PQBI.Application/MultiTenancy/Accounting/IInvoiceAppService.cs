using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using PQBI.MultiTenancy.Accounting.Dto;

namespace PQBI.MultiTenancy.Accounting
{
    public interface IInvoiceAppService
    {
        Task<InvoiceDto> GetInvoiceInfo(EntityDto<long> input);

        Task CreateInvoice(CreateInvoiceDto input);
    }
}
