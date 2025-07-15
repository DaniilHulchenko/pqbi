using PQS.Data.Configurations.Enums;
using PQS.Data.Configurations;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;

namespace PQBI.Requests
{
    public class PQSNopRequest : PQSCommonRequest
    {
        public PQSNopRequest(string session)
            :base(session)
        {

            AddConfigurations();

        }

        protected override void AddConfigurations()
        {
            var sessionConfigurations = new ConfigurationParameterAndValueContainer();
            sessionConfigurations.AddParamWithValue(StandardConfigurationEnum.STD_SESSION_ID, Session);


            var sessionRecord = new OperationRequestRecord(null, OperationType.NOP, sessionConfigurations);
            AddRecord(sessionRecord);
        }

    }


    public class PQSNopResponse : PQSOperationResponseBase<PQSNopRequest>
    {   
        public PQSNopResponse(PQSNopRequest request, PQSResponse response) : base(request, response)
        {

        }
    }
}
