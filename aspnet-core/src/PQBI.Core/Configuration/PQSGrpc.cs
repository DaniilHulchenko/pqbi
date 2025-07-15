using PQBI.Infrastructure;

namespace PQBI.Configuration
{
    public class PQSGrpc : PQSConfig<PQSGrpc>
    {
        public bool IsAllCertificatesTrusted { get; set; }
        public string PQSServiceUrl { get; set; }
    }
}
