using Abp.Extensions;
using Microsoft.Extensions.Logging;

namespace PQBI.Logs
{

    public abstract class LogBase
    {
        public const string PQS_Subject_Name = "PQSSubject";
        public abstract string PQSSubject { get; }

        public abstract string StructureTemplate();
        public abstract void NormalizeLog(ILogger logger);

    }

  

    public abstract class TenantLogBase : LogBase
    {
        public int TenantId { get; init; }
        public long UserId { get; init; }

        public string Message { get; init; }

        public override string StructureTemplate()
        {
            if (!Message.IsNullOrEmpty())
            {
                return $"{PQS_Subject_Name} = {{{PQS_Subject_Name}}} User = {{@User}} message = {Message}";
            }


            return $"{PQS_Subject_Name} = {{{PQS_Subject_Name}}} User = {{@User}}";
        }

        public override void NormalizeLog(ILogger logger)
        {
            logger.LogInformation(StructureTemplate(), PQSSubject, new { TenantId, UserId });
        }
    }


    public static class LogBaseExtensions
    {
        public static void LogSession(this ILogger logger, int tenantId, long userId, string message = null)
        {
            var log = new PqsSessionLog(tenantId, userId, message);
            log.NormalizeLog(logger);
        }
    }

    public class PqsSessionLog : TenantLogBase
    {
        public override string PQSSubject => "Nop_Session";

        public PqsSessionLog(int tenantId, long userId, string message)
        {
            TenantId = tenantId;
            UserId = userId;
            Message = message;
        }
    }

}
