using PQS.Data.Configurations.Enums;
using PQS.Data.Configurations;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.Common.Values;
using PQS.Data.Common;
using PQBI.Infrastructure.Extensions;

namespace PQBI.Requests
{
    public class PQSGetSessionRequest : PQSRequestBase
    {
        private readonly string _password;

        public PQSGetSessionRequest(string userName, string password)
        {
            UserName = userName;
            _password = password;

            AddConfigurations();

        }

        public string UserName { get; }

        protected override void AddConfigurations()
        {
            var sessionConfigurations = new ConfigurationParameterAndValueContainer();

            sessionConfigurations.AddParamWithValue<bool>(StandardConfigurationEnum.STD_IS_DATA_ENCRYPTED, false);
            sessionConfigurations.AddParamWithValue<string>(StandardConfigurationEnum.STD_USER_NAME, UserName);
            sessionConfigurations.AddParamWithValue<string>(StandardConfigurationEnum.STD_PASSWORD, _password);
            sessionConfigurations.AddParamWithValue<byte>(StandardConfigurationEnum.STD_LOGGED_USER_SOURCE_TYPE, 8);

            var sessionRecord = new OperationRequestRecord(null, OperationType.OPEN_SESSION, sessionConfigurations);
            AddRecord(sessionRecord);
        }
    }


    public class PQSGetSessionResponse : PQSOperationResponseBase<PQSGetSessionRequest>
    {
        public PQSGetSessionResponse(PQSGetSessionRequest request, PQSResponse response) : base(request, response)
        {
        }


        public bool TryGetSession(out string session)
        {
            session = string.Empty;
            var result = false;

            if (TryExtractOperationResponseRecord(out var operationResponseRecord))
            {
                if (operationResponseRecord.OperationConfigurationResult.TryGetConfigurationValue<string>(StandardConfigurationEnum.STD_SESSION_ID, out var sessionId))
                {
                    session = sessionId;
                    result = true;
                }
            }

            return result;
        }

        public string SessionId
        {
            get
            {
                var session = string.Empty;

                TryGetSession(out session);
                return session;
            }
        }
    }


    public static class PQSGetSessionResponseExtensions
    {
        public static bool IsEmpty(this PQSGetSessionResponse response)
        {
            var result = false;

            if(response is null || response.SessionId.IsGuidEmpty())
            {
                result = true;
            }

            return result;
        }

        public static bool IsNotEmpty(this PQSGetSessionResponse response)
        {
            return !IsEmpty(response);
        }

        public static bool IsOk(this PQSGetSessionResponse response)
        {
            return response.Status == PQZStatus.OK;
        }

    }
}
