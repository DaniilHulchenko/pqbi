using Abp.Runtime.Validation;
using Newtonsoft.Json;
using PQS.Data.Events.Enums;
using System.Collections.Generic;
using PQBI.CalculationEngine.Matrix;
using PQBI.CalculationEngine.Functions;
using System;

namespace PQBI.PQS.CalcEngine
{

    public class BarChartRequest : WidgetValidationBase, ICustomValidate
    {
        public string WidgetName { get; set; }
        public required DimensionSelector Category { get; set; }
        public required DimensionSelector SeriesBy { get; set; }
        public List<FeederComponentInfo> Feeders { get; set; } = [];
        public List<BarParameter> BarPrmList { get; set; }       

        public void AddValidationErrors(CustomValidationContext ctx)
        {
            if (ValidationErrors(ctx) == false)
            {
                return;
            }

            if (Feeders.Count == 0)
                ctx.Results.Add(new("Feeders cannot be empty."));

            if (new HashSet<FeederComponentInfo>(Feeders).Count != Feeders.Count)
                ctx.Results.Add(new("Feeders list contains duplicates."));

            if (BarPrmList.Count == 0)
                ctx.Results.Add(new("At least one BarParameter is required."));

        }
    }

    public enum DimensionType 
    { 
        Dates, 
        Parameters, 
        Feeders,
        CustomGroup 
    }

    public sealed record DimensionSelector(
        DimensionType Type,
        Guid? Id,      
        string? Name);

    public sealed record BarParameter(
        [property: JsonProperty("parameter_type")]
        string ParameterType,

        [property: JsonProperty("exclude_flagged")]
        List<EventClass> ExcludeFlagged,

        bool IsExcludeFlaggedData,

        [property: JsonProperty("custom_data")]
        CustomWidgetTableData CustomData,

        [property: JsonProperty("base_data")]
        string BaseData,

        [property: JsonProperty("event_data")]
        TableWidgetEvent TableEvent,

        string ParameterName) : IWidgetParameter;


    public class BarChartEventBase
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Header { get; set; }
        public string AggregationFunc { get; set; }
        public int EventClass { get; set; }

    }

    /// A single bar (one rectangle on the chart)  
    public record BarItem
    {
        public string SeriesName { get; set; } = default!;
        public double? Value { get; init; }
        public DataUnitType DataUnitType { get; init; }
        public PqbiDataValueStatus Status { get; init; }

        public BarItem(string seriesName, double? value, DataUnitType dataUnitType, PqbiDataValueStatus status)
        {
            SeriesName = seriesName;
            Value = value;
            DataUnitType = dataUnitType;
            Status = status;
        }
    }

    /// All bars that share the same X-axis tick
    public record BarGroup(
        string Category,   // The label shown on the X axis
        List<BarItem> Bars);      // One or more bars that sit side-by-side

    /// Full DTO returned to the web client
    public record BarChartResponse(
    DataUnitType DataUnitType,
    List<BarGroup> Groups);      // Extend with extra fields when needed



    //public class BarParameter //: ITableParameterDisplay
    //{
    //    [JsonProperty("parameter_type")]
    //    public string ParameterType { get; set; }      

    //    [JsonProperty("exclude_flagged")]
    //    public List<EventClass> ExcludeFlagged { get; set; } = new List<EventClass>();

    //    public bool IsExcludeFlaggedData { get; set; }      

    //    [JsonProperty("custom_data")]
    //    public CustomWidgetTableData CustomData { get; set; }

    //    [JsonProperty("base_data")]
    //    public string BaseData { get; set; } // Can replace with a typed model if needed

    //    [JsonProperty("event_data")]
    //    public TableWidgetEvent TableEvent { get; set; } // Can replace with a typed model if needed

    //    public string ParameterName { get; set; }
    //}

    //public class BarChartEventRequest : BarChartEventBase
    //{
    //    //public int EventClass { get; set; }

    //}

    //public class BarChartEventResponse : BarChartEventBase
    //{
    //    public double Data { get; set; }

    //}

    //    public record BarChartEvent(
    //        string Type,
    //        string Name,
    //        string Header,
    //        string AggregationFunc,

    //        double EventClass
    //);
}
