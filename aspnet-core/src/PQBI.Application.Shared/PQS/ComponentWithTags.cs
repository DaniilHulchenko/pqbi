using System.Collections.Generic;

namespace PQBI.PQS
{
    public class ComponentWithTagsResponse
    {
        public ComponentWithTagDtos[] Data { get; set; }

    }
    public class ComponentWithTagDtos
    {
        public string TagName { get; init; }
        public string TagValue { get; init; }

        public string[] ComponentIds { get; set; } 

    }

}

