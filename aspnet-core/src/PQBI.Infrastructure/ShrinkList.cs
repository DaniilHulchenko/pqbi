using System.Security.Principal;

namespace PQBI.Infrastructure;

public interface IShrinkBusiness
{
    DateTime StartTime { get; }
    int DurationMilliSeconds { get; }

    //string Id { get; }
    string EventId { get; }
    public string Phase { get;  }
    public string ComponentId { get;  }
    public string Feeder { get;  } // _seperation of list
}

public class EventComponent : IShrinkBusiness
{
    public DateTime StartTime { get; init; }

    public int DurationMilliSeconds { get; init; }

    public string EventId { get; init; }
    public string Phase { get; init; }
    public string ComponentId { get; init; }
    public string Feeder { get; init; }
    public double Daviation { get; init; }
    public double? Value { get; init; }
}


public class ShrinkList<T> where T : IShrinkBusiness
{
    public class ShrinkExecuter
    {
        public IEnumerable<T> Do(IEnumerable<T> list, int milliSeconds = 0)
        {
            var items = list.ToList();
            if (items.Count <= 1)
            {
                return items;
            }

            var buffer = new List<T>();
            var next = items[1];

            var isNextLast = false;

            for (int index = 0; index < items.Count - 1;)
            {
                var current = items[index];
                next = items[index + 1];

                var totalDuration = current.DurationMilliSeconds + milliSeconds;

                var currentEnds = current.StartTime.AddMilliseconds(totalDuration);
                if (currentEnds < next.StartTime)
                {
                    buffer.Add(current);
                    index++;
                }
                else
                {
                    buffer.Add(current);
                    index += 2;
                    isNextLast = index >= items.Count;
                }
            }

            if (isNextLast == false)
            {
                buffer.Add(items[items.Count - 1]);
            }

            return buffer;

        }
    }

    public IDictionary<string, List<T>> Shrink(IEnumerable<T> list, 
        Func<T,string> keyGenerator , 
        int milliSeconds = 0)
    {
        var phases = new Dictionary<string, List<T>>();

        foreach (var item in list)
        {
            var name = keyGenerator(item);//  item.EventId ?? string.Empty;
            if (phases.TryGetValue(name, out List<T> phasesList))
            {
                phasesList.Add(item);
            }
            else
            {
                phases[name] = new List<T> { item };
            }
        }

        var result = new Dictionary<string, List<T>>();
        foreach (var keyAndValue in phases)
        {
            var executer = new ShrinkExecuter();
            var buffer = executer.Do(keyAndValue.Value, milliSeconds);
            result[keyAndValue.Key] = buffer.ToList();
        }


        return result;
    }
}