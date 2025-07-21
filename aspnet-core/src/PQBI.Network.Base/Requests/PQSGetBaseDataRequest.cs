using PQS.Data.Configurations.Enums;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;
using PQS.Data.Measurements;
using PQS.Data.Common.Values;
using PQBI.Tenants.Dashboard.Dto;
using PQBI.Infrastructure.Extensions;
using PQS.Data.Common;
using PQS.Data.Measurements.Utils;
using PQS.Data.Measurements.StandardParameter;
using PQBI.CalculationEngine.Matrix;

namespace PQBI.Requests;

public record GetBaseDataInfoInput(Guid ComponentId, long StartTime, long EndTime, IEnumerable<MeasurementParameterBase> Parameters, CalculationTypeEnum CalculationType = CalculationTypeEnum.FORCE_DB_DATA, int whishedPoints = 0);

public class PQSGetBaseDataRequest : PQSCommonRequest
{
    public PQSGetBaseDataRequest(string session, params GetBaseDataInfoInput[] inputs) : base(session)
    {
        Inputs = inputs;
        AddConfigurations();
    }

    public GetBaseDataInfoInput[] Inputs { get; protected set; }

    protected override void AddConfigurations()
    {
        foreach (var input in Inputs.SafeList())
        {
            var opRec = new GetBaseDataRecord(input.ComponentId, input.StartTime, input.EndTime, input.Parameters.ToList(), input.CalculationType, input.whishedPoints);
            AddRecord(opRec);
        }
    }
}

public class PQSGetBaseDataResponse : PQSOperationResponseBase<PQSGetBaseDataRequest>
{
    public PQSGetBaseDataResponse(PQSGetBaseDataRequest request, PQSResponse response) : base(request, response)
    {

    }
    public virtual PQZStatus ExtractGetParametersOrError(out IEnumerable<PQBIAxisData> parameters)
    {
        parameters = null;

        ExtractBaseDataAllRecords(out BaseDataRecord[] records, out var error);
        if (error != null)
        {
            return error.Status;
        }

        var result = PQZStatus.OK;
        var paramList = new List<PQBIAxisData>();
        var recordIndex = 0;
        foreach (var record in records)
        {
            var getBaseDataInfoInput = Request.Inputs[recordIndex++];
            var compId = getBaseDataInfoInput.ComponentId;
            var timeStamps = record.DataTimeStamps;
            var paramName = string.Empty;
            var allMeasurementsParameter = record.MeasurementContainer.GetAllMeasurementsParameter();

            int? feederId = null;
            DataUnitType dataUnitType = new EmptyDataUnitType();

            foreach (MeasurementParameterBase paramAndVal in allMeasurementsParameter)
            {
                if (paramAndVal is NetworkFeederMeasurementParameter networkFeederParam)
                {
                    feederId = (int)networkFeederParam.FeederNumber;
                    var unitState  = UnitsUtility.GetUnitsFromGroupAndPhase(networkFeederParam.Group, networkFeederParam.Phase);
                    var token = UnitsEnumHelper.GetLocalizedDescriptionKey(unitState);
                    dataUnitType = new DataUnitType((int)unitState, token);

                    //string str =  unit me the description how?????
                }

                var dataTimeStemps = new List<PQBIDataTimeStampDto>();
                paramName = paramAndVal.ToString();
                var nominal = paramAndVal.Nominal;
                var container = record.MeasurementContainer[paramAndVal];

                if (container.Status == PQZStatus.OK)
                {
                    List<BaseDataValue<float>> points = container.GetBaseDataValue<float>();
                    var index = 0;
                    foreach (var point in points)
                    {
                        var dateTime = timeStamps[index++];
                        double? val = null;

                        if (float.IsNaN(point.Value) == false)
                        {
                            val = (double)point.Value;
                        }

                        dataTimeStemps.Add(new PQBIDataTimeStampDto(dateTime.DateTimeUTC, val, point.Status));
                    }

                    paramList.Add(new PQBIAxisData(compId, feederId, paramName, nominal, dataTimeStemps.ToArray(), container.Status,dataUnitType));
                }
                else
                {
                    paramList.Add(new PQBIAxisDataEmpty(compId, feederId, paramName, container.Status,dataUnitType));
                }
            }
        }

        parameters = paramList;
        return result;
    }
}

public class EmptyPQSGetBaseDataResponse : PQSGetBaseDataResponse
{
    public EmptyPQSGetBaseDataResponse(PQSGetBaseDataRequest request, PQSResponse response) : base(request, response)
    {
    }

    public override PQZStatus ExtractGetParametersOrError(out IEnumerable<PQBIAxisData> parameters)
    {
        parameters = new List<PQBIAxisData>();
        return PQZStatus.OK;
    }
}