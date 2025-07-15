using FluentAssertions;
using PQBI.PQSConnection.IntegrationTests.Requests;
using PQBI.Requests;
using PQBI.Web.Controllers;

namespace PQBI.PQSConnection.IntegrationTests.Scenarios;

internal class TestScenario : ScenarioBase
{
    public TestScenario(string baseUrl) : base(baseUrl)
    {
        TestScenariosUrl = $"https://localhost:44301/PQSRestApi/{PQSRestApiController.GetComponentScenarioUrl}";

        //TolaTestUrl = "https://localhost:44301/PQSRestApi";
    }


    public string TestScenariosUrl { get; }

    public override string ScenarioName => "Test";

    public override string Description => "Ping the PQBI";

    protected async override Task RunScenario()
    {
        await GetAllComponentsScenarioSlimAsync();
    }

    protected async Task GetAllComponentsScenarioSlimAsync()
    {
        await UserAuthenticationOperation();

        var response = await RunPostCommand<PQSGetSessionRequest, GetSessionMockResponse>(TestScenariosUrl, new PQSGetSessionRequest
        {
            UserNameOrEmailAddress = "admin",
            Password = "PQSpqs12345",
        });

        response.result.Components.Should().NotBeNull();
        //response.result..Should().NotBeNullOrEmpty();
    }
}
