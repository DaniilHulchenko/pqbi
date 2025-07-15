using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using PQBI.DataExporting.Excel.MiniExcel;
using PQBI.BarChartWidgetConfigurations.Dtos;
using PQBI.Dto;
using PQBI.Storage;

namespace PQBI.BarChartWidgetConfigurations.Exporting
{
    public class BarChartWidgetConfigurationsExcelExporter : MiniExcelExcelExporterBase, IBarChartWidgetConfigurationsExcelExporter
    {
        private readonly ITimeZoneConverter _timeZoneConverter;
        private readonly IAbpSession _abpSession;

        public BarChartWidgetConfigurationsExcelExporter(
            ITimeZoneConverter timeZoneConverter,
            IAbpSession abpSession,
            ITempFileCacheManager tempFileCacheManager) :
    base(tempFileCacheManager)
        {
            _timeZoneConverter = timeZoneConverter;
            _abpSession = abpSession;

        }

        public FileDto ExportToFile(List<GetBarChartWidgetConfigurationForViewDto> barChartWidgetConfigurations)
        {

            var items = new List<Dictionary<string, object>>();

            foreach (var barChartWidgetConfiguration in barChartWidgetConfigurations)
            {
                items.Add(new Dictionary<string, object>()
                    {
                        {"Type", barChartWidgetConfiguration.BarChartWidgetConfiguration.Type},
                        {"Components", barChartWidgetConfiguration.BarChartWidgetConfiguration.Components},
                        {"Events", barChartWidgetConfiguration.BarChartWidgetConfiguration.Events},
                        {"DateRange", barChartWidgetConfiguration.BarChartWidgetConfiguration.DateRange},

                    });
            }

            return CreateExcelPackage("BarChartWidgetConfigurationsList.xlsx", items);

        }

    }
}