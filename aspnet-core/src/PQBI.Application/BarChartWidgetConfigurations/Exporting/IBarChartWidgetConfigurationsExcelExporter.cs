using System.Collections.Generic;
using PQBI.BarChartWidgetConfigurations.Dtos;
using PQBI.Dto;

namespace PQBI.BarChartWidgetConfigurations.Exporting
{
    public interface IBarChartWidgetConfigurationsExcelExporter
    {
        FileDto ExportToFile(List<GetBarChartWidgetConfigurationForViewDto> barChartWidgetConfigurations);
    }
}