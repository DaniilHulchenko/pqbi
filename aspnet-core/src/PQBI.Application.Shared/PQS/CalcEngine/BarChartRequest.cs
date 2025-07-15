using Abp.Runtime.Validation;
using PQBI.Infrastructure.Sapphire;
using PQS.Data.Events.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQBI.PQS.CalcEngine
{

    public class BarChartRequest : ICustomValidate
    {
        //public BarChartConfig Config { get; set; }
        public List<RequestBarChartComponent> Components { get; set; }
        public List<BarChartEventRequest> Events { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public void AddValidationErrors(CustomValidationContext context)
        {
            if (StartDate >= EndDate)
            {
                context.Results.Add(new ValidationResult($"{nameof(TableWidgetParameter.Data)} - {nameof(BarChartRequest.StartDate)} <  {nameof(BarChartRequest.EndDate)}"));

            }

            if (Events is null || Events.Count == 0)
            {
                context.Results.Add(new ValidationResult($"{nameof(RequestBarChartComponent)}.{nameof(BarChartRequest.Events)} - Cannot be empty"));
                return;
            }

            if (Components is null || Components.Count == 0)
            {
                context.Results.Add(new ValidationResult($"{nameof(RequestBarChartComponent)}.{nameof(BarChartRequest.Components)} - Cannot be empty"));
                return;
            }

            foreach (var component in Components)
            {
                if (string.IsNullOrEmpty(component.Name))
                {
                    context.Results.Add(new ValidationResult($"{nameof(RequestBarChartComponent)}.{nameof(RequestBarChartComponent.Name)} - Cannot be empty"));
                }

                if (string.IsNullOrEmpty(component.Guid))
                {
                    context.Results.Add(new ValidationResult($"{nameof(RequestBarChartComponent)}.{nameof(RequestBarChartComponent.Guid)} - Cannot be empty"));
                }
            }

            //var sapphireEvents = EventFactory.GetAllEventInfos().Select(x => (ushort)x.EventClass).ToHashSet();

            //foreach (var @event in Events)
            //{

            //    var eventClassId = (ushort)@event.EventClass;
            //    if (sapphireEvents.Contains(eventClassId) == false)
            //    {
            //        context.Results.Add(new ValidationResult($"{nameof(RequestBarChartComponent)}.{nameof(BarChartEventRequest)}.{nameof(BarChartEventRequest.EventClass)} - Should be part of the {nameof(EventClass)} "));
            //        return;
            //    }
            //}
        }
    }
    public class RequestBarChartComponent
    {
        public string Guid { get; set; }
        public string Name { get; set; }
    }


    public class BarChartEventBase
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Header { get; set; }
        public string AggregationFunc { get; set; }
        public int EventClass { get; set; }

    }

    public class BarChartEventRequest : BarChartEventBase
    {
        //public int EventClass { get; set; }

    }

    public class BarChartEventResponse : BarChartEventBase
    {
        public double Data { get; set; }

    }

    //    public record BarChartEvent(
    //        string Type,
    //        string Name,
    //        string Header,
    //        string AggregationFunc,

    //        double EventClass
    //);

    public class BarCharComponentResponse
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public List<int> Feeders { get; set; }
        public List<BarChartEventResponse> Events { get; set; } = new List<BarChartEventResponse>();
    }

    public class BarChartResponse
    {
        //public BarChartConfig Config { get; set; }
        public List<BarCharComponentResponse> Components { get; set; }
        //public List<BarCharEvent> Events { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

}
