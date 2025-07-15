using System.Collections.Generic;
using PQBI.Authorization.Users.Importing.Dto;
using Abp.Dependency;

namespace PQBI.Authorization.Users.Importing
{
    public interface IUserListExcelDataReader: ITransientDependency
    {
        List<ImportUserDto> GetUsersFromExcel(byte[] fileBytes);
    }
}
