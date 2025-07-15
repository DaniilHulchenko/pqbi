using Abp.AutoMapper;
using PQBI.Authorization.Users.Dto;

namespace PQBI.Mobile.MAUI.Models.User
{
    [AutoMapFrom(typeof(CreateOrUpdateUserInput))]
    public class UserCreateOrUpdateModel : CreateOrUpdateUserInput
    {

    }
}
