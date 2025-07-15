using System;
using System.IO;
using System.Linq;
using Abp.IO.Extensions;
using Abp.UI;
using Abp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using PQBI.Storage;

using Abp.BackgroundJobs;
using PQBI.DataImporting.Excel;
using PQBI.Web.Controllers;
using PQBI.Authorization;
using System.Threading.Tasks;
using PQBI.CustomParameters;

namespace PQBI.Web.Controllers;

[Authorize]
public class CustomParametersController : ExcelImportControllerBase
{
    private readonly ITempFileCacheManager _tempFileCacheManager;

    protected readonly IBinaryObjectManager _binaryObjectManager;
    protected readonly IBackgroundJobManager _backgroundJobManager;

    public override string ImportExcelPermission => AppPermissions.Pages_CustomParameters_Create;

    public CustomParametersController(ITempFileCacheManager tempFileCacheManager, IBinaryObjectManager binaryObjectManager, IBackgroundJobManager backgroundJobManager) : base(binaryObjectManager, backgroundJobManager)
    {
        _tempFileCacheManager = tempFileCacheManager;
    }

    public override async Task EnqueueExcelImportJobAsync(ImportFromExcelJobArgs args)
    {
        await BackgroundJobManager.EnqueueAsync<ImportCustomParametersToExcelJob, ImportFromExcelJobArgs>(args);
    }

}