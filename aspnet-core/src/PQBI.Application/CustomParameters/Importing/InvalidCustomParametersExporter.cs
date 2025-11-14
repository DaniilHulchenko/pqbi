using System.Collections.Generic;
using Abp.Collections.Extensions;

using PQBI.CustomParameters.Importing.Dto;
using PQBI.DataExporting.Excel.MiniExcel;
using PQBI.DataImporting.Excel;
using PQBI.Dto;
using PQBI.Storage;

namespace PQBI.CustomParameters;

public class InvalidCustomParameterExporter(ITempFileCacheManager tempFileCacheManager)
    : MiniExcelExcelExporterBase(tempFileCacheManager), IExcelInvalidEntityExporter<ImportCustomParameterDto>
{
    public FileDto ExportToFile(List<ImportCustomParameterDto> customParameterList)
{
    var items = new List<Dictionary<string, object>>();

    foreach (var customParameter in customParameterList)
    {
        items.Add(new Dictionary<string, object>()
            {
                {"Refuse Reason", customParameter.Exception},
                    {"Name", customParameter.Name},
                    {"AggregationFunction", customParameter.AggregationFunction},
                    {"Type", customParameter.Type},
                    {"InnerCustomParameters", customParameter.InnerCustomParameters},
                    {"ResolutionInSeconds", customParameter.ResolutionInSeconds},
                    {"CustomBaseDataList", customParameter.CustomBaseDataList}
            });
    }

    return CreateExcelPackage("InvalidCustomParameterImportList.xlsx", items);
}
}