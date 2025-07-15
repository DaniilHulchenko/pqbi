//using FluentAssertions;
//using Newtonsoft.Json;
//using PQBI.IntegrationTests.Requests;
//using PQBI.Migrations.Seed.Tenants;
//using PQBI.PQS.CalcEngine;
//using PQBI.Web.Controllers;
//using System.Text.Json;

//namespace PQBI.IntegrationTests.Scenarios;

////---------------------------------------

//internal class RemoteTrendScenario : ScenarioUrlsBase
//{
//    public RemoteTrendScenario(string baseAuthenticationUrl) : base(baseAuthenticationUrl)
//    {
//        PostTrendCalculationIntegrationTestUrl = $"{BaseUrl}EngineCalculator/{EngineCalculatorController.PostTrendCalculationIntegrationTestUrl}";
//        PostCustomParameterIntegrationTestUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.PostCustomerParametersIntegrationTestsUrl}";
//        PostChartBarIntegrationTestsUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.PostChartBarIntegrationTestsUrl}";
//    }

//    public string PostTrendCalculationIntegrationTestUrl { get; }
//    public string PostCustomParameterIntegrationTestUrl { get; }
//    public string PostChartBarIntegrationTestsUrl { get; }



//    public override string ScenarioName => "Test";

//    public override string Description => "Get Tree";

//    protected override string UserName => "admin";

//    protected override string UserPassword => "PQSpqs12345";

//    protected async override Task RunScenario()
//    {
//        //await ExceptioniCalculation_Component();

//        //await SingleMaxParameters_Component();

//        await SingleParameterMultiComponenr();

//        await MultiParameterAndSingleComponent();

//        await BPCP_Parameter_Calculation();

//        await Arithmetic();

//    }

//    #region Exception

//    private async Task ExceptioniCalculation_Component()
//    {
//        var request = GetExceptionRequest();
//        var firstParameter = request.Parameters.First();

//        firstParameter.Feeders = null;
//        //firstParameter.ApplyToDos = null;


//        //var request = GetSingleParameterMultiComponentRequest(DefaultTenantBuilder.Single_MaxPoints_ParameterCustomerId);

//        await UserAuthenticationOperation();

//        var tmp = new PQSCalculationRequestTest(UserName, UserPassword, request);
//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, tmp);
//        response.Result.Calculated.Data.Count().Should().Be(1);

//        await UserLogoutOperation();
//    }

//    private TrendCalcRequest222 GetExceptionRequest()
//    {
//        var json = """

//                        {
//              "resolution": "IS1HOUR",
//              "startDate": "2024-8-15T05:07:00.0Z",
//              "endDate": "2024-08-15T08:07:00.0Z",
//              "parameters": [
//                {
//                   "feeders": [
//                    {
//                      "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//                      "id": 1,
//                      "name": "Feeder_0"
//                    },
//                  {
//                  "parent": "a059db13-2390-432a-a062-6ac41f213612",
//                  "id": 1,
//                  "name": "Feeder_0"
//                }
//                  ],
//                  "Type": "Exception",
//                  "Quantity":"avg",
//                  "Data": "{\"CustomerId\": 12345}"
//                }
//              ]
//            }
            

//            """;


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        dto.Parameters.First().Data = DefaultTenantBuilder.Exception_CustomerParameterId.ToString();


//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }

//    #endregion

//    #region Single Max Parameters
//    private async Task SingleMaxParameters_Component()
//    {
//        var request = GetSingleParameterMultiComponentRequest(DefaultTenantBuilder.Single_MaxPoints_ParameterCustomerId);
//        request.Resolution = "auto(1000)";

//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(1);

//        await UserLogoutOperation();
//    }


//    #endregion

//    #region Single Parameter

//    private async Task SingleParameterMultiComponenr()
//    {
//        var request = GetSingleParameterMultiComponentRequest(DefaultTenantBuilder.Single_ParameterCustomerId);
//        await UserAuthenticationOperation();
//        var tmp = DateTime.Now.AddDays(-30);

//        request.StartDate = DateTime.Now.AddDays(-30);
//        request.EndDate = DateTime.Now;


//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(1);

//        var stack = new Stack<int>();
//        stack.Push(338);
//        stack.Push(339);
//        stack.Push(338);

//        foreach (var set in response.Result.Calculated.Data)
//        {
//            foreach (var item in set.Data)
//            {
//                var strInt = item.ToString().Split('.')[0];

//                var pop = stack.Pop();
//                pop.ToString().Should().Be(strInt);
//            }

//        }

//        await UserLogoutOperation();

//    }

//    private TrendCalcRequest222 GetSingleParameterMultiComponentRequest(int customParameterId)
//    {
//        var json2 = """

//                        {
//              "resolution": "IS1HOUR",
//              "startDate": "2024-8-15T05:07:00.0Z",
//              "endDate": "2024-08-15T08:07:00.0Z",
//              "parameters": [
//                {
//                  "feeders": [
//                    {
//                      "parent": "223dc3ad-6c22-48dc-af8a-4d8b971e4e65",
//                      "id": 1,
//                      "name": "Feeder_0"
//                    },
//                  {
//                  "parent": "0176e40e-1ec7-4043-84ad-e85fa17c9000",
//                  "id": 1,
//                  "name": "Feeder_0"
//                }
//                  ],
//                  "Type": "CustomParameter",
//                  "Quantity":"avg",
//                  "Data": "{\"CustomerId\": 12345}"
//                }
//              ]
//            }
            

//            """;


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json2);
//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }

//    #endregion

//    #region Multi Parameter

//    private async Task MultiParameterAndSingleComponent()
//    {
//        var request = GetMultiParameterRequest();
//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        //var stack = new Queue<int>();
//        //stack.Enqueue(351);
//        //stack.Enqueue(351);
//        //stack.Enqueue(351);
//        //stack.Enqueue(159);``
//        //stack.Enqueue(159);
//        //stack.Enqueue(159);

//        //foreach (var set in response.Result.Calculated.Data)
//        //{
//        //    foreach (var item in set.Data)
//        //    {
//        //        var strInt = item.ToString().Split('.')[0];

//        //        var pop = stack.Dequeue();
//        //        pop.ToString().Should().Be(strInt);
//        //    }
//        //}

//        await UserLogoutOperation();
//    }

//    private TrendCalcRequest222 GetMultiParameterRequest()
//    {
//        var json = """
//        {
//            "resolution": "IS1HOUR",
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T08:07:00.0Z",
//            "parameters": [
//                {
//                    "applyToDos": [
//                        {
//                            "guid": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "name": "xxx"
//                        },
//                        {
//                            "guid": "a059db13-2390-432a-a062-6ac41f213612",
//                            "name": "R&D LAB - 14"
//                        }
//                    ],
//                    "feeders": [
//                        {
//                            "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "id": 1,
//                            "name": "Feeder_0"
//                        },
//                        {
//                            "parent": "a059db13-2390-432a-a062-6ac41f213612",
//                            "id": 1,
//                            "name": "Feeder_0"
//                        }
//                    ],
//                    "Type": "CustomParameter",
//                    "Quantity": "avg",
//                    "Data": "{\"CustomerId\": 12345}"
//                }
//            ]
//        }
//        """;


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        dto.Parameters.First().Data = DefaultTenantBuilder.Multi_ParameterCustomeId.ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }
//    #endregion

//    #region BaseParameter
//    private async Task BPCP_Parameter_Calculation()
//    {
//        var request = GetBPCPRequest();
//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        //var stack = new Stack<int>();
//        //stack.Push(338);
//        //stack.Push(339);
//        //stack.Push(338);

//        //foreach (var set in response.Result.Calculated.Data)
//        //{
//        //    foreach (var item in set.Data)
//        //    {
//        //        var strInt = item.ToString().Split('.')[0];

//        //        var pop = stack.Pop();
//        //        pop.ToString().Should().Be(strInt);
//        //    }

//        //}

//        await UserLogoutOperation();
//    }


//    private TrendCalcRequest222 GetBPCPRequest()
//    {
//        var json = """

//            {
//              "resolution": "IS1HOUR",
//              "startDate": "2024-8-15T05:07:00.0Z",
//              "endDate": "2024-08-15T08:07:00.0Z",
//              "parameters": [
//                {
//                   "feeders": [
//                    {
//                      "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//                      "id": 1,
//                      "name": "Feeder_0"
//                    },
//                  {
//                  "parent": "a059db13-2390-432a-a062-6ac41f213612",
//                  "id": 1,
//                  "name": "Feeder_0"
//                }
//                  ],
//                  "type": "BaseParameter",
//                  "quantity": "AVG",
//                  "data": "{\"type\":\"LOGICAL\",\"name\":\"bp\",\"feeder\":null,\"group\":\"RMS\",\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"base\":\"BHCYC\",\"quantity\":null,\"resolution\":null,\"operator\":null,\"aggregationFunction\":null}"
//                }
//              ]
//            }
            
            

//            """;


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.CustomParameterIds = new List<int> { DefaultTenantBuilder.BPCP_CustomerParameterId };


//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }


//    #endregion

//    #region Arithmetic

//    private async Task Arithmetic()
//    {
//        var request = GetArithmeticRequest();
//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        //var stack = new Queue<int>();
//        //stack.Enqueue(351);
//        //stack.Enqueue(351);
//        //stack.Enqueue(351);
//        //stack.Enqueue(159);
//        //stack.Enqueue(159);
//        //stack.Enqueue(159);

//        //foreach (var set in response.Result.Calculated.Data)
//        //{
//        //    foreach (var item in set.Data)
//        //    {
//        //        var strInt = item.ToString().Split('.')[0];

//        //        var pop = stack.Dequeue();
//        //        pop.ToString().Should().Be(strInt);
//        //    }
//        //}

//        await UserLogoutOperation();
//    }

//    private TrendCalcRequest222 GetArithmeticRequest()
//    {
//        var json = """
//        {
//            "resolution": "IS1HOUR",
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T08:07:00.0Z",
//            "parameters": [
//                {
//                    "applyToDos": [
//                        {
//                            "guid": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "name": "xxx"
//                        },
//                        {
//                            "guid": "a059db13-2390-432a-a062-6ac41f213612",
//                            "name": "R&D LAB - 14"
//                        }
//                    ],
//                    "feeders": [
//                        {
//                            "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "id": 1,
//                            "name": "Feeder_0"
//                        },
//                        {
//                            "parent": "a059db13-2390-432a-a062-6ac41f213612",
//                            "id": 1,
//                            "name": "Feeder_0"
//                        }
//                    ],
//                    "Type": "CustomParameter",
//                    "Quantity": "avg",
//                    "Data": "{\"CustomerId\": 12345}"
//                }
//            ]
//        }
//        """;


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        dto.Parameters.First().Data = DefaultTenantBuilder.MultiParameterMC_Arithmetic_CustomerParameterId.ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;
//    }
//    #endregion


//}