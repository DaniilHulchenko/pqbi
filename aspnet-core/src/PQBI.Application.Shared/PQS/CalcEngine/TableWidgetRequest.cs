using Abp.Collections.Extensions;
using Abp.Extensions;
using Abp.Runtime.Validation;
using Newtonsoft.Json;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Matrix;
using PQBI.Infrastructure.Extensions;
using PQS.Data.Common;
using PQS.Data.Events.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PQBI.PQS.CalcEngine;

public enum CustomParameterType
{
    SPMC,
    MPSC,
    Exception,
    BPCP
}

public enum InternaParameterType
{
    None = 0,
    CustomParameters,
    BaseParameters
}



public enum TrendWidgetParameterType
{
    CustomParameter,
    BaseParameter,
    Exception
}


public enum ParameterListItemType
{
    Logical,
    Channel,
    Exception,
    Additional
}


public enum WidgetTableParameterType : uint
{
    Deviation = 0,
    Duration = 1,
    Value = 2
}

public class TableWidgetRequest : WidgetValidationBase, ICustomValidate
{
    public string WidgetName { get; set; }
    public int UserTimeZone { get; set; }
    public RowWidgetTable Rows { get; set; }
    public List<ColumnWidgetTable> ColumnWidgetTables { get; set; }



    public void AddValidationErrors(CustomValidationContext context)
    {

        if (ValidationErrors(context) == false)
        {
            return;
        }

        if (Rows is null)
        {
            context.Results.Add(new ValidationResult($"{nameof(Rows)} - Cannot be empty"));
            return;
        }

        if (Rows.Feeders.IsCollectionEmpty())
        {
            context.Results.Add(new ValidationResult($"{nameof(RowWidgetTable.Feeders)} - Cannot be empty"));
            return;
        }

        var feeders = new HashSet<FeederComponentInfo>(Rows.Feeders);
        if (feeders.Count != Rows.Feeders.Count)
        {
            context.Results.Add(new ValidationResult($"{nameof(RowWidgetTable.Feeders)} - Should not be any duplication."));
            return;
        }

        if (Rows.Tags.IsCollectionEmpty() == false)
        {
            foreach (var tag in Rows.Tags)
            {
                if (tag.Id.IsNullOrEmpty())
                {
                    context.Results.Add(new ValidationResult($"{nameof(TagTableWidget.Id)} - Should not be empty."));
                    return;
                }

                if (tag.Name.IsNullOrEmpty())
                {
                    context.Results.Add(new ValidationResult($"{nameof(TagTableWidget.Name)} - Should not be empty."));
                    return;
                }

                if (tag.Feeders.IsCollectionEmpty())
                {
                    context.Results.Add(new ValidationResult($"{nameof(TagTableWidget.Feeders)} - Should not be empty."));
                    return;
                }

                //foreach (var feeder in tag.Feeders)
                //{
                //    if (feeders.Contains(feeder) == false)
                //    {
                //        context.Results.Add(new ValidationResult($"{nameof(TagTableWidget.Feeders)} - {feeder} doesnt exists in main {nameof(TagTableWidget.Feeders)} section."));
                //        return;
                //    }
                //}
            }
        }
    }
}

public class RowWidgetTable
{
    public List<FeederComponentInfo> Feeders { get; set; } = [];
    public List<TagTableWidget> Tags { get; set; } = [];
}

public class TagTableWidget
{
    public string Name { get; set; }
    public string Id { get; set; }
    public List<FeederComponentInfo> Feeders { get; set; } = new List<FeederComponentInfo>();

    public override int GetHashCode()
    {
        return Id.GetHashCode() ^ Name.GetHashCode(); ;
    }

    public override bool Equals(object obj)
    {
        if (obj is TagTableWidget tag)
        {
            return tag.Id.Equals(tag.Id) && tag.Name.Equals(tag.Name);
        }

        return false;
    }
}

public class ColumnEventData
{
    [JsonProperty("event")]
    public ColumnEventInfo Event { get; set; }

    [JsonProperty("phases")]
    public List<string> Phases { get; set; } = new();

    [JsonProperty("parameter")]
    public string Parameter { get; set; }          // "Deviation", "Duration", …

    [JsonProperty("isPolyphase")]
    public bool IsPolyphase { get; set; }

    [JsonProperty("aggregationInSeconds")]
    public int AggregationInSeconds { get; set; }
}

public class ColumnEventInfo
{
    [JsonProperty("eventClass")]
    public int EventClass { get; set; }

    [JsonProperty("alias")]
    public string Alias { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
    [JsonProperty("eventConfID")]
    public int EventConfID { get; set; }
}

public enum NormalizeEnum
{
    NO, NOMINAL, VALUE
}
//public enum LimitType
//{
//    NONE, PERCENT_FROM_NOMINAL, PERCENT_FROM_VAL
//}

//public enum ColorSchemeType
//{
//    NONE, OUT_OF_LIMITS, GRADIENT
//}

public class ColorWidgetTable
{
    [JsonProperty("out_of_limits")]
    public string OutOfLimits { get; set; }

    [JsonProperty("from")]
    public string From { get; set; }

    [JsonProperty("to")]
    public string To { get; set; }

    [JsonProperty("ok")]
    public string Ok { get; set; }

    [JsonProperty("no_data")]
    public string NoData { get; set; }
}

public class ColumnWidgetTable //: ITableParameterDisplay
{
    [JsonProperty("parameter_type")]
    public string ParameterType { get; set; }

    //[JsonProperty("flagging_events")]
    //public List<int> FlaggingEvents { get; set; }

    [JsonProperty("normalize")]
    public NormalizeEnum Normalize { get; set; }

    [JsonProperty("normal_value")]
    public double? NormalValue { get; set; }

    [JsonProperty("exclude_flagged")]
    public List<EventClass> ExcludeFlagged { get; set; } = new List<EventClass>();
  
    public bool IsExcludeFlaggedData { get; set; }

    [JsonProperty("ignore_aligning_function")]
    public bool IgnoreAligningFunction { get; set; }

    [JsonProperty("replace_aggregation_with")]
    public string? ReplaceAggregationWith { get; set; } = null;

    //[JsonProperty("limit_type")]
    //public LimitType LimitType { get; set; }

    //[JsonProperty("lower_limit_value")]
    //public double LowerLimitValue { get; set; }

    //[JsonProperty("upper_limit_value")]
    //public double UpperLimitValue { get; set; }

    //[JsonProperty("color_scheme_type")]
    //public ColorSchemeType ColorSchemeType { get; set; }

    //[JsonProperty("colors")]
    //public ColorWidgetTable Colors { get; set; }

    [JsonProperty("custom_data")]
    public CustomWidgetTableData CustomData { get; set; }

    [JsonProperty("base_data")]
    public string BaseData { get; set; } // Can replace with a typed model if needed

    [JsonProperty("event_data")]
    public TableWidgetEvent TableEvent { get; set; } // Can replace with a typed model if needed

    public string ParameterName { get; set; }
}

public class CustomWidgetTableData
{
    public int Id { get; set; }
    public bool IgnoreAlignment { get; set; }
    public string Quantity { get; set; }
}



//public class TableWidgetRequest : WidgetValidationBase, ICustomValidate
//{
//    public List<TableWidgetComponent> Components { get; set; } = new List<TableWidgetComponent>();
//    public List<TableWidgetParameter> Parameters { get; set; } = new List<TableWidgetParameter>();

//    public List<FeederComponentInfo> Feeders { get; set; } = new List<FeederComponentInfo>();

//    public string WidgetName { get; set; }



//    public void AddValidationErrors(CustomValidationContext context)
//    {
//        if (ValidationErrors(context) == false)
//        {
//            return;
//        }


//        if (Parameters is null || Parameters.Count == 0)
//        {
//            context.Results.Add(new ValidationResult($"{nameof(Parameters)} - Cannot be empty"));
//            return;
//        }


//        foreach (var param in Parameters)
//        {
//            if (string.IsNullOrEmpty(param.Quantity))
//            {
//                context.Results.Add(new ValidationResult($"{nameof(TableWidgetParameter.Quantity)} - Cannot be empty"));
//                return;
//            }

//            if (string.IsNullOrEmpty(param.Data))
//            {
//                context.Results.Add(new ValidationResult($"{nameof(TableWidgetParameter.Data)} - Cannot be empty"));
//                return;
//            }

//            if (string.IsNullOrEmpty(param.ParameterName))
//            {
//                context.Results.Add(new ValidationResult($"{nameof(TableWidgetParameter.ParameterName)} - Cannot be empty"));
//                return;
//            }

//            if (Enum.TryParse(param.Type, true, out TableWidgetParameterType columnParameterType))
//            {
//                switch (columnParameterType)
//                {
//                    case TableWidgetParameterType.CustomParameter:
//                        if ((Components is null || Components.Count == 0) && (Feeders is null || Feeders.Count == 0))
//                        {
//                            context.Results.Add(new ValidationResult($"Both feeders and Channels - Cannot be empty."));
//                        }

//                        if (int.TryParse(param.Data, out _) == false)
//                        {
//                            context.Results.Add(new ValidationResult($"In custom parameter a data field should be int {nameof(TableWidgetParameter)}."));
//                        }
//                        break;

//                    case TableWidgetParameterType.Event:
//                        if ((Components is null || Components.Count == 0) && (Feeders is null || Feeders.Count == 0))
//                        {
//                            context.Results.Add(new ValidationResult($"Both feeders and Channels - Cannot be empty."));
//                        }

//                        try
//                        {

//                            var tableEvent = JsonConvert.DeserializeObject<TableWidgetEvent>(param.Data) ?? throw new Exception();


//                            if (!Enum.IsDefined(typeof(EventClass), tableEvent.EventId))
//                            {
//                                context.Results.Add(new ValidationResult($"{nameof(TableWidgetEvent.EventId)} should be part of {nameof(EventClass)}."));
//                                return;
//                            }

//                        }
//                        catch
//                        {
//                            context.Results.Add(new ValidationResult($"{nameof(ParameterListDto.Data)} should be of typr {nameof(TableWidgetEvent)}."));
//                            return;
//                        }
//                        break;

//                    //case TrendWidgetParameterType.Exception:
//                    //    if ((param.Feeders is not null && param.Feeders.Count > 0) || (param.ApplyToDos is not null && param.ApplyToDos.Count > 0))
//                    //    {
//                    //        context.Results.Add(new ValidationResult($"Both feeders and Channels in exception mode should be empty."));
//                    //    }
//                    //    break;
//                    default:
//                        break;
//                }

//            }
//            else
//            {
//                context.Results.Add(new ValidationResult($"Type cann be only part of {nameof(TableWidgetParameterType)}"));
//            }
//        }
//    }

public record TableWidgetComponent(string ComponentId, string ComponentName, List<string> Tags); //: IApplyTo;
public class TableWidgetParameter //: ITableParameterDisplay
{
    public string Type { get; set; } // TableWidgetParameterType
    public string ParameterName { get; set; }
    public string Quantity { get; set; }
    //public string AggregationFunc { get; set; } // Average, Min, Max, Count, etc.
    public string Data { get; set; } // Changed to string to match 'any' type of TableWidgetParameterType
                                     //public bool ShowFlagged { get; set; }
                                     //public bool Normalize { get; set; } // no, nominal, custom
                                     //public double? NormalizationValue { get; set; } // custom normalization value
}

public enum TableWidgetParameterType
{
    BaseParameter,
    CustomParameter,
    Exception,
    Event
}


public class TableWidgetEvent
{
    public List<string> Phases { get; set; } = new List<string>();
    public uint EventId { get; set; }
    public EventClass EventClass { get; set; }
    public bool IsShared { get; set; }
    //public string Parameter { get; set; }
    public WidgetTableParameterType Parameter { get; set; }

    public bool IsPolyphase { get; set; }
    public int? AggregationInSeconds { get; set; }

    public string Quantity { get; set; }
}


//public class EventTypeInfo
//{
//    // if your JSON uses camelCase, either use JsonPropertyName
//    [JsonPropertyName("eventId")]
//    public uint EventId { get; set; }

//    [JsonPropertyName("eventClass")]
//    public EventClass EventClass { get; set; }

//    [JsonPropertyName("isShared")]
//    public bool IsShared { get; set; }
//}

public class TableWidgetResponse
{
    public List<TableWidgetResponseItem> Items { get; set; } = new List<TableWidgetResponseItem>();
}

public record ErrorInfo(int Status);

public class Tag
{
    public string? TagId { get; set; }
    public string? TagValue { get; set; }
}

public class TableWidgetResponseItem
{
    public string? ComponentId { get; set; } = null;
    public int? FeederId { get; set; } = null;

    public Tag Tag { get; set; } = null;

    //public string? TagId { get; set; } = null;
    //public string? TagValue { get; set; } = null;


    public string ParameterName { get; set; }
    //public string AggregationFunc { get; set; }
    public double? Calculated { get; set; }
    public PqbiDataValueStatus DataValueStatus { get; set; }
    public string Quantity { get; set; }

    public MissingBaseParameterInfo MissingBaseParameterInfo { get; set; }

    public DataUnitType DataUnitType { get; set; }

    //public ErrorInfo ErrorInfo { get; set; } = null;


    //public bool ShowFlagged { get; set; }
    //public bool Normalize { get; set; } // no, nominal, custom
    //public double? NormalizationValue { get; set; } // no, nominal, custom
    //public double NormalizationCalculated { get; set; } // no, nominal, custom

    //public string Type { get; set; } //

}

