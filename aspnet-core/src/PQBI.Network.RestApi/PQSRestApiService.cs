using Abp.Authorization;
using Microsoft.Extensions.Logging;
using PQBI.Configuration;
using PQBI.Infrastructure.Extensions;
using PQBI.Network.Base;
using PQBI.PQS;
using PQBI.Requests;
using PQS.Data.Configurations;
using PQS.Data.Configurations.Enums;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.Permissions;
using System;
using Permission = PQS.Data.Permissions.Permission;
using PQS.Data.Permissions.Enums;
using Abp.Localization;
using PQBI.Authorization;

namespace PQBI.Network.RestApi
{
    public interface IPQSRestApiService : IPQSRestApiServiceBase
    {
    }

    public class PQSRestApiBinaryService : PQSRestApiServiceBase, IPQSRestApiService
    {
        private readonly ILocalizationManager _localizationManager;

        public PQSRestApiBinaryService(ILogger<PQSRestApiBinaryService> logger, IHttpClientFactory httpClientFactory, IPQZBinaryWriterWrapper pQZBinaryWriterCore, IPQSenderHelper pQSenderHelper, ILocalizationManager localizationManager) : base(httpClientFactory, pQZBinaryWriterCore, pQSenderHelper, logger)
        {
            _localizationManager = localizationManager;
        }

        protected override string ClientAlias => IPQSRestApiService.Alias;

        public async Task<bool> CloseSessionForUserAsync(string url, string session)
        {
            var result = false;
            var request = new PQSCloseSessionRequest(session);

            var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);
            if (pqsResponse != null)
            {
                var response = new PQSCloseSessionResponse(request, pqsResponse);
                result = response.IsValid;
            }

            return result;
        }

        public async Task<string> IndentifyAsync(string url)
        {
            var response = await PQSIndentifyAsync(url);
            return response.ToString();
        }

        public async Task<PQSGetSessionResponse> OpenSessionForUserAsync(string url, string userName, string password)
        {
            var request = new PQSGetSessionRequest(userName, password);
            PQSGetSessionResponse response = null;
            var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);

            if (pqsResponse != null)
            {
                response = new PQSGetSessionResponse(request, pqsResponse);

            }

            return response;
        }
  
        public async Task<string> GetUserRole(string session, string url, string userName)
        {
            ConfigurationParameterBase permissionUserNameConf = StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_USER_NAME);
            ConfigurationParameterBase permissionPerfConf = StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_PERMISSIONS);
            //List<ConfigurationParameterBase> confList = new List<ConfigurationParameterBase>();
            var confList = new List<ConfigurationParameterBase>();
            confList.Add(permissionUserNameConf);
            confList.Add(permissionPerfConf);

            ObjectsRequestRecord objectsRequestRecord = new ObjectsRequestRecord(null, ObjectType.Users, ObjectFilterType.NoFilter, objectsAditionalConfiguration: confList);

            //Guid.TryParse(session, out Guid sessionGuid);
            PQSGetObjectsRequest req = new PQSGetObjectsRequest(session);
            req.AddRecord(objectsRequestRecord);
            PQSResponse pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, req);

            //try
            //{
            //    pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, req);
            //}
            //catch (Exception ex)
            //{
            //    if (ex is SessionExpiredException)
            //        throw ex;                
            //}

            if (pqsResponse != null)
            {
                PQSRecordBase recBase = pqsResponse.GetRecords().First();

                if (recBase != null)
                {
                    if (recBase is ObjectsResponseRecord objectRec)
                    {
                        foreach (ConfigurationParameterAndValueContainer item in objectRec.ObjectsAndConfigurations.Values)
                        {
                            item.TryGetConfigurationValue<string>(permissionUserNameConf, out string userNameVal);

                            if (userNameVal.Equals(userName))
                            {
                                item.TryGetConfigurationValue<string>(permissionPerfConf, out string permissionConfXml);

                                Permission permissions = new Permission(permissionConfXml);


                                switch (permissions.PQBIRole)
                                {
                                    case PQBIRoleEnum.Admin:
                                        return PQBIRoleEnum.Admin.ToString();
                                    case PQBIRoleEnum.Editor:
                                        return "DashboardBuilder";
                                    case PQBIRoleEnum.Viewer:
                                        return PQBIRoleEnum.Viewer.ToString();
                                    case PQBIRoleEnum.None:
                                        //throw new AbpAuthorizationException(_localizationManager.GetString(PQBIConsts.LocalizationSourceName, LoginStatusEnum.UserNotAuthorized.ToString()));
                                        return PQBIRoleEnum.Admin.ToString();
                                    default:
                                        return PQBIRoleEnum.Admin.ToString();
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public async Task<string> RequestXmlAsync(string url, string request)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SendNOPForUserAsync(string url, string session)
        {
            var request = new PQSNopRequest(session);
            var status = false;

            var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);
            if (pqsResponse != null)
            {
                var response = new PQSNopResponse(request, pqsResponse);
                status = response.IsValid;
            }

            return status;
        }


    }
}