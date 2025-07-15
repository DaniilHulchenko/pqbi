using FluentAssertions;
using PQBI.IntegrationTests.Requests;
using PQBI.Web.Models.TokenAuth;

namespace PQBI.IntegrationTests.Scenarios;

public abstract class ScenarioUrlsBase : ScenarioBase
{
    protected ScenarioUrlsBase(string baseAuthenticationUrl)
       : base()
    {
        BaseUrl = baseAuthenticationUrl; //https://localhost:44301/api
        TokenAuthControllerUrl = $"{BaseUrl}api/TokenAuth";
        AuthenticateUrl = $"{TokenAuthControllerUrl}/Authenticate";
        LogoutUrl = $"{TokenAuthControllerUrl}/Logout";
    }

    protected string AuthenticateUrl { get; }
    protected string LogoutUrl { get; }
    protected string TokenAuthControllerUrl { get; }
    public string BaseUrl { get; }

    //--------------------------------------------------------------------------
    //--------------------------------------------------------------------------
    //--------------------------------------------------------------------------

    protected virtual string UserName => "admin";
    protected virtual string UserPassword => "PQSpqs12345";

    public string Token { get; private set; }

    protected async Task UserAuthenticationOperation()
    {
        var payload = new AuthenticateModelRequest
        {
            UserNameOrEmailAddress = UserName,
            Password = UserPassword,
            RememberClient = false,
            SingleSignIn = false,
            ReturnUrl = (string)null,
            CaptchaResponse = (string)null
        };


        await Task.Delay(1000 * 10);
        AuthenticationMockResponse res = null;
        try
        {
            res = await RunPostCommand<AuthenticateModelRequest, AuthenticationMockResponse>(AuthenticateUrl, payload);
        }
        catch (Exception ex)
        {
            res = await RunPostCommand<AuthenticateModelRequest, AuthenticationMockResponse>(AuthenticateUrl, payload);
        }

        res.Result.Should().NotBeNull();

        Token = res.Result.AccessToken;
    }

    protected async Task UserLogoutOperation(bool isRethrow = true)
    {
        try
        {
            var res = await Get<AuthenticationMockResponse>(LogoutUrl, false, Token);
        }
        catch (Exception ex)
        {
            if (isRethrow)
            {
                throw;
            }
        }
    }
}



//public class Rootobject
//{
//    public Result result { get; set; }
//    public object targetUrl { get; set; }
//    public bool success { get; set; }
//    public object error { get; set; }
//    public bool unAuthorizedRequest { get; set; }
//    public bool __abp { get; set; }

//}

//public class Result
//{
//    public Component[] components { get; set; }
//}

//public class Component
//{
//    public Network[] networks { get; set; }
//    public string name { get; set; }
//    public string guid { get; set; }
//    public string[] parameterNames { get; set; }

//public string RootTagName { get; set; }
//public string[] Labels { get; set; }

//}

//public class Network
//{
//    public int id { get; set; }
//    public string name { get; set; }
//    public object channels { get; set; }
//    public Feeder[] feeders { get; set; }
//}

//public class Feeder
//{
//    public int id { get; set; }
//    public string name { get; set; }
//    public Channel[] channels { get; set; }
//}

//public class Channel
//{
//    public int id { get; set; }
//    public string value { get; set; }
//}