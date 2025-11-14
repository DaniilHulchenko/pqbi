using PQS.Data.Events.Enums;

namespace PQBI.Infrastructure.Sapphire;

public class EventClassDescription
{
    public EventClass EventClass { get; set; }
    public uint ConfID { get; set; }
    public bool IsShared { get; set; }
    public string Name { get; set; }
    public AggregationEnum AggregationEnum { get; set; }
    public string Description { get; set; }
}

