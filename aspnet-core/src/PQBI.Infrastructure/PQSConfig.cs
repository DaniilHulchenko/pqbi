namespace PQBI.Infrastructure
{
    public class PQSConfig<TClass> where TClass : class
    {
        public static string ApiName = typeof(TClass).Name;
        public string ConfigurationName => ApiName;
    }
}
