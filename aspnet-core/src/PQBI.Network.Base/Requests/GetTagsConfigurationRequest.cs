using PQS.Data.Configurations.Enums;
using PQS.Data.Configurations;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;
using PQS.Data.Common.Values;
using PQS.Data.Configurations.Tag;
using System.Reflection.Emit;
using PQBI.PQS;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Abp.Extensions;
using PQBI.PQS.Cache.Tags;

namespace PQBI.Requests;

public class GetTagsConfigurationRequest : PQSCommonRequest
{
    public GetTagsConfigurationRequest(string session) :
        base(session)
    {
        AddConfigurations();
    }

    protected override void AddConfigurations()
    {
        var sessionConfigurations = new ConfigurationParameterAndValueContainer();
        sessionConfigurations.AddParamWithValue(StandardConfigurationEnum.STD_SESSION_ID, Session);

        var tagMapRecord = new GetInstantConfigurationRecord(null, new List<ConfigurationParameterBase>() { new StandardConfiguration(StandardConfigurationEnum.STD_TAGS_MAP, PQSType.STRING) });
        AddRecord(tagMapRecord);
    }
}


public class GetTagsConfigurationResponse : PQSOperationResponseBase<GetTagsConfigurationRequest>
{
    public GetTagsConfigurationResponse(GetTagsConfigurationRequest request, PQSResponse response) : base(request, response)
    {

    }

    public bool TryGetMap(out IDictionary<string, ComponentWithTagsDto> componentWithTags, out IEnumerable<TagWithComponents> tagWithComponents)
    {
        var result = false;
        componentWithTags = new Dictionary<string, ComponentWithTagsDto>();
        if (TryGetList(out var list, out tagWithComponents))
        {
            foreach (var component in list)
            {
                componentWithTags[component.ComponentId] = component;
            }

            result = true;
        }

        return result;
    }

    public bool TryGetList(out IEnumerable<ComponentWithTagsDto> componentWithTags, out IEnumerable<TagWithComponents> tagWithComponents)
    {
        componentWithTags = null;
        tagWithComponents = null;

        var result = false;

        var tagWithComponentDics = new Dictionary<string, TagWithComponents>();
        var compoWithTags = new Dictionary<Guid, ComponentWithTags>();



        if (TryExtractConfigurationResponseRecord(out var operationResponseRecord))
        {
            if (operationResponseRecord.Configuration.TryGetValue(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_TAGS_MAP), out var tagsMapConf))
            {
                var mapping = new Dictionary<string, Dictionary<BaseValue, List<Guid>>>();
                var ptr = tagsMapConf;

                if (tagsMapConf.ConfigurationValue is PQSValue<string> pvalue)
                {
                    var val = pvalue.Value;
                    var tagMaps = new TagMap(val);
                    var values = tagMaps.GetTags();

                    foreach (var tagVal in values)
                    {
                        var vals = tagMaps.GetValuesAndComponents(tagVal);
                        var valToComp = new Dictionary<BaseValue, List<Guid>>();

                        foreach (var keyAndValue in vals.GetTagValues())
                        {
                            if (keyAndValue is NoValue)
                            {
                                continue;
                            }

                            var ids = vals.GetComponents(keyAndValue).ToArray();
                            valToComp.Add(keyAndValue, ids.ToList());

                            foreach (var idVal in ids)
                            {
                                ComponentWithTags componentWithTags1 = null;
                                if (compoWithTags.TryGetValue(idVal, out componentWithTags1) == false)
                                {
                                    compoWithTags[idVal] = componentWithTags1 = new ComponentWithTags();

                                    componentWithTags1.ComponentId = idVal.ToString();
                                    componentWithTags1.Labels = new Dictionary<string, string>();
                                }

                                TagWithComponents tagWithComponent = null;
                                var key = TagWithComponents.CreateKey(tagVal.ParameterCode.ToString(), keyAndValue.ToString());
                                if (tagWithComponentDics.TryGetValue(key, out tagWithComponent) == false)
                                {
                                    tagWithComponentDics[key] = tagWithComponent = new TagWithComponents
                                    {
                                        TagName = tagVal.ParameterCode.ToString(),
                                        TagValue = keyAndValue.ToString()
                                    };
                                    tagWithComponent.ComponentIds = new HashSet<string>();
                                }

                                componentWithTags1.Labels.Add(tagVal.ParameterCode.ToString(), keyAndValue.ToString());
                                tagWithComponent.ComponentIds.Add(idVal.ToString());
                            }
                        }

                        mapping.Add(tagVal.ParameterCode, valToComp);
                    }
                }

                var componentWithTagList = new List<ComponentWithTagsDto>();

                foreach (var item in compoWithTags)
                {
                    var tags = item.Value.Labels.Select((x) => new TagDto(x.Key, x.Value)).ToArray();
                    componentWithTagList.Add(new ComponentWithTagsDto(item.Key.ToString(), tags));
                }



                result = true;
                componentWithTags = componentWithTagList;
                tagWithComponents = tagWithComponentDics.Values.ToArray();
            }
        }

        return result;
    }

}

//public class TagWithComponents
//{


//    public HashSet<string> ComponentIds { get; set; } = new HashSet<string>();

//    public string TagName { get; init; }
//    public string TagValue { get; init; }


//    public static string CreateKey(string key, string value) => $"{key}__{value}";
//    public static (string TagName,string TagValue) Unkey(string key)
//    {
//        var items = key.Split("__");
//        return (items.FirstOrDefault() , items.LastOrDefault());    
//    }

//    public override int GetHashCode()
//    {
//        return CreateKey(TagName, TagValue).GetHashCode();
//    }

//    public override bool Equals(object obj)
//    {
//        if (obj is TagWithComponents comp)
//        {
//            return CreateKey(TagName, TagValue).Equals(CreateKey(comp.TagName, comp.TagValue));
//        }

//        return false;
//    }

//}


public class ComponentWithTags
{
    public string ComponentId { get; set; }

    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

}

public record ComponentWithTagsDto(string ComponentId, TagDto[] Tags);

