//using Abp.UI;
//using Newtonsoft.Json;
//using PQBI.CalculationEngine.Functions;
//using PQBI.PQS.CalcEngine;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace PQBI.Network.RestApi.EngineCalculation
//{
//    public class CustomParameterWidgetTableRequestBatch
//    {
//        public int CustomParameterId { get; set; }
//        public IEnumerable<FeederComponentInfo> Feeders { get; set; }
//    }

//    public static class WidgetTableHelper
//    {
//        public static CustomParameterWidgetTableRequestBatch CreatOrderedCustomParameterWidgetTableRequesth(TableWidgetRequest222 request)
//        {
//            foreach (var columnWidgetTable in request.ColumnWidgetTables)
//            {
//                TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(columnWidgetTable.ParameterType);

//                if (widgetTableType  == TableWidgetParameterType.CustomParameter)
//                {

//                }

//                switch (widgetTableType)
//                {

//                    case TableWidgetParameterType.CustomParameter:


//                        ////group = await CreateTableNode(url, session, input, parameter);
//                        ////var graphes = await _engineControllerService.CalculateCustomParameterAsync(group);
//                        //var graphes = await CreateTableNode2222Async(url, session, input, parameter);
//                        //responseItems.AddRange(ArrangingForTable(graphes, parameter));

//                        break;

//                    case TableWidgetParameterType.BaseParameter:

//                        //var singleBaseParameter = JsonConvert.DeserializeObject<BaseParameter>(parameter.Data);
//                        //singleBaseParameter.Quantity = parameter.Quantity;

//                        //int totalSeconds = (int)(input.EndDate - input.StartDate).TotalSeconds;
//                        //singleBaseParameter.Resolution = $"ISX{totalSeconds}";




//                        //group = new CustomParameterNodeCalculator(CustomParameterType.BPCP, GroupByCalcFunction.Single_Resolution.ToString(), string.Empty, input.StartDate, input.EndDate, GroupByCalcFunction.Single_Resolution.ToString(), parameter.Quantity);

//                        //var parameterComponents2 = singleBaseParameter.CreateBaseParameterComponents(input.Components, input.Feeders);
//                        //group.Populate(parameterComponents2);
//                        //paramComponents.AddRange(parameterComponents2);

//                        //SelectAssemble(group, input.Feeders, input.Components);

//                        //await SendingAndStoreingDataAsync(url, session, input.StartDate, input.EndDate, (false, null), paramComponents);
//                        //var graphes2 = _engineControllerService.CalculateBaseParameter222(group);

//                        //responseItems.AddRange(ArrangingForTable(graphes2, parameter));

//                        break;

//                    case TableWidgetParameterType.Event:
//                        //In Event only count can be Nadav H.

//                        //var tmp = await WidgetTableEventCalculation(url, session, input, input.StartDate, input.EndDate, parameter);
//                        //responseItems.AddRange(tmp);

//                        break;


//                    case TableWidgetParameterType.Exception:

//                    default:
//                        break;
//                        throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
//                }
//            }

//            var result = new CustomParameterWidgetTableRequestBatch();

            

//            return null;
//        }


//        public static IEnumerable<IEnumerable<FeederComponentInfo>> CreatOrderedCOrderBatchh(TableWidgetRequest222 tableWidgetRequest222)
//        {
//            var result = new List<IEnumerable<FeederComponentInfo>>();
//            var rows = tableWidgetRequest222.Rows;

//            foreach (var tag in rows.Tags)
//            {
//                var buffer = new List<FeederComponentInfo>();   
//                foreach (var feeder in tag.Feeders)
//                {
//                    buffer.Add(feeder);
//                }

//                result.Add(buffer);
//            }


//            return result;
//        }
//    }
//}
