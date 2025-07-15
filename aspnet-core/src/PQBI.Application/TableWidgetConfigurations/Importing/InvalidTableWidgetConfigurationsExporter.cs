using System.Collections.Generic;
using Abp.Collections.Extensions;
using PQBI.TableWidgetConfigurations.Importing.Dto;
using PQBI.DataExporting.Excel.MiniExcel;
using PQBI.DataImporting.Excel;
using PQBI.Dto;
using PQBI.Storage;

namespace PQBI.TableWidgetConfigurations
{
    public class InvalidTableWidgetConfigurationExporter(ITempFileCacheManager tempFileCacheManager)
        : MiniExcelExcelExporterBase(tempFileCacheManager), IExcelInvalidEntityExporter<ImportTableWidgetConfigurationDto>
    {
        public FileDto ExportToFile(List<ImportTableWidgetConfigurationDto> tableWidgetConfigurationList)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var tableWidgetConfiguration in tableWidgetConfigurationList)
        {
            items.Add(new Dictionary<string, object>()
                {
                    {"Refuse Reason", tableWidgetConfiguration.Exception},
                    {"Configuration", tableWidgetConfiguration.Configuration},
                    {"Components", tableWidgetConfiguration.Components},
                    {"DateRange", tableWidgetConfiguration.DateRange}
                });
        }

        return CreateExcelPackage("InvalidTableWidgetConfigurationImportList.xlsx", items);
    }
}
}