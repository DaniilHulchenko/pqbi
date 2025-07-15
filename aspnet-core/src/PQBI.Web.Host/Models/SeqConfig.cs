using PQBI.Configuration;
using PQBI.Infrastructure;

namespace PQBI.Web.Models
{
    public class SeqConfig : PQSConfig<SeqConfig>
    {
        public string Url { get; set; }
    }
}
