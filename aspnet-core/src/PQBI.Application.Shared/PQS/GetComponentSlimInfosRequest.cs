using System;
using System.Collections.Generic;

namespace PQBI.PQS
{
    public class GetComponentSlimInfosRequest
    {
        public IEnumerable<Guid> ComponentIds { get; set; }
    }

    public class GetComponentSlimInfosResponse
    {
        public IEnumerable<ComponentSlimDto> Components { get; set; }
    }
}
