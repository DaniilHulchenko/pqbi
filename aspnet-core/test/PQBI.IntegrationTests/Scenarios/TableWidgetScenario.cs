//using FluentAssertions;
//using Newtonsoft.Json;
//using PQBI.IntegrationTests.Requests;
//using PQBI.Migrations.Seed.Tenants;
//using PQBI.PQS.CalcEngine;
//using PQBI.Web.Controllers;
//using System.Text.Json;

//namespace PQBI.IntegrationTests.Scenarios;

//internal class TableWidgetScenario : ScenarioUrlsBase
//{
//    public TableWidgetScenario(string baseAuthenticationUrl) : base(baseAuthenticationUrl)
//    {
//        PostCustomerParametersIntegrationTestsUrl = $"{BaseUrl}{PQSRestApiController.ControllerPrefixUrl}PQSRestApi/{PQSRestApiController.PostCustomerParametersIntegrationTestsUrl}";
//    }

//    public string PostCustomerParametersIntegrationTestsUrl { get; }



//    public override string ScenarioName => "Test";

//    public override string Description => "Get Table";

//    //protected override string UserName => "admin";

//    //protected override string UserPassword => "PQSpqs12345";

//    protected async override Task RunScenario()
//    {
//        //await SingleParameter_Calculation();
//        await MultiParameter_Calculation();

//        //await BaseParameter_Calculation();

//        //await EventParameter_Calculation();

//    }

//    #region Event

//    private async Task EventParameter_Calculation()
//    {
//        var request = GetEventRequest();
//        var firstParameter = request.Parameters.First();


//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest222, ResponseTestWrapper<TableWidgetResponse>>(PostCustomerParametersIntegrationTestsUrl, new PQSCalculationRequestTest222(UserName, UserPassword, request));
//        response.Result.Items.Count().Should().Be(1);

//        await UserLogoutOperation();
//    }

//    private TableWidgetRequest GetEventRequest()
//    {
//        //30007 - variation more than single phase
//        var json = """
//{
//  "parameters": [
//    {
//      "type": "Event",
//      "parameterName": "Temperature",
//      "quantity": "AVG",
//      "data": "{\"EventId\":30007,\"Phases\":[\"PH1\",\"PH2\"],\"Parameter\":\"Deviation\",\"IsPolyphase\":false,\"AggregationInSeconds\":null}"
//    }
//  ],
//  "feeders": [
//    {
//      "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//      "id": 1,
//      "name": "Feeder_0"
//    },
//    {
//      "parent": "a059db13-2390-432a-a062-6ac41f213612",
//      "id": 1,
//      "name": "Feeder_0"
//    }
//  ],
//  "startDate": "2024-08-12T05:07:00.0Z",
//  "endDate": "2024-11-23T08:07:00.0Z"
//}
//""";



//        var dto = JsonConvert.DeserializeObject<TableWidgetRequest>(json);
//        //dto.Parameters.First().Data = DefaultTenantBuilder.Exception_CustomerParameterId.ToString();


//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }

//    #endregion

//    #region Single Parameter

//    private async Task SingleParameter_Calculation()
//    {
//        TableWidgetRequest request = GetSingleParameterRequest();
//        await UserAuthenticationOperation();

//        //var serverRequest = new PQSCalculationRequestTest222(UserName, UserPassword, new TableWidgetRequest() );
//        var serverRequest = new PQSCalculationRequestTest222(UserName, UserPassword, request);
//        var response = await RunPostCommand<PQSCalculationRequestTest222, ResponseTestWrapper<TableWidgetResponse>>(PostCustomerParametersIntegrationTestsUrl, serverRequest);
//        response.Result.Items.Count().Should().Be(2);


//        await UserLogoutOperation();
//    }

//    private TableWidgetRequest GetSingleParameterRequest()
//    {
//        var json = """

//        {
//          "parameters": [
//            {
//              "type": "CustomParameter",
//              "parameterName": "Pressure",
//              "quantity": "MAX",
//              "data": "{\"CustomerId\": 12345}"
//            }
//          ],
//           "feeders": [
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
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T08:07:00.0Z"
//        }

//        """;


//        var dto = JsonConvert.DeserializeObject<TableWidgetRequest>(json);
//        dto.Parameters.First().Data = DefaultTenantBuilder.Single_ParameterCustomerId.ToString();
//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }
//    #endregion


//    #region BaseParameter

//    private async Task BaseParameter_Calculation()
//    {
//        var request = GetBaseParameterRequest();
//        var firstParameter = request.Parameters.First();



//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest222, ResponseTestWrapper<TableWidgetResponse>>(PostCustomerParametersIntegrationTestsUrl, new PQSCalculationRequestTest222(UserName, UserPassword, request));
//        response.Result.Items.Count().Should().Be(4);

//        await UserLogoutOperation();
//    }

//    private TableWidgetRequest GetBaseParameterRequest()
//    {
//        var json = """

//            {
//              "parameters": [
//                {
//                  "type": "BaseParameter",
//                  "parameterName": "Temperature",
//                  "quantity": "AVG",
//                 "data": "{\"type\":\"LOGICAL\",\"name\":\"bp\",\"feeder\":null,\"group\":\"RMS\",\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"base\":\"BHCYC\",\"quantity\":null,\"resolution\":\"IS1HOUR\",\"operator\":null,\"aggregationFunction\":null}"
//                }
//              ],
//               "feeders": [
//                            {
//                                "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//                                "id": 1,
//                                "name": "Feeder_0"
//                            },
//                            {
//                                "parent": "a059db13-2390-432a-a062-6ac41f213612",
//                                "id": 1,
//                                "name": "Feeder_0"
//                            }
//                        ],
//                "startDate": "2024-08-17T05:07:00.0Z",
//                "endDate": "2024-08-22T08:07:00.0Z"
//            }

//            """;


//        var dto = JsonConvert.DeserializeObject<TableWidgetRequest>(json);


//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }

//    #endregion

//    #region Multi Parameter

//    private async Task MultiParameter_Calculation()
//    {
//        TableWidgetRequest request = GetMultiParameterRequest();
//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest222, ResponseTestWrapper<TableWidgetResponse>>(PostCustomerParametersIntegrationTestsUrl, new PQSCalculationRequestTest222(UserName, UserPassword, request));
//        response.Result.Items.Count().Should().Be(3);


//        await UserLogoutOperation();
//    }

//    private TableWidgetRequest GetMultiParameterRequest()
//    {
//        var json = """

//{
//  "parameters": [
//    {
//      "type": "CustomParameter",
//      "parameterName": "Pressure",
//      "quantity": "MAX",
//      "data": "{\"CustomerId\": 12345}"
//    }
//  ],
//  "feeders": [
//    {
//      "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//      "id": 1,
//      "name": "Feeder_0"
//    },
//    {
//      "parent": "a059db13-2390-432a-a062-6ac41f213612",
//      "id": 1,
//      "name": "Feeder_0"
//    }
//  ],
//  "components": [
//    {
//      "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//      "componentName": "xxx",
//      "tags": ["tag1", "tag2"]
//    }
//  ],
//  "startDate": "2024-08-17T05:07:00.0Z",
//  "endDate": "2024-08-17T08:07:00.0Z"
//}

//""";



//        var dto = JsonConvert.DeserializeObject<TableWidgetRequest>(json);
//        dto.Parameters.First().Data = DefaultTenantBuilder.Multi_ParameterCustomeId.ToString();
//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }
//    #endregion

//}
