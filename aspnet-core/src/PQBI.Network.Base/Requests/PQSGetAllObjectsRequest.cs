using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;
using PQS.Data.Configurations;
using PQS.Data.Configurations.Enums;
using PQZTimeFormat;
using PQS.Data.Common.Values;
using PQS.PQZxml;
using PQS.Data.Measurements.CustomParameter;
using PQBI.PQS;

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

    public void ExtractGetParametersOrError(out IReadOnlyDictionary<string, string[]> parameters, out IReadOnlyDictionary<string, IEnumerable<CustomMeasurementParameter>> customParameters, out IReadOnlyDictionary<string, IEnumerable<CustomBaseInfo>> customBaseMap, out ErrorRecord errorRecord)
    {
        errorRecord = null;
        parameters = null;
        customParameters = null;
        customBaseMap = null;

        var list = new Dictionary<string, string[]>();
        var customPrmList = new Dictionary<string, IEnumerable<CustomMeasurementParameter>>();
        var customBaseList = new Dictionary<string, IEnumerable<CustomBaseInfo>>();      
      
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

                if (record.OperationConfigurationResult.TryGetConfigurationValue<string>(StandardConfigurationEnum.STD_SUPPORTED_PARAMETERS_CUSTOM, out var customParametersStrList))
                {
                    var customPrms = PQZxmlReader.ReadCustomMeasurmentsParameters(customParametersStrList);
                    if (customPrms.Count() > 0)
                        customPrmList.Add(record.ObjectID!.ToString(), customPrms);
                }

                if (record.OperationConfigurationResult.TryGetConfigurationValue<string>(StandardConfigurationEnum.STD_CUSTOM_RESOLUTION_ARRAY, out var customBaseParametersStrList))
                {
                    List<CustomCalculationBaseInfo> customBase = CustomPrmSerializationUtill.DeserializeXmlToObject<List<CustomCalculationBaseInfo>>(customBaseParametersStrList);

                    List<CustomBaseInfo> customBaseInfoList = new List<CustomBaseInfo>();

                    foreach (var baseInfo in customBase)
                    {
                        string basexVal = baseInfo.WindowInterval == null ? baseInfo.CalcBase.ToString() : $"{baseInfo.CalcBase}_{baseInfo.WindowInterval}";
                        customBaseInfoList.Add(new CustomBaseInfo
                        {
                            BaseXString = basexVal,
                            PresentedName = baseInfo.PresentedName,
                        });
                    }                   

                    if (customBase.Count() > 0)
                        customBaseList.Add(record.ObjectID!.ToString(), customBaseInfoList);
                }
            }

            parameters = list;

            customParameters = customPrmList;
            customBaseMap = customBaseList;
        }
    }
}