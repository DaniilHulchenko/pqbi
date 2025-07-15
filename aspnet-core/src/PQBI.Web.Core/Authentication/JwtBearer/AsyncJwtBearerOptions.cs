using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PQBI.Web.Authentication.JwtBearer
{
    public class AsyncJwtBearerOptions : JwtBearerOptions
    {
        public readonly List<IAsyncSecurityTokenValidator> AsyncSecurityTokenValidators;
        
        private readonly PQBIAsyncJwtSecurityTokenHandler _defaultAsyncHandler = new PQBIAsyncJwtSecurityTokenHandler();

        public AsyncJwtBearerOptions()
        {
            AsyncSecurityTokenValidators = new List<IAsyncSecurityTokenValidator>() {_defaultAsyncHandler};
        }
    }

}
