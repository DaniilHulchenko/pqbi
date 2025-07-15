using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.Extensions;
using Abp.ObjectMapping;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using PQBI.TableWidgetConfigurations.Importing.Dto;
using PQBI.TableWidgetConfigurations.Dtos;
using PQBI.DataImporting.Excel;
using PQBI.Notifications;
using PQBI.Storage;
using System;
using Abp.UI;

namespace PQBI.TableWidgetConfigurations
{

    public class ImportTableWidgetConfigurationsToExcelJob(
        IObjectMapper objectMapper,
        IUnitOfWorkManager unitOfWorkManager,
        TableWidgetConfigurationListExcelDataReader dataReader,
        InvalidTableWidgetConfigurationExporter invalidEntityExporter,
        IAppNotifier appNotifier,
        IRepository<TableWidgetConfiguration> tableWidgetConfigurationRepository,

        IBinaryObjectManager binaryObjectManager)
        : ImportToExcelJobBase<ImportTableWidgetConfigurationDto, TableWidgetConfigurationListExcelDataReader, InvalidTableWidgetConfigurationExporter>(appNotifier,
            binaryObjectManager, unitOfWorkManager, dataReader, invalidEntityExporter)
    {
        public override string ErrorMessageKey => "FileCantBeConvertedToTableWidgetConfigurationList";

    public override string SuccessMessageKey => "AllTableWidgetConfigurationsSuccessfullyImportedFromExcel";

    protected override async Task CreateEntityAsync(ImportTableWidgetConfigurationDto entity)
    {
        var tableWidgetConfiguration = objectMapper.Map<TableWidgetConfiguration>(entity);

        // Add your custom validation here.

        await tableWidgetConfigurationRepository.InsertAsync(tableWidgetConfiguration);
    }
}
}