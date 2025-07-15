using System.Collections.Generic;
using PQBI.TableWidgetConfigurations.Dtos;
using PQBI.Dto;

namespace PQBI.TableWidgetConfigurations.Exporting
{
    public interface ITableWidgetConfigurationsExcelExporter
    {
        FileDto ExportToFile(List<GetTableWidgetConfigurationForViewDto> tableWidgetConfigurations);
    }
}