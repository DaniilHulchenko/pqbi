using PQBI.PQS.Cache.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQBI.PQS
{
    public class GetComponentByTagsRequest
    {
        public IEnumerable<string> Tags { get; set; }
    }

    public class GetComponentByTagsResponse
    {
        public TagWithComponents[] Components { get; set; }
    }
}
