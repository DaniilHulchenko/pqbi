using System.Collections.Generic;
using System;
using System.Linq;


namespace PQBI.PQS.Cache.Tags;

[Serializable]
public class TagWithComponentCacheItem
{
    public const string CacheName = nameof(TagWithComponentCacheItem);

    public IEnumerable<TagWithComponents> Components { get; set; } = new List<TagWithComponents>();
}


[Serializable]
public class TagWithComponents
{

    public HashSet<string> ComponentIds { get; set; } = new HashSet<string>();

    public string TagName { get; init; }
    public string TagValue { get; init; }


    public static string CreateKey(string key, string value) => $"{key}__{value}";
    public static (string TagName, string TagValue) Unkey(string key)
    {
        var items = key.Split("__");
        return (items.FirstOrDefault(), items.LastOrDefault());
    }

    public override int GetHashCode()
    {
        return CreateKey(TagName, TagValue).GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is TagWithComponents comp)
        {
            return CreateKey(TagName, TagValue).Equals(CreateKey(comp.TagName, comp.TagValue));
        }

        return false;
    }

}