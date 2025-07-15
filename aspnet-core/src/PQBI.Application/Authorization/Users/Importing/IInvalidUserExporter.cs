using System.Collections.Generic;
using PQBI.Authorization.Users.Importing.Dto;
using PQBI.Dto;

namespace PQBI.Authorization.Users.Importing
{
    public interface IInvalidUserExporter
    {
        FileDto ExportToFile(List<ImportUserDto> userListDtos);
    }
}
