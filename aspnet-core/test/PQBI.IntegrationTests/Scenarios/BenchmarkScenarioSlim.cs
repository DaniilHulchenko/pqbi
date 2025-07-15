using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;
using PQBI.IntegrationTests.Requests;
using PQBI.Tenants.Dashboard.Dto;
using PQBI.Web.Controllers;

namespace PQBI.IntegrationTests.Scenarios;

public class BenchmarkScenarioSlim
{
    private static readonly string GellAllRemoteComponentsUrl = $"https://localhost:44301/PQSRestApi/GetAllComponentConcurent";
    private static readonly string GetPqsTrendWidgeScenarioTestUrl = $"https://localhost:44301/PQSRestApi/PQSData";

    protected string UserName { get; } = "admin";
    protected string UserPassword { get; } = "PQSpqs123";



    [Benchmark]
    public async Task RunSerialMode()
    {
        var request = GetJsonRequest();

        var response = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GetPqsTrendWidgeScenarioTestUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));
    }

    [Benchmark]
    public async Task RunParallelMode()
    {
        var request = GetJsonRequest();

        var response = await RunPostCommand<GetPqsWidgetRequestTest, ResponseTestWrapper<PQSOutput>>(GellAllRemoteComponentsUrl, new GetPqsWidgetRequestTest(UserName, UserPassword, request));
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
                                ""componentId"": ""762fcee4-ef85-4a31-9483-998f40c17116"",
                                ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                                ""quantity"": ""QAVG""
                            },
                            {
                                ""componentId"": ""44ec313d-070c-4ede-8ddb-64fcc7bd11f0"",
                                ""ParamName"": ""STD_UNBAL_BHCYC_UV123_FEEDER_1"",
                                ""quantity"": ""QAVG""
                            },
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
                ""startDate"": ""2024-06-15T14:39:56.000Z"",
                ""endDate"": ""2024-06-17T14:39:56.000Z"",
                ""resolution"": ""IS2HOUR""
            }";

        return JsonConvert.DeserializeObject<PQSInput>(json);
    }

    protected async Task<TResponse> RunPostCommand<TRequest, TResponse>(string url, TRequest request) where TRequest : class
    {
        return await RunPutOrPostCommand<TRequest, TResponse>(url, request, isPostRequest: true);

    }

    protected async Task<TResponse> RunPutCommand<TRequest, TResponse>(string url, TRequest request) where TRequest : class
    {

        return await RunPutOrPostCommand<TRequest, TResponse>(url, request, isPostRequest: false);
    }

    private async Task<TResponse> RunPutOrPostCommand<TRequest, TResponse>(string url, TRequest request, bool isPostRequest = true)
    {
        using (HttpClient client = new HttpClient())
        {

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = null;

            if (isPostRequest)
            {
                response = await client.PostAsync(url, content);
            }
            else
            {
                response = await client.PutAsync(url, content);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var instance = JsonConvert.DeserializeObject<TResponse>(responseContent);
                return instance;
            }

            throw new Exception($"Failed Populate in {url}");
        }
    }
}
