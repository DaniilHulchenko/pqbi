using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.Extensions;
using Abp.ObjectMapping;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using PQBI.CustomParameters.Importing.Dto;
using PQBI.CustomParameters.Dtos;
using PQBI.DataImporting.Excel;
using PQBI.Notifications;
using PQBI.Storage;
using System;
using Abp.UI;

namespace PQBI.CustomParameters;

public class ImportCustomParametersToExcelJob(
    IObjectMapper objectMapper,
    IUnitOfWorkManager unitOfWorkManager,
    CustomParameterListExcelDataReader dataReader,
    InvalidCustomParameterExporter invalidEntityExporter,
    IAppNotifier appNotifier,
    IRepository<CustomParameter> customParameterRepository,

    IBinaryObjectManager binaryObjectManager)
    : ImportToExcelJobBase<ImportCustomParameterDto, CustomParameterListExcelDataReader, InvalidCustomParameterExporter>(appNotifier,
        binaryObjectManager, unitOfWorkManager, dataReader, invalidEntityExporter)
{
    public override string ErrorMessageKey => "FileCantBeConvertedToCustomParameterList";

public override string SuccessMessageKey => "AllCustomParametersSuccessfullyImportedFromExcel";

protected override async Task CreateEntityAsync(ImportCustomParameterDto entity)
{
    var customParameter = objectMapper.Map<CustomParameter>(entity);

    // Add your custom validation here.

    await customParameterRepository.InsertAsync(customParameter);
}
}