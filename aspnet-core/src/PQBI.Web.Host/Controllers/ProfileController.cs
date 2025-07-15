using Abp.AspNetCore.Mvc.Authorization;
using PQBI.Authorization.Users.Profile;
using PQBI.Graphics;
using PQBI.Storage;

namespace PQBI.Web.Controllers
{
    [AbpMvcAuthorize]
    public class ProfileController : ProfileControllerBase
    {
        public ProfileController(
            ITempFileCacheManager tempFileCacheManager,
            IProfileAppService profileAppService,
            IImageValidator imageValidator) :
            base(tempFileCacheManager, profileAppService, imageValidator)
        {
        }
    }
}