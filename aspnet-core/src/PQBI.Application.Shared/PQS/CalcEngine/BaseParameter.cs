using NUglify.JavaScript.Syntax;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Functions.CalcSingleAxis;
using PQS.Data.Measurements;
using System;

namespace PQBI.PQS.CalcEngine;

public class BaseParameter
{
    public const string ISX = "ISX";

    public string Type { get; set; } // Logical/Channel/Exception
    public string AggregationFunction { get; set; }
    public string Operator { get; set; }

    public string Quantity { get; set; }
    public int? Resolution { get; set; }
    public string Group { get; set; }
    public HarmonicsDto Harmonics { get; set; }
    public FeederComponentInfo FromComponents { get; set; }
    public string Phase { get; set; }
    public string BaseResolution { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }

    //public int ResolutionInSeconds
    //{
    //    get
    //    {
    //        var seconds = GroupByCalcFunction.ParseAndConvertToSecond(Resolution);
    //        return seconds;
    //    }
    //}

    public SyncInterval SyncInterval
    {
        get
        {
            //if (Resolution.StartsWith(ISX))
            //{
            //    var syncInterval  = new SyncInterval(Resolution);
            //    return syncInterval;
            //}

            if(Resolution == null)
            {
                return SyncInterval.GetSyncEnum(0);
            }

            return SyncInterval.GetSyncEnum(Resolution.Value );
        }
    }

    public string ScadaQuantityName
    {
        get
        {
            if (string.IsNullOrEmpty(Quantity))
            {
                return string.Empty;
            }

            if (Quantity[0] != 'Q')
            {
                return $"Q{Quantity}";
            }

            return Quantity;
        }
    }

    public bool IsExceptionParameter => FromComponents is not null;
}

public static class BaseParameterExtensions
{
    //For Trend
    //public static void SetAutoResolution(this BaseParameter baseParameter, DateTime startDate , DateTime endDate, string resolution)
    //{

    //    var syncStr = string.Empty;
    //    if (AutoCalcFunction.TryExtracMaxPoints(resolution, out var maxPoints))
    //    {

    //        var period = endDate - startDate;
    //        var periodInSeconds = (double)period.TotalSeconds / maxPoints;
    //        var paramSync = SyncInterval.GetSyncEnum(periodInSeconds);
    //        syncStr = paramSync.ToString();

    //        baseParameter.Resolution = syncStr;
    //    }
    //    else
    //    {
    //        baseParameter.Resolution = resolution;
    //    }
    //}


    //For Table
    //public static void SetISXResolution(this BaseParameter baseParameter, DateTime startDate, DateTime endDate)
    //{
    //    int totalSeconds = (int)(endDate - startDate).TotalSeconds;
    //    baseParameter.Resolution = $"{BaseParameter.ISX}{totalSeconds}";
    //}
}