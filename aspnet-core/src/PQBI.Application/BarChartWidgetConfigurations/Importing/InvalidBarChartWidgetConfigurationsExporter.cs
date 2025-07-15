using PQBI.BarChartWidgetConfigurations;

using System.Collections.Generic;
using Abp.Collections.Extensions;
using PQBI.BarChartWidgetConfigurations.Importing.Dto;
using PQBI.DataExporting.Excel.MiniExcel;
using PQBI.DataImporting.Excel;
using PQBI.Dto;
using PQBI.Storage;

namespace PQBI.BarChartWidgetConfigurations
{
    public class InvalidBarChartWidgetConfigurationExporter(ITempFileCacheManager tempFileCacheManager)
        : MiniExcelExcelExporterBase(tempFileCacheManager), IExcelInvalidEntityExporter<ImportBarChartWidgetConfigurationDto>
    {
        public FileDto ExportToFile(List<ImportBarChartWidgetConfigurationDto> barChartWidgetConfigurationList)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var barChartWidgetConfiguration in barChartWidgetConfigurationList)
        {
            items.Add(new Dictionary<string, object>()
                {
                    {"Refuse Reason", barChartWidgetConfiguration.Exception},
                    {"Type", barChartWidgetConfiguration.Type},
                    {"Components", barChartWidgetConfiguration.Components},
                    {"Events", barChartWidgetConfiguration.Events},
                    {"DateRange", barChartWidgetConfiguration.DateRange}
                });
        }

        return CreateExcelPackage("InvalidBarChartWidgetConfigurationImportList.xlsx", items);
    }
}
}