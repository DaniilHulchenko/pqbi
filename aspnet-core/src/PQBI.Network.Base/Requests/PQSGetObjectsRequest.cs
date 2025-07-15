using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;
using PQS.Data.Configurations;
using PQS.Data.Configurations.Enums;
using PQZTimeFormat;
using PQS.Data.Common.Values;
using PQS.Data.Common;

namespace PQBI.Requests
{

    public class PQSGetObjectsRequest : PQSCommonRequest
    {
        public PQSGetObjectsRequest(string targetComponentGui, string session)
            : base(session)
        {
            ID = Guid.Parse(targetComponentGui);
            AddConfigurations();
        }


        public PQSGetObjectsRequest(string session)
           : base(session)
        {           
        }

        protected override void AddConfigurations()
        {
            var presetConfigurations = new ConfigurationParameterAndValueContainer();
            presetConfigurations.AddParamWithValue<PQZDateTime>(StandardConfigurationEnum.STD_OPERATION_START_TIME, PQZDateTime.MinValue);
            presetConfigurations.AddParamWithValue<PQZDateTime>(StandardConfigurationEnum.STD_OPERATION_END_TIME, PQZDateTime.MaxValue);

            var opRec = new OperationRequestRecord(ID, OperationType.GET_ALL_SUPPORTED_PARAMETERS, presetConfigurations);
            var error = new ErrorRecord(null, PQZStatus.Acknowledge);
            AddRecord(opRec);
        }
    }


 
    public class PQSGetObjectsResponse : PQSOperationResponseBase<PQSGetObjectsRequest>
    {
        public PQSGetObjectsResponse(PQSGetObjectsRequest request, PQSResponse response) : base(request, response)
        {

        }

        public string []  ParameterNames
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

}