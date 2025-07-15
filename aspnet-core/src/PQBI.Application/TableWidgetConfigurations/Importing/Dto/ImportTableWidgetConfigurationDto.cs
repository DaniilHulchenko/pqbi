using System;
using Abp.AutoMapper;
using PQBI.DataImporting.Excel;
using PQBI.TableWidgetConfigurations.Dtos;

namespace PQBI.TableWidgetConfigurations.Importing.Dto
{
    [AutoMapTo(typeof(TableWidgetConfiguration))]
    public class ImportTableWidgetConfigurationDto : ImportFromExcelDto
    {
        public string Configuration { get; set; }
        public string Components { get; set; }
        public string DateRange { get; set; }

    }
}