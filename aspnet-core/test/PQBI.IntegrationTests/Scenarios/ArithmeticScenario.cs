//using FluentAssertions;
//using Newtonsoft.Json;
//using PQBI.IntegrationTests.Requests;
//using PQBI.Migrations.Seed.Tenants;
//using PQBI.PQS.CalcEngine;
//using PQBI.Web.Controllers;
//using System.Text.Json;

//namespace PQBI.IntegrationTests.Scenarios;

//internal class ArithmeticScenario : WidgetScenario
//{
//    public ArithmeticScenario(string baseAuthenticationUrl) : base(baseAuthenticationUrl)
//    {
//    }


//    protected async override Task RunScenario()
//    {
//        await SingleParameterMultiArithmeticsComponenr();
//    }

//    private async Task SingleParameterMultiArithmeticsComponenr()
//    {
//        var request = GetSingleArithmeticParameterMultiComponentRequest();
//        await UserAuthenticationOperation();

//        var response = await RunPostCommand<PQSCalculationRequestTest, ResponseTestWrapper<PQSCalculationResponse>>(PostWidgetRequestIntegrationTestUrl, new PQSCalculationRequestTest(UserName, UserPassword, request));
//        response.Result.Calculated.Data.Count().Should().Be(1);

//        var stack = new Stack<int>();
//        stack.Push(465);
//        stack.Push(467);
//        stack.Push(465);

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


//    private CalcRequestDto GetSingleArithmeticParameterMultiComponentRequest()
//    {
//        var json = """
//{
//  "functions": [
//    //{
//    //  "name": "ABSOLUTE",
//    //  "arg": null
//    //},
//    {
//      "name": "AVG",
//      "arg": null
//    }
//    //,
//    //{
//    //  "name": "DIVIDE",
//    //  "arg": 1
//    //}
//  ],
//  "applyTo": [
//    {
//      "guid": "08c3912f-0275-4278-bf86-917168d88eef",
//      "name": "Igor LAB - 10"
//    },
//    //{
//    //  "guid": "a059db13-2390-432a-a062-6ac41f213612",
//    //  "name": "R&D LAB - 14"
//    //}
//  ],
//  "customParametersIds": [1,2],
//  "feeders": [
//    {
//"data":{
//      "parent": "08c3912f-0275-4278-bf86-917168d88eef",
//      "id": 1,
//      "name": "Feeder_0"
//    }    ,
//"key": "08c3912f-0275-4278-bf86-917168d88eef",
//    "label": "Main"
//}
////    {
////"data":{
////      "parent": "a059db13-2390-432a-a062-6ac41f213612",
////      "id": 1,
////      "name": "Feeder_0"
////    },
////"key": "a059db13-2390-432a-a062-6ac41f213612",
////    "label": "Main"
////},
//  ],
//  "resolution": "IS1HOUR",
//  "startDate": "2024-8-15T05:07:00.0Z",
//  "endDate": "2024-08-15T08:07:00.0Z"
//}
//""";


//        var dto = JsonConvert.DeserializeObject<CalcRequestDto>(json);
//        dto.CustomParameterIds = new List<int> { DefaultTenantBuilder.MultiParameterMC_Arithmetic_CustomerParameterId };

//        var options = new JsonSerializerOptions { WriteIndented = true };
//        var txt = System.Text.Json.JsonSerializer.Serialize(dto, options);
//        return dto;

//    }



//}
