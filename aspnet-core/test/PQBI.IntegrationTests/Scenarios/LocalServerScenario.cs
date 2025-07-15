using FluentAssertions;
using Newtonsoft.Json;
using PQBI.IntegrationTests.Requests;
using PQBI.PQS;
using PQBI.PQS.CalcEngine;
using PQBI.Sapphire.Options;
using PQBI.Tenants.Dashboard.Dto;
using PQBI.Web.Controllers;
using PQZTimeFormat;
using System.Text.Json;

namespace PQBI.IntegrationTests.Scenarios;
internal class LocalServerScenario : ScenarioUrlsBase
{
    public LocalServerScenario(string baseAuthenticationUrl) : base(baseAuthenticationUrl)
    {
        GetBaeDataScenarioTestUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.GetBaeDataScenarioTestUrl}";
        GetPqsTrendWidgeScenarioTestUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.GetBaeDataUrl}";
        GetTags = $"{BaseUrl}PQSRestApi/{PQSRestApiController.GetTags}";

        PostIntegrationTestsStaticDataUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.PostIntegrationTestsStaticDataUrl}";
        PostIntegrationTestsStaticDataUrl2 = $"{BaseUrl}PQSRestApi/{PQSRestApiController.PostIntegrationTestsStaticDataIntegrationTestUrl}";

        PostEventsIntegrationTestsUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.PostEventsIntegrationTestsUrl}";
        GetStaticDataUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.GetStaticDataUrl_Old}";
        BaseParameterNameIntegrationTestUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.BaseParameterNameIntegrationTestUrl}";

    }



    //public string GetTagsOmnibusTestUrl { get; }
    public string BaseParameterNameIntegrationTestUrl { get; }
    public string GetComponentScenarioTestUrl { get; }
    public string GetBaeDataScenarioTestUrl { get; }
    public string GetPqsTrendWidgeScenarioTestUrl { get; }
    public string GetTags { get; }
    public string PostIntegrationTestsStaticDataUrl { get; }
    public string PostIntegrationTestsStaticDataUrl2 { get; }
    public string PostEventsIntegrationTestsUrl { get; }
    public string GetStaticDataUrl { get; }



    public override string ScenarioName => "Test";

    public override string Description => "Ping the PQBI";

    //protected override string UserName => "admin";

    //protected override string UserPassword => "PQSpqs12345";

    protected async override Task RunScenario()
    {
        //await GetBaseParameterName();
        //await GetStaticData();
        //await GetStaticData2();
        await GetEvents();
        await GetALlTags();
        await GetAllComponentsScenarioSlimAsync();
        await GetBaseDataScenarioAsync();
        await GetPqsTrendWidgetScenarioAsync();
    }

    private async Task GetBaseParameterName()
    {
        await UserAuthenticationOperation();

        var json = @"{
  ""type"": ""LOGICAL"",
  ""name"": ""bp1"",
  ""aggregationFunction"": ""Avg"",
  ""Operator"": ""Mul(2)"",
  ""quantity"": ""QAVG"",
  ""group"": ""RMS"",
  ""resolution"": ""IS1MIN"",
  ""phase"": ""UV1N"",
  ""base"": ""BHCYC"",
  ""id"": ""6nCqj"",
  ""feederId"": ""1""
}";

        var request = JsonConvert.DeserializeObject<BaseParameterNameSlim>(json);
        var wrapperResponse = await RunPostCommand<GetBaseParameterNameIntegrationTest, ResponseTestWrapper<OptionTree>>(BaseParameterNameIntegrationTestUrl, new GetBaseParameterNameIntegrationTest(UserName, UserPassword, request));

    }

    private async Task GetEvents()
    {
        await UserAuthenticationOperation();

        var start = DateTime.UtcNow.AddDays(-120);
        var end = DateTime.UtcNow.AddDays(-1);

        var response = await RunPostCommand<PostEventstRequestTest, ResponseTestWrapper<GetAllEventsResponse>>(PostEventsIntegrationTestsUrl, new PostEventstRequestTest(UserName, UserPassword,
            new GetEventstRequest(start, end
            , "0e481fab-ad7f-45bb-9244-b0f2d0fc5dcc", "a059db13-2390-432a-a062-6ac41f213612", "08c3912f-0275-4278-bf86-917168d88eef"))); //,"0e481fab-ad7f-45bb-9244-b0f2d0fc5dcc", "a059db13-2390-432a-a062-6ac41f213612"

        //"e768875f-958e-4742-a3e6-5a85273c43b8"
        response.Result.Events.Should().HaveCountGreaterThan(1);

    }


    private async Task GetStaticData()
    {
        var wrapperResponse = await RunPostCommand<PQSGetSessionRequestTest, ResponseTestWrapper<OptionTree>>(PostIntegrationTestsStaticDataUrl, new PQSGetSessionRequestTest(UserName, UserPassword));
        var response = wrapperResponse.Result;


        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = System.Text.Json.JsonSerializer.Serialize(wrapperResponse.Result, options);

        response.LogicalParameters.Count.Should().Be(81);
        response.ChannelParameters.Count.Should().Be(51);
    }

    private async Task GetStaticData2()
    {
        var wrapperResponse = await RunPostCommand<PQSGetSessionRequestTest, ResponseTestWrapper<StaticTreeNode>>(PostIntegrationTestsStaticDataUrl2, new PQSGetSessionRequestTest(UserName, UserPassword));
        var response = wrapperResponse.Result;


        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = System.Text.Json.JsonSerializer.Serialize(wrapperResponse.Result, options);

        //response.LogicalParameters.Count.Should().Be(81);
        //response.ChannelParameters.Count.Should().Be(51);
    }


    private async Task GetALlTags()
    {
        await UserAuthenticationOperation();

        var wrapperResponse = await RunPostCommand<PQSGetSessionRequestTest, ResponseTestWrapper<ComponentWithTagsResponse>>(GetTags, new PQSGetSessionRequestTest(UserName, UserPassword));
        var response = wrapperResponse.Result;

        response.Data.Should().HaveCountGreaterThan(2);
        //response.Result.Components.Should().NotBeNull();

        //var first = response.Result.Components.First();

        //first.ParameterNames.Should().NotBeNull();
        //first.Tags.Should().HaveCountGreaterThan(0);
    }

    protected async Task GetAllComponentsScenarioSlimAsync()
    {
        await UserAuthenticationOperation();

        var response = await RunPostCommand<PQSGetSessionRequestTest, ResponseTestWrapper<PQSGetSessionResponseTest>>(GetComponentScenarioTestUrl, new PQSGetSessionRequestTest(UserName, UserPassword));
        response.Result.Components.Should().NotBeNull();

        var first = response.Result.Components.First();

        //first.Parameter.Parameters.Should().NotBeNull();
        first.Tags.Should().HaveCountGreaterThan(0);
    }

    protected async Task GetBaseDataScenarioAsync()
    {
        await UserAuthenticationOperation();

        var response = await RunPostCommand<PQSGetSessionRequestTest, ResponseTestWrapper<PQSOutput>>(GetBaeDataScenarioTestUrl, new PQSGetSessionRequestTest(UserName, UserPassword));
    }


    protected async Task GetPqsTrendWidgetScenarioAsync()
    {
        await UserAuthenticationOperation();

        var request = GetJsonRequest();
        var response = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GetPqsTrendWidgeScenarioTestUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));
        response.Result.Data.First().DataTimeStamps.Should().NotBeNull();
        response.Result.Data.Length.Should().Be(3);
    }


    private PQSInput GetJsonRequest()
    {
        string json = @"
        {
            ""customParameter"": {
                ""name"": ""CP1_AVG_UNBAL_BHCYC"",
                ""value"": {
                    ""id"": 1,
                    ""name"": ""CP1_AVG_UNBAL_BHCYC"",
                    ""aggregationFunction"": ""Avg"",
                    ""group"": ""UNBAL"",
                    ""base"": ""BHCYC"",
                    ""parameterList"": [
                        {
                            ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                            ""componentId"": ""44ec313d-070c-4ede-8ddb-64fcc7bd11f0"",
                            ""quantity"": ""QAVG""
                        },
{
                            ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                            ""componentId"": ""44ec313d-070c-4ede-8ddb-64fcc7bd11f0"",
                            ""quantity"": ""QMIN""
                        },
                        {
                            ""ParamName"": ""STD_UNBAL_BHCYC_UI123_FEEDER_1"",
                            ""componentId"": ""762fcee4-ef85-4a31-9483-998f40c17116"",
                            ""quantity"": ""QMIN""

                        }
                    ]
                }
            },
            ""startDate"": ""2024-06-02T14:39:56.000Z"",
            ""endDate"": ""2024-06-06T14:39:56.000Z"",
            ""resolution"": ""IS2HOUR""
        }";

        var request = JsonConvert.DeserializeObject<PQSInput>(json);


        return request;
    }



}
