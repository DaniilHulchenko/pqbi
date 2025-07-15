using PQS.Data.Configurations.Enums;
using PQS.Data.Configurations;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;

namespace PQBI.Requests
{
    public class PQSCloseSessionRequest : PQSCommonRequest
    {
        public PQSCloseSessionRequest(string session):
            base(session)
        {
            AddConfigurations();
        }

        protected override void AddConfigurations()
        {
            var sessionConfigurations = new ConfigurationParameterAndValueContainer();
            sessionConfigurations.AddParamWithValue(StandardConfigurationEnum.STD_SESSION_ID, Session);


            var sessionRecord = new OperationRequestRecord(null, OperationType.CLOSE_SESSION, sessionConfigurations);
            AddRecord(sessionRecord);
        }
    }

    public class PQSCloseSessionResponse : PQSOperationResponseBase<PQSCloseSessionRequest>
    {
        public PQSCloseSessionResponse(PQSCloseSessionRequest request, PQSResponse response) : base(request, response)
        {

        }
    }
}
