using Abp.Runtime.Validation;
using Castle.Core.Logging;
using Castle.MicroKernel.Internal;
using Newtonsoft.Json;
using PQBI.Configuration;
using PQBI.Infrastructure;
using PQBI.Infrastructure.Extensions;
using PQS.Data.Measurements.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PQBI.PQS.CalcEngine;

public class WidgetValidationBase
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public double? RefreshRateInSeconds { get; set; }
    public bool IsRealTime { get; set; }
    public string? UserTimeZone { get; set; }
    public int? UtcOffsetMinutes { get; set; }
    public bool IsMondayStartOfWeek { get; set; }

    public ILogger Logger { get; set; } = NullLogger.Instance;
    public uint? UserTimeZoneID { get; set; }

    public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) NormalizeDatesToDateTimeOffset()
    {
        // If you have a real TZ id, you normally shouldn't need offset-only conversion here.
        // (You may still use the TZ later for bucketing/labels.)
        if (!string.IsNullOrWhiteSpace(UserTimeZone))
            return (StartDate.ToUniversalTime(), EndDate.ToUniversalTime());

        // Offset-only mode: interpret the CLOCK part as wall time with the chosen fixed offset
        if (UtcOffsetMinutes.HasValue)
        {
            var offset = TimeSpan.FromMinutes(UtcOffsetMinutes.Value);

            DateTimeOffset ApplyOffsetAsWallTime(DateTimeOffset incoming)
            {
                // Take the wall-clock fields (year/month/day/hour/min/sec) and "stamp" the chosen offset on them
                var wall = DateTime.SpecifyKind(incoming.DateTime, DateTimeKind.Unspecified);
                return new DateTimeOffset(wall, offset).ToUniversalTime();
            }

            return (ApplyOffsetAsWallTime(StartDate), ApplyOffsetAsWallTime(EndDate));
        }

        // Default: treat inputs as instants
        return (StartDate.ToUniversalTime(), EndDate.ToUniversalTime());
    }

    public (DateTime StartUtc, DateTime EndUtc) NormalizeDatesToUtc()
    {
        var (s, e) = NormalizeDatesToDateTimeOffset();
        return (s.UtcDateTime, e.UtcDateTime);
    }

    //public (DateTime, DateTime) NormalizeDatesToUtc()
    //{

    //    // Parse exactly as provided — preserves the offset

    //    var start = DateTimeOffset.Parse(StartDate.ToString());
    //    var end = DateTimeOffset.Parse(EndDate.ToString());
    //    return (start.UtcDateTime, end.UtcDateTime);      

    //}

    //public (DateTimeOffset, DateTimeOffset) NormalizeDatesToDateTimeOffset()
    //{

    //    // Parse exactly as provided — preserves the offset

    //    var start = DateTimeOffset.Parse(StartDate.ToString());
    //    var end = DateTimeOffset.Parse(EndDate.ToString());
    //    return (start.ToUniversalTime(), end.ToUniversalTime());

    //}

    public (DateTime, DateTime, TimeSpan, TimeSpan) GetOffsetAndNormalizeDatesToUtc()
    {

        // Parse exactly as provided — preserves the offset

        var start = DateTimeOffset.Parse(StartDate.ToString());
        var end = DateTimeOffset.Parse(EndDate.ToString());
        return (start.UtcDateTime, end.UtcDateTime, start - start.UtcDateTime, end - end.UtcDateTime);

    }

    public bool IsMondayDefinedStartOfWeek()
    {
        return IsMondayStartOfWeek;
        //return WeekConfiguration.IsMondayStartOfWeek;
    }


    //public (DateTime, DateTime) NormalizeDatesToUtc()
    //{
    //    // Local helper to normalize a single DateTime
    //    DateTime NormalizeSingleDate(DateTime value, TimeZoneInfo tz)
    //    {
    //        switch (value.Kind)
    //        {
    //            case DateTimeKind.Utc:
    //                // Already UTC, don't touch
    //                return value;

    //            case DateTimeKind.Local:
    //                // Interpreted as *server* local time. If that's incorrect in your scenario,
    //                // you might want to treat Local the same as Unspecified and use tz instead.
    //                return value.ToUniversalTime();

    //            case DateTimeKind.Unspecified:
    //            default:
    //                // Interpret as time in the user's time zone and convert once
    //                return TimeZoneInfo.ConvertTimeToUtc(value, tz);
    //        }
    //    }

    //    // No user time zone: just "remove offset" without shifting the clock
    //    // (assume the given values are already UTC times, but Kind may be wrong)
    //    if (string.IsNullOrWhiteSpace(UserTimeZone))
    //    {
    //        var startUtcFallback = DateTime.SpecifyKind(StartDate, DateTimeKind.Utc);
    //        var endUtcFallback = DateTime.SpecifyKind(EndDate, DateTimeKind.Utc);
    //        return (startUtcFallback, endUtcFallback);
    //    }

    //    TimeZoneInfo tz;

    //    try
    //    {
    //        tz = TZConvert.GetTimeZoneInfo(UserTimeZone);
    //        Logger.Warn($"tz = TZConvert.GetTimeZoneInfo(UserTimeZone): {tz.Id}");
    //    }
    //    catch (TimeZoneNotFoundException ex)
    //    {
    //        Logger.Error(
    //            $"NormalizeDatesToUtc FAILED, TimeZoneNotFoundException. UserTimeZone={UserTimeZone}, StartDate={StartDate}, EndDate={EndDate}",
    //            ex);

    //        var startUtcFallback = DateTime.SpecifyKind(StartDate, DateTimeKind.Utc);
    //        var endUtcFallback = DateTime.SpecifyKind(EndDate, DateTimeKind.Utc);
    //        return (startUtcFallback, endUtcFallback);
    //    }
    //    catch (InvalidTimeZoneException ex)
    //    {
    //        Logger.Error(
    //            $"NormalizeDatesToUtc FAILED, InvalidTimeZoneException. UserTimeZone={UserTimeZone}, StartDate={StartDate}, EndDate={EndDate}",
    //            ex);

    //        var startUtcFallback = DateTime.SpecifyKind(StartDate, DateTimeKind.Utc);
    //        var endUtcFallback = DateTime.SpecifyKind(EndDate, DateTimeKind.Utc);
    //        return (startUtcFallback, endUtcFallback);
    //    }

    //    var startUtc = NormalizeSingleDate(StartDate, tz);
    //    var endUtc = NormalizeSingleDate(EndDate, tz);

    //    Logger.Warn($"NormalizeDatesToUtc: StartDate={StartDate:o}, StartUtc={startUtc:o}, Kind={StartDate.Kind}");
    //    Logger.Warn($"NormalizeDatesToUtc: EndDate={EndDate:o}, EndUtc={endUtc:o}, Kind={EndDate.Kind}");

    //    return (startUtc, endUtc);
    //}


    public bool ValidationErrors(CustomValidationContext context)
    {
        if (StartDate >= EndDate)
        {
            context.Results.Add(new ValidationResult($"{nameof(WidgetValidationBase.StartDate)} <  {nameof(WidgetValidationBase.EndDate)}"));
            return false;
        }

        if (RefreshRateInSeconds == null)
            RefreshRateInSeconds = 0;
        if (RefreshRateInSeconds <= 0)
            IsRealTime = false;

        if (TimeZoneNumericMap.TryGetNumericId(UserTimeZone, out int numericID))
        {
            UserTimeZoneID = (uint)numericID;
        }
        else
        {
            UserTimeZoneID = null;
        }

        return true;

    }
}

public class ParameterListDto
{
    public List<FeederComponentInfo> Feeders { get; set; } = new List<FeederComponentInfo>();
    public string Type { get; set; } //CustomParameter{CustomerId}/BaseParameter{SingleBaseParameter}/Exception{CustomerId}
    public string Quantity { get; set; }
    public string Data { get; set; } //CustomerId{numeric}/json
}


public class TrendParameter
{
    public List<FeederComponentInfo> Feeders { get; set; } = new List<FeederComponentInfo>();
    public string Type { get; set; }
    //public string Quantity { get; set; }

    [JsonProperty("custom_data")]
    public TrendCustomWidgetData CustomData { get; set; }

    [JsonProperty("base_data")]
    public BaseData BaseData { get; set; }
    //public TrendBaseData BaseData { get; set; }
}

public class BaseData
{
    [JsonProperty("base_type")]
    public string Type { get; set; } // Logical/Channel/Exception
    public string Group { get; set; }
    public FeederComponentInfo FromComponents { get; set; }
    public string Phase { get; set; }
    public HarmonicsDto Harmonics { get; set; }
    [JsonProperty("base_resolution")]
    public string BaseResolution { get; set; }
    public string Quantity { get; set; }

}

public static class BaseDataExtensions
{
    public static BaseParameter ToBaseParameter(this BaseData trendBaseData)
    {
        if (trendBaseData == null)
        {
            throw new ArgumentNullException(nameof(BaseData));
        }

        return new BaseParameter
        {
            Type = trendBaseData.Type, // assuming Base_Type maps to Type
            Group = trendBaseData.Group,
            Phase = trendBaseData.Phase,
            Harmonics = trendBaseData.Harmonics != null
                ? new HarmonicsDto { Value = trendBaseData.Harmonics.Value }
                : null,
            BaseResolution = trendBaseData.BaseResolution,
            Quantity = trendBaseData.Quantity,
            //Id = trendBaseData.Id,
            //Name = trendBaseData.Name,

            AggregationFunction = string.Empty,
            Operator = string.Empty,
            Resolution = 1,
            FromComponents = null
        };
    }
}

public class TrendCustomWidgetData
{
    public int Id { get; set; }
    public bool IgnoreAlignment { get; set; }
    public string Quantity { get; set; }
}

public class Harmonics
{
    public int? Index { get; set; }
}

public class TrendCalcRequest : WidgetValidationBase, ICustomValidate
{
    public bool IsAutoResolution { get; set; }
    public int? ResolutionInSeconds { get; set; }
    //public string Resolution { get; set; }
    public string WidgetName { get; set; }
    public List<TrendParameter> Parameters { get; set; } = new List<TrendParameter>();
    public IntervalSynchronized SelectedResolution { get; set; }

    public void AddValidationErrors(CustomValidationContext context)
    {
        if (ValidationErrors(context) == false)
        {
            return;
        }

        //if (string.IsNullOrEmpty(Resolution))
        //{
        //    context.Results.Add(new ValidationResult($"{nameof(Resolution)} - Cannot be empty"));
        //    return;
        //}

        if (Parameters is null || Parameters.Count == 0)
        {
            context.Results.Add(new ValidationResult($"{nameof(Parameters)} - Cannot be empty"));
            return;
        }


        foreach (var param in Parameters)
        {
            if (Enum.TryParse(param.Type, true, out TrendWidgetParameterType columnParameterType))
            {
                switch (columnParameterType)
                {
                    case TrendWidgetParameterType.CustomParameter:

                        if (string.IsNullOrEmpty(param.CustomData.Quantity))
                        {
                            context.Results.Add(new ValidationResult($"{nameof(TrendCustomWidgetData.Quantity)} - Cannot be empty"));
                            return;
                        }

                        if (param.Feeders.IsCollectionEmpty())
                        {
                            context.Results.Add(new ValidationResult($"Both feeders and Channels - Cannot be empty."));
                            return;
                        }

                        break;

                    case TrendWidgetParameterType.BaseParameter:
                        if (param.Feeders.IsCollectionEmpty())
                        //if ((param.ApplyToDos is null || param.ApplyToDos.Count == 0) && (param.Feeders is null || param.Feeders.Count == 0))
                        {
                            context.Results.Add(new ValidationResult($"Both feeders and Channels - Cannot be empty."));
                            return;
                        }

                        try
                        {

                            var baseParameter = param.BaseData.ToBaseParameter();
                            //var baseParameter = param.BaseData.ToBaseParameter();
                            //var baseParameter = JsonConvert.DeserializeObject<BaseParameter>(param.Data) ?? throw new Exception();
                            //if (string.IsNullOrEmpty(baseParameter.Name))
                            //{
                            //    context.Results.Add(new ValidationResult($"BaseParameter name - Cannot be empty."));
                            //    return;
                            //}

                            if (Enum.TryParse(baseParameter.Type, true, out ParameterListItemType businessType))
                            {
                                switch (businessType)
                                {
                                    case ParameterListItemType.Logical:
                                        if (param.Feeders.Count <= 0)
                                        {
                                            context.Results.Add(new ValidationResult($"In BaseParameter (Logical) mode feeders must be."));
                                            return;
                                        }
                                        break;

                                    case ParameterListItemType.Channel:

                                        //if (param.Feeders.FirstOrDefault(x => x.Id is null) != null)
                                        ////if (param.ApplyToDos.Count <= 0)
                                        //{
                                        //    context.Results.Add(new ValidationResult($"In BaseParameter (Channel) mode channels must be."));
                                        //    return;
                                        //}
                                        break;

                                    case ParameterListItemType.Exception:
                                        break;
                                    default:
                                        break;
                                }

                            }
                        }
                        catch
                        {
                            context.Results.Add(new ValidationResult($"{nameof(ParameterListDto.Data)} should be base parameter."));
                            return;
                        }
                        break;

                    case TrendWidgetParameterType.Exception:
                        if (param.Feeders.IsCollectionExists())
                        //if ((param.Feeders is not null && param.Feeders.Count > 0) || (param.ApplyToDos is not null && param.ApplyToDos.Count > 0))
                        {
                            context.Results.Add(new ValidationResult($"Both feeders and Channels in exception mode should be empty."));
                            return;
                        }
                        break;
                    default:
                        break;
                }

            }
            else
            {
                context.Results.Add(new ValidationResult($"Type cann be only part of {nameof(ParameterListDto.Data)}"));
                return;
            }
        }
    }
}