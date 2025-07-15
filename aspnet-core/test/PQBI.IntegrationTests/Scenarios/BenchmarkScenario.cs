using BenchmarkDotNet.Configs;
using FluentAssertions;
using Newtonsoft.Json;
using PQBI.IntegrationTests.Requests;
using PQBI.Tenants.Dashboard.Dto;
using PQBI.Web.Controllers;
using System.Diagnostics;

namespace PQBI.IntegrationTests.Scenarios;

public class CustomConfig : ManualConfig
{
    public CustomConfig()
    {
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}

internal class BenchmarkScenario : ScenarioUrlsBase
{
    public BenchmarkScenario(string baseAuthenticationUrl) : base(baseAuthenticationUrl)
    {
        GellAllRemoteComponentsUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.GetAllComponentConcurentlyUrl}";
        GetPqsTrendWidgeScenarioTestUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.GetBaeDataUrl}";
    }

    public string GellAllRemoteComponentsUrl { get; }
    public string GetPqsTrendWidgeScenarioTestUrl { get; }


    public override string ScenarioName => "Test";

    public override string Description => "Ping the PQBI";

    protected override string UserName => "admin";

    protected override string UserPassword => "PQSpqs123";

    protected async override Task RunScenario()
    {
        const int Amount_Time = 5;

        Console.WriteLine($"Current Directory {AppDomain.CurrentDomain.BaseDirectory}");
        var request = GetJsonRequest();

        //await GetAllComponentsScenarioSlimAsync(request);

        Console.WriteLine("Authentication started");
        await UserAuthenticationOperation();
        Console.WriteLine("Authentication finished");
        var measurements1 = new List<string>();
        var measurements2 = new List<string>();
        var headers = new List<string>();

        for (int i = 0; i < Amount_Time; i++)
        {
            var time1 = await MethodWrapper(RunSerialMode);
            measurements1.Add(time1.ToString());

            headers.Add($"Measurement {i + 1}");
        }

        for (int i = 0; i < Amount_Time; i++)
        {
            var time2 = await MethodWrapper(RunParallelMode);
            measurements2.Add(time2.ToString());
        }

        // Create a 2D array to hold the times
        string[,] rows = new string[2, Amount_Time];

        // Populate the 2D array with values from the lists
        for (int i = 0; i < Amount_Time; i++)
        {
            rows[0, i] = measurements1[i];
            rows[1, i] = measurements2[i];
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();

        DisplayTable(headers.ToArray(), rows);
    }


    public async Task<TimeSpan> MethodWrapper(Func<Task> callback)
    {
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        await callback();
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }


    public async Task RunSerialMode()
    {
        var request = GetJsonRequest();

        var response = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GetPqsTrendWidgeScenarioTestUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));

    }

    private async Task RunParallelMode()
    {
        var request = GetJsonRequest();
        var response = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GellAllRemoteComponentsUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));
    }


    protected async Task GetAllComponentsScenarioSlimAsync_Origin()
    {
        await UserAuthenticationOperation();

        var request = GetJsonRequest();

        var stopwatch1 = new Stopwatch();
        stopwatch1.Start();
        var response = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GetPqsTrendWidgeScenarioTestUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));
        stopwatch1.Stop();

        var duration1 = stopwatch1.Elapsed;



        var stopwatch2 = new Stopwatch();
        stopwatch2.Start();
        var batchReponse = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GellAllRemoteComponentsUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));
        stopwatch2.Stop();
        var duration2 = stopwatch2.Elapsed;


        batchReponse.Result.Data.Length.Should().Be(response.Result.Data.Length);
        for (int i = 0; i < batchReponse.Result.Data.Length; i++)
        {
            var timestemps1 = batchReponse.Result.Data[i].DataTimeStamps;
            var timestemps2 = response.Result.Data[i].DataTimeStamps;

            for (int index = 0; index < timestemps1.Length; index++)
            {

                var point1 = timestemps1[index].Point;
                var point2 = timestemps2[index].Point;

                point1.Should().Be(point2);
            }
        }
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
                    //{
                    //    ""componentId"": ""762fcee4-ef85-4a31-9483-998f40c17116"",
                    //    ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                    //    ""quantity"": ""QAVG""
                    //},
                    //{
                    //    ""componentId"": ""44ec313d-070c-4ede-8ddb-64fcc7bd11f0"",
                    //    ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                    //    ""quantity"": ""QAVG""
                    //},
                    {
                        ""componentId"": ""3649ca3e-0651-4027-8c44-9110e5b8786c"",
                        ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                        ""quantity"": ""QAVG""
                    },
                    {
                        ""componentId"": ""08c3912f-0275-4278-bf86-917168d88eef"",
                        ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                        ""quantity"": ""QAVG""
                    },
                    {
                        ""componentId"": ""990c3c32-e441-4e12-9e5f-7fdeb2d7ba5e"",
                        ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                        ""quantity"": ""QAVG""
                    },
                    {
                        ""componentId"": ""0e481fab-ad7f-45bb-9244-b0f2d0fc5dcc"",
                        ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                        ""quantity"": ""QAVG""
                    },
                    {
                        ""componentId"": ""a059db13-2390-432a-a062-6ac41f213612"",
                        ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                        ""quantity"": ""QAVG""
                    },
                    {
                        ""componentId"": ""e768875f-958e-4742-a3e6-5a85273c43b8"",
                        ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                        ""quantity"": ""QAVG""
                    }
                ]
            }
        },
        ""startDate"": ""2024-02-15T14:39:56.000Z"",
        ""endDate"": ""2024-06-17T14:39:56.000Z"",
        ""resolution"": ""IS2HOUR""
    }";
        var request = JsonConvert.DeserializeObject<PQSInput>(json);


        return request;
    }


    protected async Task GetAllComponentsScenarioSlimAsync(PQSInput request)
    {
        await UserAuthenticationOperation();

        var stopwatch1 = new Stopwatch();
        stopwatch1.Start();

        var response = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GetPqsTrendWidgeScenarioTestUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));
        stopwatch1.Stop();

        var duration1 = stopwatch1.Elapsed;



        var stopwatch2 = new Stopwatch();
        stopwatch2.Start();
        var batchReponse = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GellAllRemoteComponentsUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));
        stopwatch2.Stop();
        var duration2 = stopwatch2.Elapsed;


        batchReponse.Result.Data.Length.Should().Be(response.Result.Data.Length);
        for (int i = 0; i < batchReponse.Result.Data.Length; i++)
        {
            var timestemps1 = batchReponse.Result.Data[i].DataTimeStamps;
            var timestemps2 = response.Result.Data[i].DataTimeStamps;

            for (int index = 0; index < timestemps1.Length; index++)
            {

                var point1 = timestemps1[index].Point;
                var point2 = timestemps2[index].Point;

                point1.Should().Be(point2);
            }
        }
    }

}
