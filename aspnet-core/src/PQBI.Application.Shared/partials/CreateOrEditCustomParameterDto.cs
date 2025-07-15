using Abp.Runtime.Validation;
using Newtonsoft.Json;
using PQBI.CalculationEngine;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Functions.Aggregation;
using PQBI.PQS.CalcEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PQBI.CustomParameters.Dtos
{
    public partial class CreateOrEditCustomParameterDto : ICustomValidate
    {
        public void AddValidationErrors(CustomValidationContext context)
        {
            if (ResolutionInSeconds <0)
            {
                context.Results.Add(new ValidationResult($"{nameof(ResolutionInSeconds)} - Cannot be negetive"));
                return;
            }

            //if (string.IsNullOrEmpty(Quantity))
            //{
            //    context.Results.Add(new ValidationResult($"{nameof(Quantity)} - Cannot be empty"));
            //    return;
            //}

            if (string.IsNullOrEmpty(Type))
            {
                context.Results.Add(new ValidationResult($"{nameof(Type)} - Cannot be empty"));
                return;
            }


            if (string.IsNullOrEmpty(InnerCustomParameters) == false)
            {
                var innerCustomParameters = JsonConvert.DeserializeObject<InnerCustomParameter[]>(InnerCustomParameters);

                if (string.IsNullOrEmpty(CustomBaseDataList) == true && innerCustomParameters.Length == 0)
                {
                    context.Results.Add(new ValidationResult($"Either InnerCustomParameters or STDPQSParametersList must be provided and cannot both be empty."));
                    return;
                }
            }



            //if (Enum.TryParse(Type, true, out TrendWidgetParameterType columnParameterType) == false)
            //{
            //    //context.Results.Add(new ValidationResult($"{nameof(Type)} can be party of {nameof(TrendWidgetParameterType)}."));
            //    //return;
            //}

            if (Enum.TryParse(Type, true, out CustomParameterType paramType))
            {
                if (paramType == CustomParameterType.SPMC)
                {
                    if (AggregationFunction.StartsWith(ArithmeticCalcFunction.Arithmetic_Function))
                    {
                        context.Results.Add(new ValidationResult($"In single parameter not arithmetics is allowed."));
                        return;
                    }
                }
            }

            var aggregation = AggregationFunction.ToLower();
            if (aggregation.StartsWith(ArithmeticCalcFunction.Arithmetic_Function))
            {
                if (aggregation.Contains("/"))
                {
                    context.Results.Add(new ValidationResult($"Arithmetic currently doesnt support devision."));
                    return;
                }
            }

            var functions = IFunctionEngine.GetAllMathematicalFunction().Select(x=>x.Alias).ToHashSet();
            //var aggregationFunctions = IFunctionEngine.GetAllAggregationFunctions().Select(x=>x.Alias).ToHashSet();
            var parameterList = JsonConvert.DeserializeObject<BaseParameter[]>(CustomBaseDataList);

            //var (mainResolutionNum, mainResolutionUnit) = GroupByCalcFunction.ParsePeriod(Resolution);
            var mainRarameterResolution = ResolutionInSeconds;// GroupByCalcFunction.ConvertToSecond(mainResolutionNum, mainResolutionUnit);
            var names = new HashSet<string>();
            foreach (var parameter in parameterList)
            {
                if (string.IsNullOrEmpty(parameter.Name))
                {
                    context.Results.Add(new ValidationResult($"{nameof(BaseParameter.Name)} - Cannot be empty."));
                    return;
                }

                if (string.IsNullOrEmpty(parameter.Operator) == false)
                {
                    if (functions.Any(f => parameter.Operator.ToLower().StartsWith(f)) == false)
                    {
                        context.Results.Add(new ValidationResult($"{nameof(BaseParameter.Operator)} - Alias not valid."));
                        return;
                    }
                }

                //if (names.Add(parameter.Name) == false)
                //{
                //    context.Results.Add(new ValidationResult($"Duplication in Base Parameter {parameter.Name}"));
                //}

                //if (parameter.Resolution< 0 )
                ////if (parameter.Resolution< 0 || string.IsNullOrEmpty(parameter.Id))
                //{
                //    var serialized = JsonConvert.SerializeObject(parameter);
                //    context.Results.Add(new ValidationResult($"The parameter {serialized} is not valid"));
                //    return;
                //}

                ////var (resolutionNum, resolutionUnit) = GroupByCalcFunction.ParsePeriod(parameter.Resolution);
                ////var parameterResolution = GroupByCalcFunction.ConvertToSecond(resolutionNum, resolutionUnit);

                //if (parameter.Resolution > mainRarameterResolution)
                //{
                //    context.Results.Add(new ValidationResult($"{nameof(ResolutionInSeconds)} - Cannot be less the parameter resolution [{parameter.Resolution}]"));
                //    return;
                //}


                Enum.TryParse(parameter.Type, true, out ParameterListItemType baseParameterType);

                //switch (columnParameterType)
                //{
                //    case TrendWidgetParameterType.CustomParameter:
                //        break;

                //    case TrendWidgetParameterType.BaseParameter:
                //        break;

                //    case TrendWidgetParameterType.Exception:

                //        if (parameter.FromComponents is null)
                //        {
                //            context.Results.Add(new ValidationResult($"{nameof(parameter.FromComponents)} - Cannot be empty."));
                //            return;
                //        }

                //        if (parameter.FromComponents.Key == Guid.Empty)
                //        {
                //            context.Results.Add(new ValidationResult($"{nameof(parameter.FromComponents.Key)} - Cannot be empty."));
                //            return;
                //        }

                //        if (baseParameterType == ParameterListItemType.Logical)
                //        {
                //            if (string.IsNullOrEmpty(parameter.FromComponents.Tag))
                //            {
                //                context.Results.Add(new ValidationResult($"{nameof(parameter.FromComponents.Tag)} - Cannot be empty."));
                //                return;
                //            }
                //        }

                //        break;

                //    default:
                //        break;
                //}
            }
        }
    }
}
