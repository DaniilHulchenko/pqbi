using PQS.Data.Events.Enums;
using PQS.Data.Events.Filters;
using PQS.Data.Events;
using PQS.Data.RecordsContainer;
using PQZTimeFormat;
using PQS.Data.RecordsContainer.Records;
using PQBI.PQS;

namespace PQBI.Requests;

public class PQSGetEventRequest : PQSCommonRequest
{
    private readonly string[] _componentIds;
    private readonly PQZDateTime _start;
    private readonly PQZDateTime _end;
    private readonly IEnumerable<EventClass> _events;

    public PQSGetEventRequest(string session, PQZDateTime start, PQZDateTime end, IEnumerable<EventClass> events, params string[] componentsIds)
         : base(session)
    {
        _componentIds = componentsIds;
        _start = start;
        _end = end;
        _events = events;
        AddConfigurations();
    }

    protected override void AddConfigurations()
    {
        var container = new FiltersGroupContainer();
        var filter = new ClassFilter();

        foreach (var @event in _events)
        {
            filter.AddSingleValue(@event);
        }
        //filter.AddSingleValue(EventClass.EVENT_CLASSIFICATION_DIP);
        //filter.AddSingleValue(EventClass.EVENT_CLASSIFICATION_SWELL);
        //filter.AddSingleValue(EventClass.EVENT_CLASSIFICATION_FLICKERING);
        //filter.AddSingleValue(EventClass.EVENT_CLASSIFICATION_VARIATION);


        var group = new FiltersGroup();
        group.AddFilter(filter);

        container.AddFilterGroup(group);

        foreach (var componentId in _componentIds)
        {
            GetEventsRecord getRec = new GetEventsRecord(Guid.Parse(componentId), _start.TicksPQZTimeFormat, _end.TicksPQZTimeFormat, EventRequestTypeEnum.DETAILED_EVENT_STRUCTURE, 1000000, LimitTypeEnum.TIME_ASC, SegmentationTypeEnum.None, container);
            AddRecord(getRec);
        }
    }
}

public class PQSAddEventResponse : PQSOperationResponseBase<PQSGetEventRequest>
{
    public PQSAddEventResponse(PQSGetEventRequest request, PQSResponse response) : base(request, response)
    {

    }


    public IEnumerable<PQSEventDto[]> Events
    {
        get
        {
            var events = new List<PQSEventDto[]>();
            ExtractAllRecords(out EventsRecord[] eventRecords, out _);
            foreach (var evntRes in eventRecords)
            {
                var list = new List<PQSEventDto>();
                var eventsContainer = evntRes.GetEventsContainer();
                foreach (var pqEv in eventsContainer)
                {
                    if (pqEv is PQEvent @event)
                    {
                        //var tmp = @event.CurrentPhases;
                        //tmp.NamePhases

                        var phases = @event.VoltagePhases.NamePhases?.Select(x=>x.ToString()).ToList() ?? new List<string>();

                        var start = @event.StartTime.DateTime;
                        var duration = @event.Duration.Milliseconds;
                        //var dto = new PQSEventDto((int)@event.Class, @event.Value);
                        var dto = new PQSEventDto((int)@event.Class, @event.Value, @event.Deviation, start, duration, phases,@event.Feeders.ToList());
                        list.Add(dto);
                    }
                }

                if (list.Any())
                {
                    events.Add(list.ToArray());
                }
            }

            return events.ToArray();
        }
    }
}
