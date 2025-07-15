using PQBI.BarChartWidgetConfigurations;

using System;
using Abp.AutoMapper;
using PQBI.DataImporting.Excel;
using PQBI.BarChartWidgetConfigurations.Dtos;

namespace PQBI.BarChartWidgetConfigurations.Importing.Dto
{
    [AutoMapTo(typeof(BarChartWidgetConfiguration))]
    public class ImportBarChartWidgetConfigurationDto : ImportFromExcelDto
    {
        public BarChartType Type { get; set; }
        public string Components { get; set; }
        public string Events { get; set; }
        public string DateRange { get; set; }

    }
}