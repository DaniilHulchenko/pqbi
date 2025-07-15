using PQBI.IntegrationTests.Requests;
using PQBI.PQS;
using PQBI.Web.Controllers;
using System.Text.Json;


namespace PQBI.IntegrationTests.Scenarios;

internal class TreeBuilderScenario : ScenarioUrlsBase
{
    public TreeBuilderScenario(string baseAuthenticationUrl) : base(baseAuthenticationUrl)
    {
        PostTreetIntegrationTestUrl = $"{BaseUrl}api/services/app/TreeBuilder/{TreeBuilderController.PostTreetIntegrationTestUrl}";
        PostLogicalChannelTreeTestUrl = $"{BaseUrl}api/services/app/TreeBuilder/{TreeBuilderController.PostLogicalChannelTreeUrl}";
        PostTreeTabletIntegrationTestUrl = $"{BaseUrl}api/services/app/TreeBuilder/{TreeBuilderController.PostTreeTabletIntegrationTestUrl}";

        CheckGetBaseDataTreeUrl = $"{BaseUrl}TreeBuilder/{TreeBuilderController.CheckGetBaseDataTreeUrl}";
    }

    public string PostTreetIntegrationTestUrl { get; }
    public string PostLogicalChannelTreeTestUrl { get; }
    public string PostTreeTabletIntegrationTestUrl { get; }
    public string CheckGetBaseDataTreeUrl { get; }


    public override string ScenarioName => "Test";

    public override string Description => "Get Tree";

    //protected override string UserName => "admin";

    //protected override string UserPassword => "PQSpqs12345";

    protected async override Task RunScenario()
    {
        await GetOmnibusTree();

        //await GetBaseData();
        //await GetLogicalChannelTree();

    }

    private async Task GetBaseData()
    {
        await UserAuthenticationOperation();

        var start = DateTime.UtcNow.AddDays(-120);
        var end = DateTime.UtcNow.AddDays(-1);

        var response = await RunPostCommand<CheckGetBaseDataTreeRequestTest, ResponseTestWrapper<StaticTreeNode>>(CheckGetBaseDataTreeUrl, new CheckGetBaseDataTreeRequestTest(UserName, UserPassword,[ "0e481fab-ad7f-45bb-9244-b0f2d0fc5dcc", "a059db13-2390-432a-a062-6ac41f213612"])); //,"0e481fab-ad7f-45bb-9244-b0f2d0fc5dcc", "a059db13-2390-432a-a062-6ac41f213612"


        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(response.Result, options);

        await UserLogoutOperation();
    }

    private async Task GetTreeTable()
    {
        await UserAuthenticationOperation();

        var start = DateTime.UtcNow.AddDays(-120);
        var end = DateTime.UtcNow.AddDays(-1);

        var response = await RunPostCommand<PostEventstRequestTest, ResponseTestWrapper<TagTreeRootDto>>(PostTreeTabletIntegrationTestUrl, new PostEventstRequestTest(UserName, UserPassword,
            new GetEventstRequest(start, end
            , "0e481fab-ad7f-45bb-9244-b0f2d0fc5dcc", "a059db13-2390-432a-a062-6ac41f213612"))); //,"0e481fab-ad7f-45bb-9244-b0f2d0fc5dcc", "a059db13-2390-432a-a062-6ac41f213612"


        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(response.Result, options);

        await UserLogoutOperation();
    }

    private async Task GetLogicalChannelTree()
    {
        await UserAuthenticationOperation();

        var response = await RunPostCommand<PostGetLogicalTreeRequestTest, ResponseTestWrapper<DynamicTreeNode>>(PostLogicalChannelTreeTestUrl, new PostGetLogicalTreeRequestTest(UserName, UserPassword, "a059db13-2390-432a-a062-6ac41f213612"));

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(response.Result, options);
    }

    private async Task GetOmnibusTree()
    {
        await UserAuthenticationOperation();

        var response = await RunPostCommand<PQSGetSessionRequestTest, ResponseTestWrapper<TagTreeRootDto>>(PostTreetIntegrationTestUrl, new PQSGetSessionRequestTest(UserName, UserPassword));

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(response.Result, options);

    }



}
