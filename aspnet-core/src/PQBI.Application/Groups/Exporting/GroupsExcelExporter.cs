using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using PQBI.DataExporting.Excel.MiniExcel;
using PQBI.Groups.Dtos;
using PQBI.Dto;
using PQBI.Storage;

namespace PQBI.Groups.Exporting;

public class GroupsExcelExporter : MiniExcelExcelExporterBase, IGroupsExcelExporter
{
    private readonly ITimeZoneConverter _timeZoneConverter;
    private readonly IAbpSession _abpSession;

    public GroupsExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager) :
base(tempFileCacheManager)
    {
        _timeZoneConverter = timeZoneConverter;
        _abpSession = abpSession;

    }

    public FileDto ExportToFile(List<GetGroupForViewDto> groups)
    {

        var items = new List<Dictionary<string, object>>();

        foreach (var group in groups)
        {
            items.Add(new Dictionary<string, object>()
                    {
                        {"Name", group.Group.Name},
                        {"Subgroups", group.Group.Subgroups},

                    });
        }

        return CreateExcelPackage("GroupsList.xlsx", items);

    }

}