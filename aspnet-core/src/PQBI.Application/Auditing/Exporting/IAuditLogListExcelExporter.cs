using System.Collections.Generic;
using PQBI.Auditing.Dto;
using PQBI.Dto;

namespace PQBI.Auditing.Exporting
{
    public interface IAuditLogListExcelExporter
    {
        FileDto ExportToFile(List<AuditLogListDto> auditLogListDtos);

        FileDto ExportToFile(List<EntityChangeListDto> entityChangeListDtos);
    }
}
