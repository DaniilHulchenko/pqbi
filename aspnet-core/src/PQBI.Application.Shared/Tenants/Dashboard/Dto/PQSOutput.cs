using System.Linq;
using System;
using PQS.Data.Common.Values;
using PQS.Data.Common;
using PQBI.CalculationEngine.Matrix;

namespace PQBI.Tenants.Dashboard.Dto
{
    public class PQBIDataTimeStampDto
    {
        public DateTime DateTime { get; }
        public double? Point { get; set; }
        public DataValueStatus DataValueStatus { get; }

        public PQBIDataTimeStampDto(DateTime dateTime, double? point, DataValueStatus dataValueStatus)
        {
            DateTime = dateTime;
            Point = point;
            DataValueStatus = dataValueStatus;
        }

        public override string ToString()
        {
            return $"[{DateTime:yyyy-MM-dd HH:mm:ss}, Point={Point?.ToString() ?? "null"}, Status={DataValueStatus}]";
        }
    }


    public class PQBIAxisData
    {
        public PQBIAxisData(Guid componentId, int? feederID, string parameter, double? nominal, PQBIDataTimeStampDto[] dataTimeStamps, PQZStatus pQZStatus, DataUnitType dataUnitType)
        //public PQBIAxisData(Guid componentId, string? feederID, string parameter, double? nominal, PQBIDataTimeStampDto[] dataTimeStamps, PQZStatus pQZStatus, int dataUnitType)
        {
            ComponentId = componentId;
            FeederID = feederID;
            ParameterName = parameter; //STD_PST_ISN_B10MIN_QMAX_UV1N_FEEDER_1
            Nominal = nominal;
            PQZStatus = pQZStatus;
            DataUnitType = dataUnitType;
            //IsFakeData = isFakeData;
            DataTimeStamps = dataTimeStamps.ToArray();
        }

        public Guid ComponentId { get; }
        public int? FeederID { get; }
        public string ParameterName { get; }
        public double? Nominal { get; }
        public PQZStatus PQZStatus { get; }
        public DataUnitType DataUnitType { get; set; }
        public PQBIDataTimeStampDto[] DataTimeStamps { get; }

        public override string ToString()
        {
            // join each timestamp’s own ToString()
            var timestamps = string.Join(", ",
                DataTimeStamps.Select(d => d.ToString()));

            return $"Axis: ComponentId={ComponentId}, " +
                   $"FeederID={(FeederID.HasValue ? FeederID.Value.ToString() : "null")}, " +
                   $"ParameterName={ParameterName}, " +
                   $"Nominal={(Nominal.HasValue ? Nominal.Value.ToString() : "null")}, " +
                   $"PQZStatus={PQZStatus}, " +
                   $"DataUnitType={DataUnitType}, " +
                   $"DataTimeStamps=[{timestamps}]";
        }


    }

    public class PQBIAxisDataEmpty : PQBIAxisData
    {
        public PQBIAxisDataEmpty(Guid compId, int? feederID, string parameterName, PQZStatus status, DataUnitType dataUnitType)
            : base(compId, feederID, parameterName, null, [], status, dataUnitType)
        {

        }
    }


    public class PQSOutput
    {

        public PQBIAxisData[] Data { get; init; }
    }
}
