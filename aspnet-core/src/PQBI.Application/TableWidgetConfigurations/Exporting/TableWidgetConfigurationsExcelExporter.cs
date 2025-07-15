using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using PQBI.DataExporting.Excel.MiniExcel;
using PQBI.TableWidgetConfigurations.Dtos;
using PQBI.Dto;
using PQBI.Storage;

namespace PQBI.TableWidgetConfigurations.Exporting
{
    public class TableWidgetConfigurationsExcelExporter : MiniExcelExcelExporterBase, ITableWidgetConfigurationsExcelExporter
    {
        private readonly ITimeZoneConverter _timeZoneConverter;
        private readonly IAbpSession _abpSession;

        public TableWidgetConfigurationsExcelExporter(
            ITimeZoneConverter timeZoneConverter,
            IAbpSession abpSession,
            ITempFileCacheManager tempFileCacheManager) :
    base(tempFileCacheManager)
        {
            _timeZoneConverter = timeZoneConverter;
            _abpSession = abpSession;

        }

        public FileDto ExportToFile(List<GetTableWidgetConfigurationForViewDto> tableWidgetConfigurations)
        {

            var items = new List<Dictionary<string, object>>();

            foreach (var tableWidgetConfiguration in tableWidgetConfigurations)
            {
                items.Add(new Dictionary<string, object>()
                    {
                        {"Configuration", tableWidgetConfiguration.TableWidgetConfiguration.Configuration},
                        {"Components", tableWidgetConfiguration.TableWidgetConfiguration.Components},
                        {"DateRange", tableWidgetConfiguration.TableWidgetConfiguration.DateRange},

                    });
            }

            return CreateExcelPackage("TableWidgetConfigurationsList.xlsx", items);

        }

    }
}