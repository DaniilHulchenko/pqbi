namespace PQBI.Infrastructure
{
    public class PQSConfig<TClass> where TClass : class
    {
        public static string ApiName = typeof(TClass).Name;
        public string ConfigurationName => ApiName;
    }

    public enum PQSCommunitcationType
    {
        RestApi = 1,
        //Grpc
    }

    public class PQSComunication : PQSConfig<PQSComunication>
    {
        public bool IsAllCertificatesTrusted { get; set; }
        public string PQSServiceRestUrl { get; set; }
        //public string PQSServiceGrpcUrl { get; set; }

        public PQSCommunitcationType DefaultCommunicationType { get; set; }
    }
}


