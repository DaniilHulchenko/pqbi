using System.Threading.Tasks;
using PQBI.Security.Recaptcha;

namespace PQBI.Test.Base.Web
{
    public class FakeRecaptchaValidator : IRecaptchaValidator
    {
        public Task ValidateAsync(string captchaResponse)
        {
            return Task.CompletedTask;
        }
    }
}
