using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PQBI.Network.RestApi;


public class LoggerStopwatch : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _label;
    private readonly Stopwatch _stopwatch;


    public LoggerStopwatch(ILogger logger, string label)
    {
        _logger = logger;
        _label = label;
        _stopwatch = Stopwatch.StartNew();

        _logger.LogInformation($"{_label} started");

    }

    public LoggerStopwatch CreateSubLogger(string subLabel)
    {
        subLabel = subLabel.Replace(" ", string.Empty);
        return new LoggerStopwatch(_logger, $"{_label} ==> {subLabel}");
    }

    public TimeSpan Elasped => _stopwatch.Elapsed;

    public LoggerStopwatch CreateSubLoggerPerCalculation()
    {
        return CreateSubLogger("Per_Calculation");
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation($"{_label} {message} [{_stopwatch.Elapsed}]");
    }

    public void LogError(string message)
    {
        _logger.LogError($"{_label} {message} [{_stopwatch.Elapsed}]");
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.LogInformation($"{_label} stopped [{_stopwatch.Elapsed}]");
    }
}


public static class PqbiStopwatch
{
    private static readonly AsyncLocal<LoggerStopwatch?> _currentStopwatch = new();

    public static LoggerStopwatch AnchorAsync(string mainLabel, ILogger logger)
    {
        if (_currentStopwatch.Value is not null)
        {
            var current = _currentStopwatch.Value;
            var subLogger = current.CreateSubLogger(mainLabel);

            _currentStopwatch.Value = subLogger;

            return subLogger;
        }

        var stopwatch = new LoggerStopwatch(logger, mainLabel);
        _currentStopwatch.Value = stopwatch;
        return stopwatch;
    }

    public static LoggerStopwatch Anchor(string mainLabel)
    {
        if (_currentStopwatch.Value is not null)
        {
            var current = _currentStopwatch.Value;
            var subLogger = current.CreateSubLogger(mainLabel);
            return subLogger;
        }

        throw new Exception("No Anchor");
    }
}