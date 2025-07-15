using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace PQBI.TableWidgetConfigurations.Dtos
{
    public class GetAllTableWidgetConfigurationsForExcelInput
    {
        public string Filter { get; set; }

        public string ConfigurationFilter { get; set; }

    }
}