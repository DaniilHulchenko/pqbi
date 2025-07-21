using Abp.Collections.Extensions;
using Abp.Extensions;
using Abp.Runtime.Validation;
using Newtonsoft.Json;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Matrix;
using PQBI.Infrastructure.Extensions;
using PQS.Data.Common;
using PQS.Data.Common.Values;
using PQS.Data.Events.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using static Abp.Domain.Uow.AbpDataFilters;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    Custom
}


public enum WidgetTableParameterType : uint
{
    Deviation = 0,
    Duration = 1,
    Value = 2
}

public class TableWidgetRequest222 : WidgetValidationBase, ICustomValidate
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

                foreach (var feeder in tag.Feeders)
                {
                    if (feeders.Contains(feeder) == false)
                    {
                        context.Results.Add(new ValidationResult($"{nameof(TagTableWidget.Feeders)} - {feeder} doesnt exists in main {nameof(TagTableWidget.Feeders)} section."));
                        return;
                    }
                }
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

public class ColumnWidgetTable //: ITableParameterDisplay
{
    [JsonProperty("parameter_type")]
    public string ParameterType { get; set; }

    [JsonProperty("flagging_events")]
    public List<int> FlaggingEvents { get; set; }

    [JsonProperty("exclude_flagged")]
    public bool ExcludeFlagged { get; set; }

    [JsonProperty("custom_data")]
    public CustomWidgetTableData CustomData { get; set; }

    [JsonProperty("base_data")]
    public string BaseData { get; set; } // Placeholder for detailed fields

    [JsonProperty("event_data")]
    public TableWidgetEvent TableEvent { get; set; } // Placeholder for detailed fields
    public string ParameterName { get; set; }
    //public string Quantity => CustomData?.Quantity;
}

public class CustomWidgetTableData
{
    public int Id { get; set; }
    public bool IgnoreAlignment { get; set; }
    public string Quantity { get; set; }
}


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
    public ushort EventId { get; set; }
    //public string Parameter { get; set; }
    public WidgetTableParameterType Parameter { get; set; }

    public bool IsPolyphase { get; set; }
    public int? AggregationInSeconds { get; set; }

    public string Quantity { get; set; }
}


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
    public string? FeederId { get; set; } = null;

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

