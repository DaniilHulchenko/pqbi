using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.Common;
using PQS.Data.Configurations;

namespace PQBI.Requests;

public abstract class PQBIResponseBase
{
    protected void ExtractResponseRecord<TOperationType>(PQSRecordsContainer response, out TOperationType operationResponseRecord, out ErrorRecord errorRecord)
    {
        operationResponseRecord = default(TOperationType);
        errorRecord = null;

        var record = response.GetRecords().FirstOrDefault();
        ExtractSingleRecord(record, out operationResponseRecord, out errorRecord);
    }

    protected void ExtractSingleRecord<TOperationType>(PQSRecordBase record, out TOperationType operationResponseRecord, out ErrorRecord errorRecord)
    {
        operationResponseRecord = default(TOperationType);
        errorRecord = null;

        if (record != null)
        {
            if (record is TOperationType operation)
            {
                operationResponseRecord = operation;
            }
            else
            {
                if (record is ErrorRecord error)
                {
                    errorRecord = error;
                }
            }
        }
    }

    protected void ExtractEventOrErrorRecord(PQSRecordsContainer response, out EventsRecord operationRecord, out ErrorRecord errorRecord)
    {
        ExtractResponseRecord(response, out operationRecord, out errorRecord);
    }

    protected void ExtractOperationOrErrorRecord(PQSRecordsContainer response, out OperationResponseRecord operationRecord, out ErrorRecord errorRecord)
    {
        ExtractResponseRecord(response, out operationRecord, out errorRecord);
    }

    protected void ExtractObjectsResponseRecord(PQSRecordsContainer response, out ObjectsResponseRecord operationRecord, out ErrorRecord errorRecord)
    {
        ExtractResponseRecord(response, out operationRecord, out errorRecord);
    }
    protected void ExtractConfigurationResponseRecord(PQSRecordsContainer response, out InstantConfigurationRecord operationRecord, out ErrorRecord errorRecord)
    {
        ExtractResponseRecord(response, out operationRecord, out errorRecord);
    }

    protected void ExtractGetBaseConfigurationRecord(PQSRecordsContainer response, out BaseConfigurationRecord operationRecord, out ErrorRecord errorRecord)
    {
        ExtractResponseRecord(response, out operationRecord, out errorRecord);
    }
}

public abstract class PQSOperationResponseBase<TRequest> : PQBIResponseBase where TRequest : PQSRequestBase
{
    public PQSOperationResponseBase(TRequest request, PQSResponse response)
    {
        Request = request;
        Response = response;
    }

    public TRequest Request { get; set; }
    public PQSResponse Response { get; }


    public PQZStatus Status
    {
        get
        {
            var record = Response.GetRecord(0);

            if (record is PQSStatusRecordeBase statusRecordeBase)
            {
                return statusRecordeBase.Status;
            }

            if (record is ObjectsResponseRecord objectsResponseRecord)
            {
                return objectsResponseRecord.Status;
            }

            return PQZStatus.FAIL;
        }
    }

    public bool IsValid => Status == PQZStatus.OK;
    protected void ExtractBaseDataAllRecords(out BaseDataRecord[] configurationParameterAndValues, out ErrorRecord errorRecord)
    {
        errorRecord = null;
        configurationParameterAndValues = [];

        var operations = new List<BaseDataRecord>();
        foreach (var responseRecord in Response.GetRecords())
        {
            ExtractSingleRecord(responseRecord, out BaseDataRecord record, out var error);
            if (error != null)
            {
                errorRecord = error;
                return;
            }

            if (record != null)
            {
                operations.Add(record);
            }
        }

        configurationParameterAndValues = operations.ToArray();
    }

    protected void ExtractGetBaseConfigurationRecord(out BaseConfigurationRecord[] configurationParameterAndValues, out ErrorRecord errorRecord)
    {
        errorRecord = null;
        configurationParameterAndValues = [];

        var operations = new List<BaseConfigurationRecord>();
        foreach (var responseRecord in Response.GetRecords())
        {
            if (responseRecord is BaseConfigurationRecord getBaseConfigurationRecord)
            {
                ExtractSingleRecord(responseRecord, out BaseConfigurationRecord record, out var error);
                if (error != null)
                {
                    errorRecord = error;
                    return;
                }

                if (record != null)
                {
                    operations.Add(record);
                }
            }
        }

        configurationParameterAndValues = operations.ToArray();
    }

    protected void ExtractOperationAllRecords(out OperationResponseRecord[] configurationParameterAndValues, out ErrorRecord errorRecord)
    {
        errorRecord = null;
        configurationParameterAndValues = [];

        var operations = new List<OperationResponseRecord>();
        foreach (var responseRecord in Response.GetRecords())
        {
            ExtractSingleRecord(responseRecord, out OperationResponseRecord record, out var error);
            if (error != null)
            {
                errorRecord = error;
                return;
            }

            if (record != null)
            {
                operations.Add(record);
            }
        }

        configurationParameterAndValues = operations.ToArray();
    }

    protected void ExtractAllRecords<TRecord>(out TRecord[] configurationParameterAndValues, out ErrorRecord errorRecord)
    {
        errorRecord = null;
        configurationParameterAndValues = [];

        var operations = new List<TRecord>();
        foreach (var responseRecord in Response.GetRecords())
        {
            ExtractSingleRecord(responseRecord, out TRecord record, out var error);
            if (error != null)
            {
                errorRecord = error;
                return;
            }

            if (record != null)
            {
                operations.Add(record);
            }
        }

        configurationParameterAndValues = operations.ToArray();
    }



    protected bool TryExtractParameterAndValueContainerFirstRecord(out ConfigurationParameterAndValueContainer configurationParameterAndValue)
    {
        configurationParameterAndValue = null;

        var record = Response.GetRecord(0) as OperationResponseRecord;
        if (record != null && record.OperationConfigurationResult != null && record.OperationConfigurationResult.Count > 0)
        {
            configurationParameterAndValue = record.OperationConfigurationResult;
        }

        return configurationParameterAndValue != null;
    }

    protected bool TryExtractEventResponseRecord(out EventsRecord operationResponseRecord)
    {
        ExtractEventOrErrorRecord(Response, out operationResponseRecord, out var error);
        return error == null;

    }

    protected bool TryExtractOperationResponseRecord(out OperationResponseRecord operationResponseRecord)
    {
        ExtractOperationOrErrorRecord(Response, out operationResponseRecord, out var error);
        return error == null;

    }

    protected bool TryExtractObjectsResponseRecord(out ObjectsResponseRecord operationResponseRecord)
    {
        ExtractObjectsResponseRecord(Response, out operationResponseRecord, out var error);
        return error == null;
    }

    protected bool TryExtractConfigurationResponseRecord(out InstantConfigurationRecord operationResponseRecord)
    {
        ExtractConfigurationResponseRecord(Response, out operationResponseRecord, out var error);
        return error == null;
    }


}
