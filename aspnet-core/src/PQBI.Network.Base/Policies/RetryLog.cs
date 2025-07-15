using Microsoft.Extensions.Logging;
using PQBI.Logs;

namespace PQBI.Policies
{
    public class RetryLog : LogBase
    {
        public RetryLog(HttpResponseMessage httpResponse)
        {
            HttpResponse = httpResponse;
        }

        public override string PQSSubject => "Retry_Comunication";

        public HttpResponseMessage HttpResponse { get; }

        public override string StructureTemplate()
        {
            return $"{PQS_Subject_Name} = {{{PQS_Subject_Name}}}  url ={{Url}} response = {{@Response}}";
        }

        public override void NormalizeLog(ILogger logger)
        {
            logger.LogWarning(StructureTemplate(), PQSSubject, HttpResponse.RequestMessage.RequestUri, HttpResponse);
        }
    }


    public static class LogBaseExtensions
    {
        public static void LogCommunication(this ILogger logger, HttpResponseMessage responseMessage)
        {
            var log = new RetryLog(responseMessage);
            log.NormalizeLog(logger);
        }
    }


}
