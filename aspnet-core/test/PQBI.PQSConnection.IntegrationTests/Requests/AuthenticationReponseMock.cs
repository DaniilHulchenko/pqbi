using PQBI.Requests;
using PQBI.Web.Controllers;
using PQBI.Web.Models.TokenAuth;

namespace PQBI.PQSConnection.IntegrationTests.Requests;


    public class ResponseMockWrapper<TResult> where TResult : class
    {
        public TResult result { get; set; }
        public object targetUrl { get; set; }
        public bool success { get; set; }
        public object error { get; set; }
        public bool unAuthorizedRequest { get; set; }
        public bool __abp { get; set; }
    }



    public class GetSessionMockResponse : ResponseMockWrapper<PQSGetSessionResponse>
    {

    }


    public class AuthenticationMockResponse : ResponseMockWrapper<AuthenticateResultModel>
    {

    }

