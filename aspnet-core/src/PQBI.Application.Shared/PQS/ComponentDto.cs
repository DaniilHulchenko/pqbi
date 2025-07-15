using PQS.Data.Measurements;
using System.Collections.Generic;

namespace PQBI.PQS;

public class AdditionalData
{
    public string PropertyName { get; set; }
    public string Base { get; set; }
    public MeasurmentsParameterDetails MeasurmentsParameterDetails { get; set; } = null;

    public override bool Equals(object obj)
    {
        if (obj is AdditionalData additionalData && additionalData.MeasurmentsParameterDetails is not null)
        {
            return additionalData.MeasurmentsParameterDetails.Units == MeasurmentsParameterDetails.Units &&
             additionalData.MeasurmentsParameterDetails.SupportInstantParam == MeasurmentsParameterDetails.SupportInstantParam &&
             additionalData.MeasurmentsParameterDetails.Name == MeasurmentsParameterDetails.Name;

        }

        return false;
    }


    public override int GetHashCode()
    {
        return MeasurmentsParameterDetails.Units.GetHashCode() ^ MeasurmentsParameterDetails.SupportInstantParam.GetHashCode() ^
            MeasurmentsParameterDetails.Name.GetHashCode();
    }
}

public class ComponentSlimDto
{
    public ComponentSlimDto(string componentId, string componentName, List<FeederDescriptionDto> feeders, List<ChannelDescriptionDto> channels, IEnumerable<AdditionalData> additionalDatas )
    {
        ComponentId = componentId;
        ComponentName = componentName;
        Feeders = feeders;
        Channels = channels;
        AdditionalDatas = additionalDatas;
    }


    public string ComponentId { get; }
    public string ComponentName { get; }
    public List<FeederDescriptionDto> Feeders { get; }
    public List<ChannelDescriptionDto> Channels { get; }
    public IEnumerable<AdditionalData> AdditionalDatas { get; }

}

public class ComponentDto : ComponentSlimDto
{
    public ComponentDto(string componentId, string componentName, List<FeederDescriptionDto> feeders, List<ChannelDescriptionDto> channels,IEnumerable<AdditionalData> additionalDatas)
        : base(componentId, componentName, feeders, channels, additionalDatas)
    {
    }

    public List<TagDto> Tags { get; set; } = new List<TagDto>();
    public List<string> ParameterInfos { get; set; } = new List<string>();
}


public class StaticDataInfo
{
    public StaticTreeNode StaticTreeNode { get; set; }

    public IEnumerable<AdditionalData> AdditionalDatas { get; set; }

}

public abstract class TreeParameterNodeBase
{
    public const string RootLabel = "Root";
    public const string LogicalLabel = "Logical";
    public const string ChannelLabel = "Channel";
    
    public string Value { get; set; }

    public string Description { get; set; }

}

public class StaticTreeNode : TreeParameterNodeBase
{
    public string? Range { get; set; } = null;
    public List<StaticTreeNode> Children { get; set; } = new List<StaticTreeNode>();

}

public class DynamicTreeNode : TreeParameterNodeBase
{
    public List<DynamicTreeNode> Children { get; set; } = new List<DynamicTreeNode>();
}



public record TagDto(string TagName, string TagValue)
{
    public string TagDescription => $"{TagName}:{TagValue}";
}

public record FeederDescriptionDto(uint Id, string Name);
public record ChannelDescriptionDto(uint Id, string Name);

