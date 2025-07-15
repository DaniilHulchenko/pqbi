using System.Collections.Generic;
using System;
using Abp.Runtime.Validation;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using PQBI.Infrastructure.Extensions;
using System.Linq;

namespace PQBI.PQS.CalcEngine;

public class WidgetValidationBase
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool ValidationErrors(CustomValidationContext context)
    {
        if (StartDate >= EndDate)
        {
            context.Results.Add(new ValidationResult($"{nameof(WidgetValidationBase.StartDate)} <  {nameof(WidgetValidationBase.EndDate)}"));
            return false;
        }

        return true;

    }
}

public class ParameterListDto
{
    public List<FeederComponentInfo> Feeders { get; set; } = new List<FeederComponentInfo>();
    public string Type { get; set; } //CustomParameter{CustomerId}/BaseParameter{SingleBaseParameter}/Exception{CustomerId}
    public string Quantity { get; set; }
    public string Data { get; set; } //CustomerId{numeric}/json
}


public class TrendParameter
{
    public List<FeederComponentInfo> Feeders { get; set; } = new List<FeederComponentInfo>();
    public string Type { get; set; }
    //public string Quantity { get; set; }

    [JsonProperty("custom_data")]
    public TrendCustomWidgetData CustomData { get; set; }

    [JsonProperty("base_data")]
    public BaseData BaseData { get; set; }
    //public TrendBaseData BaseData { get; set; }
}

public class BaseData
{
    [JsonProperty("base_type")]
    public string Type { get; set; } // Logical/Channel/Exception
    public string Group { get; set; }
    public FeederComponentInfo FromComponents { get; set; }
    public string Phase { get; set; }
    public HarmonicsDto Harmonics { get; set; }
    [JsonProperty("base_resolution")]
    public string BaseResolution { get; set; }
    public string Quantity { get; set; }

}

public static class BaseDataExtensions
{
    public static BaseParameter ToBaseParameter(this BaseData trendBaseData)
    {
        if (trendBaseData == null)
        {
            throw new ArgumentNullException(nameof(BaseData));
        }

        return new BaseParameter
        {
            Type = trendBaseData.Type, // assuming Base_Type maps to Type
            Group = trendBaseData.Group,
            Phase = trendBaseData.Phase,
            Harmonics = trendBaseData.Harmonics != null
                ? new HarmonicsDto { Value = trendBaseData.Harmonics.Value }
                : null,
            BaseResolution = trendBaseData.BaseResolution,
            Quantity = trendBaseData.Quantity,
            //Id = trendBaseData.Id,
            //Name = trendBaseData.Name,

            AggregationFunction = string.Empty,
            Operator = string.Empty,
            Resolution = 1,
            FromComponents = null
        };
    }
}

public class TrendCustomWidgetData
{
    public int Id { get; set; }
    public bool IgnoreAlignment { get; set; }
    public string Quantity { get; set; }
}

public class Harmonics
{
    public int? Index { get; set; }
}

public class TrendCalcRequest : WidgetValidationBase, ICustomValidate
{
    public bool IsAutoResolution { get; set; }
    public int ResolutionInSeconds { get; set; }
    //public string Resolution { get; set; }
    public string WidgetName { get; set; }
    public int UserTimeZone { get; set; }
    public List<TrendParameter> Parameters { get; set; } = new List<TrendParameter>();


    public void AddValidationErrors(CustomValidationContext context)
    {
        if (ValidationErrors(context) == false)
        {
            return;
        }

        //if (string.IsNullOrEmpty(Resolution))
        //{
        //    context.Results.Add(new ValidationResult($"{nameof(Resolution)} - Cannot be empty"));
        //    return;
        //}

        if (Parameters is null || Parameters.Count == 0)
        {
            context.Results.Add(new ValidationResult($"{nameof(Parameters)} - Cannot be empty"));
            return;
        }


        foreach (var param in Parameters)
        {
            if (Enum.TryParse(param.Type, true, out TrendWidgetParameterType columnParameterType))
            {
                switch (columnParameterType)
                {
                    case TrendWidgetParameterType.CustomParameter:

                        if (string.IsNullOrEmpty(param.CustomData.Quantity))
                        {
                            context.Results.Add(new ValidationResult($"{nameof(TrendCustomWidgetData.Quantity)} - Cannot be empty"));
                            return;
                        }
                      
                        if (param.Feeders.IsCollectionEmpty())
                        {
                            context.Results.Add(new ValidationResult($"Both feeders and Channels - Cannot be empty."));
                            return;
                        }

                        break;

                    case TrendWidgetParameterType.BaseParameter:
                        if (param.Feeders.IsCollectionEmpty())
                        //if ((param.ApplyToDos is null || param.ApplyToDos.Count == 0) && (param.Feeders is null || param.Feeders.Count == 0))
                        {
                            context.Results.Add(new ValidationResult($"Both feeders and Channels - Cannot be empty."));
                            return;
                        }

                        try
                        {

                            var baseParameter = param.BaseData.ToBaseParameter();
                            //var baseParameter = param.BaseData.ToBaseParameter();
                            //var baseParameter = JsonConvert.DeserializeObject<BaseParameter>(param.Data) ?? throw new Exception();
                            //if (string.IsNullOrEmpty(baseParameter.Name))
                            //{
                            //    context.Results.Add(new ValidationResult($"BaseParameter name - Cannot be empty."));
                            //    return;
                            //}

                            if (Enum.TryParse(baseParameter.Type, true, out ParameterListItemType businessType))
                            {
                                switch (businessType)
                                {
                                    case ParameterListItemType.Logical:
                                        if (param.Feeders.Count <= 0)
                                        {
                                            context.Results.Add(new ValidationResult($"In BaseParameter (Logical) mode feeders must be."));
                                            return;
                                        }
                                        break;

                                    case ParameterListItemType.Channel:

                                        //if (param.Feeders.FirstOrDefault(x => x.Id is null) != null)
                                        ////if (param.ApplyToDos.Count <= 0)
                                        //{
                                        //    context.Results.Add(new ValidationResult($"In BaseParameter (Channel) mode channels must be."));
                                        //    return;
                                        //}
                                        break;

                                    case ParameterListItemType.Exception:
                                        break;
                                    default:
                                        break;
                                }

                            }
                        }
                        catch
                        {
                            context.Results.Add(new ValidationResult($"{nameof(ParameterListDto.Data)} should be base parameter."));
                            return;
                        }
                        break;

                    case TrendWidgetParameterType.Exception:
                        if (param.Feeders.IsCollectionExists())
                        //if ((param.Feeders is not null && param.Feeders.Count > 0) || (param.ApplyToDos is not null && param.ApplyToDos.Count > 0))
                        {
                            context.Results.Add(new ValidationResult($"Both feeders and Channels in exception mode should be empty."));
                            return;
                        }
                        break;
                    default:
                        break;
                }

            }
            else
            {
                context.Results.Add(new ValidationResult($"Type cann be only part of {nameof(ParameterListDto.Data)}"));
                return;
            }
        }
    }
}