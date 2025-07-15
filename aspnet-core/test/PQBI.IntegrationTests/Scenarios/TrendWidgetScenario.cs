//using FluentAssertions;
//using k8s;
//using Newtonsoft.Json;
//using PQBI.CustomParameters;
//using PQBI.CustomParameters.Dtos;
//using PQBI.IntegrationTests.Requests;
//using PQBI.Migrations.Seed.Tenants;
//using PQBI.PQS;
//using PQBI.PQS.CalcEngine;
//using PQBI.Web.Controllers;
//using System.Text.Json;

//namespace PQBI.IntegrationTests.Scenarios;

//internal class TrendWidgetScenario : ScenarioUrlsBase
//{
//    public TrendWidgetScenario(string baseAuthenticationUrl) : base(baseAuthenticationUrl)
//    {
//        PostTrendCalculationIntegrationTestUrl = $"{BaseUrl}EngineCalculator/{EngineCalculatorController.PostTrendCalculationIntegrationTestUrl}";
//        PostCustomParameterIntegrationTestUrl = $"{BaseUrl}PQSRestApi/{PQSRestApiController.PostCustomerParametersIntegrationTestsUrl}";
//        PostCustomParameterUrl = $"{BaseUrl}api/services/app/{ICustomParametersAppService.CustomParameter}/{nameof(ICustomParametersAppService.CreateOrEdit)}";
//        //api/services/app/CustomParameters/CreateOrEdit

//    }

//    public string PostTrendCalculationIntegrationTestUrl { get; }
//    public string PostCustomParameterIntegrationTestUrl { get; }
//    public string PostChartBarIntegrationTestsUrl { get; }
//    public string PostCustomParameterUrl { get; }



//    public override string ScenarioName => Description;

//    public override string Description => "Trend Widget";

//    //protected override string UserName => "admin";

//    //protected override string UserPassword => "PQSpqs12345";

//    protected async override Task RunScenario()
//    {
//        await ExceptioniCalculation_Component();


//        await Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring_Override_10Min();
//        await Multi_Sanity();
//        await Single_Sanity();

//        //await Multi_Sanity_With_Zero_Parameters();
//        //await Multi_Sanity_Without_Interpolation_id();

//        //await Multi_Sanity_With_Interpolation_id();


//        //await Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter();
//        //await Single_Max_Percentile_Component();



//        //await MultiComponent_HeighertResolution_With_Single_Custom_Paramer_Component();
//        //await MultiComponent_With_Single_HeighertResolution_Custom_Paramer_Component();

//        //await SingleParameter_With_Multi_Parameter_0_BaseParameters_Component(); // return 0 BaseParameters!!!!!!!!!!!!!!!!!!!!!!
//        //await SingleComponent__With_Multi_Custom_Paramer_Component();
//        //await MultiComponent__With_Multi_Custom_Paramer_Component();

//        //await SingleComponent__With_2222_Multi_Custom_Paramer_Component();


//        //await SingleMaxParameters_Component();
//        //await SingleParameterMultiComponenr();

//        //await MultiParameterAndSingleComponent();

//        //await BPCP_Parameter_Calculation();

//        // await Arithmetic();

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
//        Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest GetExceptionRequest()
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

//    #region Sanity

//    private async Task Single_Sanity()
//    {
//        await UserAuthenticationOperation();

//        var customParameterRequest = SingleCustomParameter__Request();
//        var customParameterResponse = await RunPostCommand<CreateOrEditCustomParameterDto, ResponseTestWrapper<CreateOrEditCustomParameterDto>>(PostCustomParameterUrl, customParameterRequest, token: Token);
//        var customParameterId = customParameterResponse.Result.Id;
//        customParameterId.Should().BeGreaterThan(0);



//        var request = Trend__Request(customParameterId.Value);
//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        var data = response.Result.Calculated.Data;
//        var count = response.Result.Calculated.Data.Count();

//        response.Result.Calculated.Data.Count().Should().Be(1);


//        await UserLogoutOperation();
//    }

//    private async Task Multi_Sanity()
//    {
//        await UserAuthenticationOperation();

//        var customParameterRequest = MultiCustomParameter__Request();
//        var customParameterResponse = await RunPostCommand<CreateOrEditCustomParameterDto, ResponseTestWrapper<CreateOrEditCustomParameterDto>>(PostCustomParameterUrl, customParameterRequest, token: Token);
//        var customParameterId = customParameterResponse.Result.Id;
//        customParameterId.Should().BeGreaterThan(0);



//        var request = Trend__Request(customParameterId.Value);
//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        var data = response.Result.Calculated.Data;
//        var count = response.Result.Calculated.Data.Count();

//        response.Result.Calculated.Data.Count().Should().Be(2);


//        await UserLogoutOperation();
//    }

//    private CreateOrEditCustomParameterDto MultiCustomParameter__Request()
//    {
//        var json = """
//        {

//            "Name": "Samity_MultiParameter",
//            "aggregationFunction": "MAX()",
//            "STDPQSParametersList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"Mult(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":\"IS1MIN\",\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"base\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":\"IS1MIN\",\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"base\":\"BHCYC\",\"id\":\"zD3e5\"}]",
//            "Type": "MPSC",
//            "resolution": "IS1MIN",
//            "innerCustomParameters": "[]"
//        }
//        """;

//        var dto = JsonConvert.DeserializeObject<CreateOrEditCustomParameterDto>(json);
//        return dto;
//    }

//    private CreateOrEditCustomParameterDto SingleCustomParameter__Request()
//    {
//        var json = """
//        {
//            "name": "jjjj",
//            "aggregationFunction": "COUNT()",
//             "STDPQSParametersList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"Mult(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":\"IS1MIN\",\"phase\":\"UV1N\",\"base\":\"BHCYC\",\"id\":\"6nCqj\"}]",
        
//            "type": "SPMC",
//            "resolution": "IS1MIN",
//            "innerCustomParameters": "[]"
//        }
//        """;

//        var dto = JsonConvert.DeserializeObject<CreateOrEditCustomParameterDto>(json);
//        return dto;
//    }

//    private TrendCalcRequest Trend__Request(int customParameterId)
//    {
//        var json = """
//        {
//            "resolution": "IS5HOUR",
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T10:07:00.0Z",
//            "parameters": [
//                {
//                    "applyToDos": [
//                        {
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;
//        //"CustomParameterIds": "[203]"


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().CustomParameterIds = [203];
//        dto.Parameters.First().Data = customParameterId.ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;
//    }

//    #endregion

//    #region Multi_Sanity_With_Zero_Parameters

//    private async Task Multi_Sanity_With_Zero_Parameters()
//    {
//        //xxxxxxxxxxxxxxxx
//        var request = Multi_Sanity_With_Zero_Parameters__Request(DefaultTenantBuilder.Sanity_Multi_WithZero_Parameters_Id);
//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        await UserLogoutOperation();

//        var data = response.Result.Calculated.Data;

//        data.Count().Should().Be(2);
//        foreach (var item in data)
//        {
//            item.Data.Count().Should().Be(1);
//        }

//        Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest Multi_Sanity_With_Zero_Parameters__Request(int customParameterId)
//    {
//        var json = """
//        {
//            "resolution": "IS5HOUR",
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T10:07:00.0Z",
//            "parameters": [
//                {
//                    "applyToDos": [
//                        {
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;
//        //"CustomParameterIds": "[203]"


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().CustomParameterIds = [203];
//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;


//        //var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        //var options = new JsonSerializerOptions { WriteIndented = true };
//        //var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        //return dto;

//    }

//    #endregion


//    #region Multi_Sanity_Without_Interpolation_id

//    private async Task Multi_Sanity_Without_Interpolation_id()
//    {
//        //xxxxxxxxxxxxxxxx
//        var request = Multi_Sanity_Without_Interpolation_id___Request(DefaultTenantBuilder.Sanity_Without_Interpolation_id);
//        await UserAuthenticationOperation();




//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(4);


//        await UserLogoutOperation();

//        var data = response.Result.Calculated.Data;

//        foreach (var item in data)
//        {
//            item.Data.Count().Should().Be(1);
//        }

//        Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest Multi_Sanity_Without_Interpolation_id___Request(int customParameterId)
//    {
//        var json = """
//        {
//            "resolution": "IS5HOUR",
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T10:07:00.0Z",
//            "parameters": [
//                {
//                    "applyToDos": [
//                        {
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;
//        //"CustomParameterIds": "[203]"


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().CustomParameterIds = [203];
//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;


//        //var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        //var options = new JsonSerializerOptions { WriteIndented = true };
//        //var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        //return dto;

//    }

//    #endregion



//    #region Multi_Sanity_With_Interpolation_id

//    private async Task Multi_Sanity_With_Interpolation_id()
//    {
//        //xxxxxxxxxxxxxxxx
//        var request = Multi_Sanity_With_Interpolation_id___Request(DefaultTenantBuilder.Sanity_With_Interpolation_id);
//        await UserAuthenticationOperation();




//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        await UserLogoutOperation();

//        var data = response.Result.Calculated.Data;

//        data.Count().Should().Be(2);
//        foreach (var item in data)
//        {
//            item.Data.Count().Should().Be(1);
//        }

//        Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest Multi_Sanity_With_Interpolation_id___Request(int customParameterId)
//    {
//        var json = """
//        {
//            "resolution": "IS4HOUR",
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T09:07:00.0Z",
//            "parameters": [
//                {
//                    "applyToDos": [
//                        {
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;
//        //"CustomParameterIds": "[203]"


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().CustomParameterIds = [203];
//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;


//        //var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        //var options = new JsonSerializerOptions { WriteIndented = true };
//        //var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        //return dto;

//    }

//    #endregion



//    #region Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter

//    private async Task Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter()
//    {
//        //xxxxxxxxxxxxxxxx
//        var request = MultiComponent__With_Single_Custom_Paramer___Request(DefaultTenantBuilder.Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter_id);
//        await UserAuthenticationOperation();


//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        await UserLogoutOperation();
//        Verification(response.Result.Calculated);
//    }

//    #endregion

//    #region MultiComponent_With_Single_HeighertResolution_Custom_Paramer_Component

//    private async Task MultiComponent_With_Single_HeighertResolution_Custom_Paramer_Component()
//    {
//        //xxxxxxxxxxxxxxxx
//        var request = MultiComponent__With_Single_Custom_Paramer___Request(DefaultTenantBuilder.Multi_WitnSingle_With_1Hour__Resolution);
//        await UserAuthenticationOperation();




//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        await UserLogoutOperation();
//        Verification(response.Result.Calculated);
//    }



//    #endregion



//    #region SingleParameter_With_Multi_Parameter_0_BaseParameters_Component

//    private async Task SingleParameter_With_Multi_Parameter_0_BaseParameters_Component()
//    {
//        var request = SingleParameter_With_Multi_Parameter_0_BaseParameters_Componentt___Request(DefaultTenantBuilder.Single_Parameter_With_Multi_0_BaseParameters);
//        await UserAuthenticationOperation();


//        //request.StartDate =DateTime.Now.AddDays(-30) ;
//        //request.EndDate = DateTime.Now;


//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(1);


//        await UserLogoutOperation();
//        //Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest SingleParameter_With_Multi_Parameter_0_BaseParameters_Componentt___Request(int customParameterId)
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
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);

//        //dto.Parameters.First().CustomParameterIds = [206,206];
//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;


//        //var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        //var options = new JsonSerializerOptions { WriteIndented = true };
//        //var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        //return dto;

//    }

//    #endregion


//    #region Single Component with Inner Multi 

//    private async Task SingleComponent__With_2222_Multi_Custom_Paramer_Component()
//    {
//        var request = SingleComponent__With_2222_Multi_Custom_Paramer_Component___Request(DefaultTenantBuilder.Single_Parameter_With_Multi_And_Multi_CustomParameter);
//        await UserAuthenticationOperation();


//        //request.StartDate =DateTime.Now.AddDays(-30) ;
//        //request.EndDate = DateTime.Now;


//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(1);


//        await UserLogoutOperation();
//        Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest SingleComponent__With_2222_Multi_Custom_Paramer_Component___Request(int customParameterId)
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
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);

//        //dto.Parameters.First().CustomParameterIds = [206,206];
//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;


//        //var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        //var options = new JsonSerializerOptions { WriteIndented = true };
//        //var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        //return dto;

//    }

//    #endregion



//    #region Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring_Override_10Min

//    private async Task Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring_Override_10Min()
//    {
//        //xxxxxxxxxxxxxxxx
//        var request = Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring_Override_10Min___Request(DefaultTenantBuilder.Multi_Parameter_With_Single_CustomParameter_id);
//        await UserAuthenticationOperation();


//        //var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        await UserLogoutOperation();

//        Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring_Override_10Min___Request(int customParameterId)
//    {
//        var json = """
//        {
//            "resolution": "IS1HOUR",
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T09:07:00.0Z",
//            "parameters": [
//                {
//                    "applyToDos": [
//                        {
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;
//        //"CustomParameterIds": "[203]"


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().CustomParameterIds = [203];
//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;


//        //var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        //var options = new JsonSerializerOptions { WriteIndented = true };
//        //var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        //return dto;

//    }

//    #endregion

//    #region MultiComponent_HeighertResolution_With_Single_Custom_Paramer_Component

//    private async Task MultiComponent_HeighertResolution_With_Single_Custom_Paramer_Component()
//    {
//        //xxxxxxxxxxxxxxxx
//        var request = MultiComponent__With_Single_Custom_Paramer___Request(DefaultTenantBuilder.Multi_Parameter_With_Single_CustomParameter);
//        await UserAuthenticationOperation();




//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(2);


//        await UserLogoutOperation();
//    }

//    private TrendCalcRequest MultiComponent__With_Single_Custom_Paramer___Request(int customParameterId)
//    {
//        var json = """
//        {
//            "resolution": "IS1HOUR",
//            "startDate": "2024-08-17T05:07:00.0Z",
//            "endDate": "2024-08-17T09:07:00.0Z",
//            "parameters": [
//                {
//                    "applyToDos": [
//                        {
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;
//        //"CustomParameterIds": "[203]"


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().CustomParameterIds = [203];
//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;


//        //var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        //var options = new JsonSerializerOptions { WriteIndented = true };
//        //var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        //return dto;

//    }

//    #endregion

//    #region Single Component with Inner Multi 

//    private async Task SingleComponent__With_Multi_Custom_Paramer_Component()
//    {
//        var request = SingleComponent__With_Multi_Custom_Paramer___Request(DefaultTenantBuilder.Single_Parameter_With_Multi_CustomParameter);
//        await UserAuthenticationOperation();


//        //request.StartDate =DateTime.Now.AddDays(-30) ;
//        //request.EndDate = DateTime.Now;


//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(1);


//        await UserLogoutOperation();

//        Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest SingleComponent__With_Multi_Custom_Paramer___Request(int customParameterId)
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
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//                    "Data": "{\"CustomerId\": 12345}",
//                }
//            ]
//        }
//        """;


//        var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);

//        //dto.Parameters.First().CustomParameterIds = [207];

//        dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;


//        //var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json);
//        //dto.Parameters.First().Data = customParameterId.ToString().ToString();

//        //var options = new JsonSerializerOptions { WriteIndented = true };
//        //var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        //return dto;

//    }

//    #endregion


//    #region Multi Component Multi Parameter with Inner Single CustomParameter

//    private async Task MultiComponent__With_Multi_Custom_Paramer_Component()
//    {
//        var request = MultiComponent__With_Multi_Custom_Paramer__Request(DefaultTenantBuilder.Multi_Parameter_With_Multi_CustomParameter);
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
//        //        var strInt = item.Value.ToString().Split('.')[0];

//        //        var pop = stack.Pop();
//        //        pop.ToString().Should().Be(strInt);
//        //    }

//        //}

//        await UserLogoutOperation();
//        Verification(response.Result.Calculated);

//    }

//    private TrendCalcRequest MultiComponent__With_Multi_Custom_Paramer__Request(int customParameterId)
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
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//        //dto.Parameters.First().CustomParameterIds = [206];

//        dto.Parameters.First().Data = customParameterId.ToString();

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }

//    #endregion













//    //#region Single Component Multi Parameter with Inner Single BaseParameter

//    //private async Task InnerBaseParameter_SingleBaseParameter_Component()
//    //{
//    //    var request = GetSingleParameter_Inner_baseParameter(DefaultTenantBuilder.Single_ParameterCustomerId);
//    //    await UserAuthenticationOperation();


//    //    //request.StartDate =DateTime.Now.AddDays(-30) ;
//    //    //request.EndDate = DateTime.Now;


//    //    var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//    //    response.Result.Calculated.Data.Count().Should().Be(1);

//    //    var stack = new Stack<int>();
//    //    stack.Push(338);
//    //    stack.Push(339);
//    //    stack.Push(338);

//    //    foreach (var set in response.Result.Calculated.Data)
//    //    {
//    //        foreach (var item in set.Data)
//    //        {
//    //            var strInt = item.Value.ToString().Split('.')[0];

//    //            var pop = stack.Pop();
//    //            pop.ToString().Should().Be(strInt);
//    //        }

//    //    }

//    //    await UserLogoutOperation();

//    //}

//    //private TrendCalcRequest GetSingleParameter_Inner_baseParameter(int customParameterId)
//    //{
//    //    var json2 = """

//    //                    {
//    //          "resolution": "IS1HOUR",
//    //          "startDate": "2024-8-15T05:07:00.0Z",
//    //          "endDate": "2024-08-15T08:07:00.0Z",
//    //          "parameters": [
//    //            {
//    //              "feeders": [
//    //                {
//    //                  "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//    //                  "id": 1,
//    //                  "name": "Feeder_0"
//    //                },
//    //              {
//    //              "parent": "a059db13-2390-432a-a062-6ac41f213612",
//    //              "id": 1,
//    //              "name": "Feeder_0"
//    //            }
//    //              ],
//    //              "Type": "CustomParameter",
//    //              "Quantity":"avg",
//    //              "Data": "{\"CustomerId\": 12345}",
//    //              "InternaParameterType":2,
//    //              "InternaParameters": "[{\"type\":\"LOGICAL\",\"name\":\"bp\",\"feeder\":null,\"group\":\"RMS\",\"phase\":\"UV2N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"base\":\"BHCYC\",\"quantity\":null,\"resolution\":null,\"operator\":null,\"aggregationFunction\":null}]"

//    //            }
//    //          ]
//    //        }


//    //        """;


//    //    var dto = JsonConvert.DeserializeObject<TrendCalcRequest>(json2);
//    //    dto.Parameters.First().Data = customParameterId.ToString().ToString();

//    //    var options = new JsonSerializerOptions { WriteIndented = true };
//    //    var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//    //    return dto;

//    //}

//    //#endregion




//    private void Verification(CalculationDto calculationDto)
//    {
//        foreach (var axises in calculationDto.Data)
//        {
//            double? value = 0;
//            foreach (var item in axises.Data)
//            {
//                value += item.Value;
//            }

//            value.Should().NotBe(0);
//        }
//    }






//    #region Single Max And Percentile
//    private async Task Single_Max_Percentile_Component()
//    {
//        var request = GetSingleParameterMultiComponentRequest(DefaultTenantBuilder.Single_Auto_And_Percentile_ParameterCustomerId);
//        request.Resolution = "auto(200)";

//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostTrendCalculationIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(1);


//        await UserLogoutOperation();


//        //foreach (var axises in response.Result.Calculated.Data)
//        //{
//        //    double value = 0;
//        //    foreach (var item in axises.Data)
//        //    {
//        //        value += item.Value;
//        //    }

//        //    value.Should().NotBe(0);
//        //}

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
//        Verification(response.Result.Calculated);
//    }


//    #endregion

//    #region Single Parameter

//    private async Task SingleParameterMultiComponenr()
//    {
//        var request = GetSingleParameterMultiComponentRequest(DefaultTenantBuilder.Single_ParameterCustomerId);
//        await UserAuthenticationOperation();


//        //request.StartDate =DateTime.Now.AddDays(-30) ;
//        //request.EndDate = DateTime.Now;


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
//                var strInt = item.Value.ToString().Split('.')[0];

//                var pop = stack.Pop();
//                pop.ToString().Should().Be(strInt);
//            }

//        }

//        await UserLogoutOperation();

//    }

//    private TrendCalcRequest GetSingleParameterMultiComponentRequest(int customParameterId)
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


//        await UserLogoutOperation();
//        Verification(response.Result.Calculated);
//    }

//    private TrendCalcRequest GetMultiParameterRequest()
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
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
//        response.Result.Calculated.Data.Count().Should().Be(4);


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
//        Verification(response.Result.Calculated);
//    }


//    private TrendCalcRequest GetBPCPRequest()
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
//                },
//              {
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
//                  "data": "{\"type\":\"LOGICAL\",\"name\":\"bp\",\"feeder\":null,\"group\":\"RMS\",\"phase\":\"UV2N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"base\":\"BHCYC\",\"quantity\":null,\"resolution\":null,\"operator\":null,\"aggregationFunction\":null}"
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

//    private TrendCalcRequest GetArithmeticRequest()
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
//                            "componentId": "08c3912f-0275-4278-bf86-917168d88eef",
//                            "componentName": "xxx"
//                        },
//                        {
//                            "componentId": "a059db13-2390-432a-a062-6ac41f213612",
//                            "componentName": "R&D LAB - 14"
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
