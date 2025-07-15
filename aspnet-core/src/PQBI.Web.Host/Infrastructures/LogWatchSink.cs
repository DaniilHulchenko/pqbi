using Serilog.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using PQBI.Trace;
using PQBI.Logs;

namespace PQBI.Web.Infrastructures
{
    public class LogWatchSink : ILogEventSink
    {

        private readonly IFormatProvider _formatProvider;
        private readonly ILogWatcherService _pQSLogWatcher;

        public LogWatchSink(ILogWatcherService pQSLogWatcher)
        {
            _formatProvider = null;
            _pQSLogWatcher = pQSLogWatcher;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.MessageTemplate.Text.StartsWith(TenantLogBase.PQS_Subject_Name))
            {
                _pQSLogWatcher.AddLog(logEvent.MessageTemplate.Text);
            }
        }
    }


    public static class CustomSinkExtensions
    {
        public static LoggerConfiguration CustomSink(this LoggerSinkConfiguration loggerConfiguration, ILogWatcherService watcher)
        {
            return loggerConfiguration.Sink(new LogWatchSink(watcher));
        }
    }
}
