using PQBI.CalculationEngine.Functions;
using PQS.Data.Measurements.Enums;

namespace PQBI.PQS.CalcEngine
{
    //public enum QuantityOp
    //{
    //    Min = 1,
    //    Max = 2,      
    //    Avg = 3
    //}

    public enum MarkerKind
    {
        Quantity = 1,
        Nominal = 2
    }

    public sealed class GaugeMarkerDto
    {
        /// <summary>Client-supplied unique key; echoed back in results.</summary>
        public string Key { get; set; } = default!;

        public MarkerKind Kind { get; set; }
       
        /// <summary>Required when Kind == Quantity.</summary>
        public PQBIQuantityType? Operation { get; set; }

        /// <summary>Required when Kind == Nominal. e.g. 80 means 80% of nominal.</summary>
        public double? PercentOfNominal { get; set; }
     
    }

    public sealed class GaugeMarkerResultDto
    {
        public string Key { get; set; } = default!;
        public double? Value { get; set; }
        public PqbiDataValueStatus DataValueStatus { get; set; }
    }
}
