using System.Collections.Generic;
using PQBI.Authorization.Users.Dto;
using PQBI.Dto;

namespace PQBI.Authorization.Users.Exporting
{
    public interface IUserListExcelExporter
    {
        FileDto ExportToFile(List<UserListDto> userListDtos, List<string> selectedColumns);
    }
}