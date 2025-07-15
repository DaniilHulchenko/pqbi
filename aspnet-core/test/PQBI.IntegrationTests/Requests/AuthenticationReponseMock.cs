using PQBI.Web.Controllers;
using PQBI.Web.Models.TokenAuth;

namespace PQBI.IntegrationTests.Requests;


//public class ResponseMockWrapper<TResult> where TResult : class
//{
//    public TResult result { get; set; }
//    public object targetUrl { get; set; }
//    public bool success { get; set; }
//    public object error { get; set; }
//    public bool unAuthorizedRequest { get; set; }
//    public bool __abp { get; set; }
//}


public class ResponseTestWrapper<TResponse>
{
    public TResponse Result { get; set; }
    public string TargetUrl { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
    public bool UnauthorizedRequest { get; set; }
    public bool Abp { get; set; }
}


public class GetSessionMockResponse : ResponseTestWrapper<PQSGetSessionResponseTest>
{

}


public class AuthenticationMockResponse : ResponseTestWrapper<AuthenticateResultModel>
{

}
