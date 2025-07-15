using PQS.Data.Configurations.Enums;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;
using PQS.Data.Measurements;
using PQS.Data.Common.Values;
using PQBI.Tenants.Dashboard.Dto;
using PQS.Data.Measurements.StandardParameter;
using PQS.Data.Measurements.Enums;
using PQBI.Infrastructure.Extensions;

namespace PQBI.Requests;

public class FlickerPQSGetBaseDataRequest : PQSGetBaseDataRequest
{
    public FlickerPQSGetBaseDataRequest(string session, params GetBaseDataInfoInput[] inputs) : base(session, null)
    {
        var records = AddFlickers(inputs);
        Inputs = records;

        AddConfigurations();
    }

    private GetBaseDataInfoInput[] AddFlickers(GetBaseDataInfoInput[] inputs)
    {
        var parameters = new List<MeasurementParameterBase>();
        parameters.Add(new NetworkFeederMeasurementParameter(1, 1, 1, PhaseMeasurementEnum.UV12, Group.PST, new SyncInterval(IntervalSynchronized.ISN), new CalcBase(CalculationBase.B10MIN), QuantityEnum.QMAX, AvgCalculationType.Arithmetic));
        parameters.Add(new NetworkFeederMeasurementParameter(1, 1, 1, PhaseMeasurementEnum.UV23, Group.PST, new SyncInterval(IntervalSynchronized.ISN), new CalcBase(CalculationBase.B10MIN), QuantityEnum.QMAX, AvgCalculationType.Arithmetic));
        parameters.Add(new NetworkFeederMeasurementParameter(1, 1, 1, PhaseMeasurementEnum.UV31, Group.PST, new SyncInterval(IntervalSynchronized.ISN), new CalcBase(CalculationBase.B10MIN), QuantityEnum.QMAX, AvgCalculationType.Arithmetic));

        var list = new List<GetBaseDataInfoInput>();
        foreach (var input in inputs.SafeList())
        {
            var data = input with
            {
                Parameters = parameters,
                whishedPoints = 1
            };

            list.Add(data);
        }

        return list.ToArray();
    }
}

public record FlickerDto(string ComponentId, double Value);


public class FlickerPQSGetBaseDataResponse : PQSOperationResponseBase<FlickerPQSGetBaseDataRequest>
{
    public FlickerPQSGetBaseDataResponse(FlickerPQSGetBaseDataRequest request, PQSResponse response) : base(request, response)
    {

    }

    public void ExtractFlickersOrError(out IEnumerable<FlickerDto> parameters, out ErrorRecord errorRecord)
    {
        errorRecord = null;
        parameters = null;

        var dataTimeStemps = new List<FlickerDto>();


        ExtractBaseDataAllRecords(out var records, out var error);
        if (error != null)
        {
            errorRecord = error;
        }
        else
        {
            var recordIndex = 0;
            foreach (var record in records)
            {

                var timeStamps = record.DataTimeStamps;

                foreach (var paramAndVal in record.MeasurementContainer.GetAllMeasurementsParameter())
                {
                    var paramName = paramAndVal.ToString();

                    var index = 0;
                    List<BaseDataValue<float>> points = (record.MeasurementContainer[paramAndVal]).GetBaseDataValue<float>();
                    foreach (var point in points)
                    {
                        double val = point.Status != DataValueStatus.HOLE ? (double)point.Value : 0.0;

                        dataTimeStemps.Add(new FlickerDto(record.ObjectID?.ToString(), val));
                    }
                }
            }

            parameters = dataTimeStemps;
        }
    }

    public string[] ParameterNames
    {
        get
        {
            var list = new List<string>();
            if (TryExtractParameterAndValueContainerFirstRecord(out var container))
            {
                if (container.TryGetConfigurationValue<ListValuesContainer<string>>(StandardConfigurationEnum.STD_SUPPORTED_PARAMETERS_STANDARD, out var supportedParametersStrList))
                {
                    foreach (var item in supportedParametersStrList)
                    {
                        list.Add(item as string);
                    }
                }
            }

            return list.ToArray();
        }
    }

}
