using PQBI.BarChartWidgetConfigurations;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.Extensions;
using Abp.ObjectMapping;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using PQBI.BarChartWidgetConfigurations.Importing.Dto;
using PQBI.BarChartWidgetConfigurations.Dtos;
using PQBI.DataImporting.Excel;
using PQBI.Notifications;
using PQBI.Storage;
using System;
using Abp.UI;

namespace PQBI.BarChartWidgetConfigurations
{

    public class ImportBarChartWidgetConfigurationsToExcelJob(
        IObjectMapper objectMapper,
        IUnitOfWorkManager unitOfWorkManager,
        BarChartWidgetConfigurationListExcelDataReader dataReader,
        InvalidBarChartWidgetConfigurationExporter invalidEntityExporter,
        IAppNotifier appNotifier,
        IRepository<BarChartWidgetConfiguration> barChartWidgetConfigurationRepository,

        IBinaryObjectManager binaryObjectManager)
        : ImportToExcelJobBase<ImportBarChartWidgetConfigurationDto, BarChartWidgetConfigurationListExcelDataReader, InvalidBarChartWidgetConfigurationExporter>(appNotifier,
            binaryObjectManager, unitOfWorkManager, dataReader, invalidEntityExporter)
    {
        public override string ErrorMessageKey => "FileCantBeConvertedToBarChartWidgetConfigurationList";

    public override string SuccessMessageKey => "AllBarChartWidgetConfigurationsSuccessfullyImportedFromExcel";

    protected override async Task CreateEntityAsync(ImportBarChartWidgetConfigurationDto entity)
    {
        var barChartWidgetConfiguration = objectMapper.Map<BarChartWidgetConfiguration>(entity);

        // Add your custom validation here.

        await barChartWidgetConfigurationRepository.InsertAsync(barChartWidgetConfiguration);
    }
}
}