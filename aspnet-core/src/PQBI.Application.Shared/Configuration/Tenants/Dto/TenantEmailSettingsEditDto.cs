using Abp.Auditing;
using PQBI.Configuration.Dto;

namespace PQBI.Configuration.Tenants.Dto
{
    public class TenantEmailSettingsEditDto : EmailSettingsEditDto
    {
        public bool UseHostDefaultEmailSettings { get; set; }
    }
}