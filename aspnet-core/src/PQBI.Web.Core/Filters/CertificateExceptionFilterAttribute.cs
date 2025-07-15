using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using PQBI.Web.Exceptions;
using System;

namespace PQBI.Web.Filters
{
    public class CertificateExceptionFilterAttribute : Attribute, IExceptionFilter
    {
        private readonly ILogger<CertificateExceptionFilterAttribute> _logger;



        public CertificateExceptionFilterAttribute(ILogger<CertificateExceptionFilterAttribute> logger)
        {
            this._logger = logger;
        }
        public void OnException(ExceptionContext context)
        {
            var result = new ObjectResult(context.Exception.Message)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            if (context.Exception is CertificateException certificateException)
            {
                _logger.LogError(certificateException.ToString());
                context.Result = new ObjectResult("PQS Service connection failure. Invalid certificate.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };

                return;
            }

            context.Result = result;
        }
    }
}
