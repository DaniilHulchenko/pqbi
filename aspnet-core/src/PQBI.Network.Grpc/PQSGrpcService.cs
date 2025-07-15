//using GrpcService1;
//using Microsoft.Extensions.Options;
//using PQBI.Configuration;
//using PQBI.Network.Base;
//using PQBI.Requests;
//using PQS.Data.Configurations.Enums;
//using PQS.Data.Configurations;
//using PQS.Data.RecordsContainer.Records;
//using PQS.Data.RecordsContainer;

//namespace PQBI.Network.Grpc
//{

//    public interface IPQSGrpcService : IPQSRestApiServiceBase
//    {

//    }

//    public class PQSGrpcService : PQSGrpcServiceBase, IPQSGrpcService 
//    {
//        public PQSGrpcService(IOptions<PQSComunication> pQsCommunication, PQSCommunication.PQSCommunicationClient grpClient) : base(pQsCommunication, grpClient)
//        {
//        }

//        public async Task<PQSGetSessionResponse> OpenSessionForUserAsync(string url, string userName, string password)
//        {

//            var message = string.Format(OPEN_SESSION_REQUEST, Repeat(), userName, password);
//            var response = await RequestXmlAsync(url, message);
//            return null;
//        }

//        public Task<string> GetUserRole(string session, string url, string userName, string password)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<bool> SendNOPForUserAsync(string url, string session)
//        {
//            throw new NotImplementedException();
//            //var request = string.Format(NOP_SESSION_REQUEST, Repeat(), session);
//            //var response = await RequestXmlAsync(url, request);

//            //return response;
//        }

//        public async Task<bool> CloseSessionForUserAsync(string url, string session)
//        {
//            throw new NotImplementedException();
//            //var message = string.Format(Close_Session_Request, Repeat(), session, session);
//            //var response = await RequestXmlAsync(url, message);

//            //return response;
//        }

//        public async Task<string> IndentifyAsync(string url)
//        {
//            var request = new RequestIdentify();
//            var response = await IndentifyAsync(request, url);


//            return response.ToString();
//        }       
//    }
//}