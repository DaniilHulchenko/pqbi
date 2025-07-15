using PQBI.Infrastructure;
using PQBI.Infrastructure.Lockers;
using System.Linq;

namespace PQBI.Trace;



public interface ILogWatcherService
{
    void AddLog(string log);
    string[] Peek(int lastAmoutLog);

}

public class LogWatcherConfig:PQSConfig<LogWatcherConfig>
{
    public int Capacity { get; set; } 
}


public class LogWatcherService : ILogWatcherService
{
    private ArrayLockerSlim _logs;

    public LogWatcherService(LogWatcherConfig config)
    {
        _logs = new ArrayLockerSlim(config.Capacity);
    }

    public void AddLog(string log)
    {
        _logs.Add(log);
    }

    public string[] Peek(int lastAmoutLog)
    {
        var result =  _logs.Peek(lastAmoutLog).Where(x => x != null).ToArray();

        return result;
    }
}
