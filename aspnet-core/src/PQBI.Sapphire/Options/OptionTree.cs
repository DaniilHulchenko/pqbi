using PQS.Data.Events.Enums;
using MeasurementGroup = PQS.Data.Measurements.Enums.Group;

namespace PQBI.Sapphire.Options;

public class OptionTree
{
    public List<ParameterNode> LogicalParameters { get; set; } = new List<ParameterNode>();
    public List<ParameterNode> ChannelParameters { get; set; } = new List<ParameterNode>();
}

public class ParameterNode
{
    public ParameterNode(MeasurementGroup group, string description)
    {
        Group = group;
        Description = description;
    }

    public MeasurementGroup Group { get; }
    public string Description { get; }

    public List<PhaseNode> Phases { get; set; } = new List<PhaseNode>();
    public List<ChannelNodeDto> Channels { get; set; } = new List<ChannelNodeDto>();
    public List<BaseOnNode> BaseOns { get; } = new List<BaseOnNode>();

}

public abstract class PhaseChannelNode
{

    protected PhaseChannelNode(string description)
    {
        Description = description;
    }
    public string Description { get; }
}

public class PhaseNode : PhaseChannelNode
{
    public PhaseNode(PhaseMeasurementWithDuplicationsEnum phase, string description)
        : base(description)
    {
        Phase = phase;
    }

    public PhaseMeasurementWithDuplicationsEnum Phase { get; }
}

public class ChannelNodeDto : PhaseChannelNode
{
    public ChannelNodeDto(int channelNum, string description)
       : base(description)
    {
        ChannelNum = channelNum;
    }

    public int ChannelNum { get; }
}

public class BaseOnNode
{
    public BaseOnNode(string description)
    {
        Description = description;
    }

    public string Description { get; }
}

