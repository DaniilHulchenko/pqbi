using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using PQBI.DataExporting.Excel.MiniExcel;
using PQBI.CustomParameters.Dtos;
using PQBI.Dto;
using PQBI.Storage;

namespace PQBI.CustomParameters.Exporting;

public class CustomParametersExcelExporter : MiniExcelExcelExporterBase, ICustomParametersExcelExporter
{
    private readonly ITimeZoneConverter _timeZoneConverter;
    private readonly IAbpSession _abpSession;

    public CustomParametersExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager) :
base(tempFileCacheManager)
    {
        _timeZoneConverter = timeZoneConverter;
        _abpSession = abpSession;

    }

    public FileDto ExportToFile(List<GetCustomParameterForViewDto> customParameters)
    {

        var items = new List<Dictionary<string, object>>();

        foreach (var customParameter in customParameters)
        {
            items.Add(new Dictionary<string, object>()
                    {
                        {"Name", customParameter.CustomParameter.Name},
                        {"AggregationFunction", customParameter.CustomParameter.AggregationFunction},
                        {"Type", customParameter.CustomParameter.Type},

                    });
        }

        return CreateExcelPackage("CustomParametersList.xlsx", items);

    }

}