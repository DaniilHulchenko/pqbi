using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;
using PQS.Data.Configurations;
using PQS.Data.Configurations.Enums;
using PQZTimeFormat;
using PQS.Data.Common.Values;

namespace PQBI.Requests;

public class PQSGetAllObjectsRequest : PQSCommonRequest
{
    private IEnumerable<Guid> _ids;

    public PQSGetAllObjectsRequest(string session, params string[] targetComponentGui)
        : base(session)
    {
        _ids = targetComponentGui.Select(x => Guid.Parse(x));
        AddConfigurations();
    }

    protected override void AddConfigurations()
    {
        var presetConfigurations = new ConfigurationParameterAndValueContainer();
        presetConfigurations.AddParamWithValue<PQZDateTime>(StandardConfigurationEnum.STD_OPERATION_START_TIME, PQZDateTime.MinValue);
        presetConfigurations.AddParamWithValue<PQZDateTime>(StandardConfigurationEnum.STD_OPERATION_END_TIME, PQZDateTime.MaxValue);


        foreach (var id in _ids)
        {
            var opRec = new OperationRequestRecord(id, OperationType.GET_ALL_SUPPORTED_PARAMETERS, presetConfigurations);
            AddRecord(opRec);
        }
    }
}


public class PQSGetAllObjectsResponse : PQSOperationResponseBase<PQSGetAllObjectsRequest>
{
    public PQSGetAllObjectsResponse(PQSGetAllObjectsRequest request, PQSResponse response) : base(request, response)
    {

    }

    public void ExtractGetParametersOrError(out IReadOnlyDictionary<string, string[]> parameters, out ErrorRecord errorRecord)
    {
        errorRecord = null;
        parameters = null;

        var list = new Dictionary<string, string[]>();

        ExtractOperationAllRecords(out var records, out var error);
        if (error != null)
        {
            errorRecord = error;
        }
        else
        {
            foreach (var record in records)
            {
                if (record.OperationConfigurationResult.TryGetConfigurationValue<ListValuesContainer<string>>(StandardConfigurationEnum.STD_SUPPORTED_PARAMETERS_STANDARD, out var supportedParametersStrList))
                {
                    list.Add(record.ObjectID!.ToString(), supportedParametersStrList.ToArray());
                }
            }

            parameters = list;
        }
    }
}