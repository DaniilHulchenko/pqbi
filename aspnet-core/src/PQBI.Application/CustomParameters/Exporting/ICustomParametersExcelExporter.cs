using System.Collections.Generic;

using PQBI.CustomParameters.Dtos;
using PQBI.Dto;

namespace PQBI.CustomParameters.Exporting;

public interface ICustomParametersExcelExporter
{
    FileDto ExportToFile(List<GetCustomParameterForViewDto> customParameters);
}