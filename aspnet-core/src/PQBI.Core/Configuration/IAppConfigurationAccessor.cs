using Microsoft.Extensions.Configuration;

namespace PQBI.Configuration
{
    public interface IAppConfigurationAccessor
    {
        IConfigurationRoot Configuration { get; }
    }
}
