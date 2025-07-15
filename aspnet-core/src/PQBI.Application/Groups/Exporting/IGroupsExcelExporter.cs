using System.Collections.Generic;

using PQBI.Groups.Dtos;
using PQBI.Dto;

namespace PQBI.Groups.Exporting;

public interface IGroupsExcelExporter
{
    FileDto ExportToFile(List<GetGroupForViewDto> groups);
}