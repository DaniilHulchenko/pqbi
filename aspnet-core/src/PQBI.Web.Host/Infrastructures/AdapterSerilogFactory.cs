using System;
using Castle.Core.Logging;
using Serilog;
using Serilog.Events;

namespace PQBI.Web.Infrastructures
{
    public class AdapterSerilogFactory : AbstractLoggerFactory
    {
        private readonly Serilog.ILogger logger;


        public AdapterSerilogFactory()
        {
            logger = Log.Logger;
        }

        public AdapterSerilogFactory(Serilog.ILogger logger)
        {
            this.logger = logger;
        }

        public override Castle.Core.Logging.ILogger Create(string name)
        {
            return new AdapterSerilogLogger(logger.ForContext("SourceContext", name), this);
        }

        public override Castle.Core.Logging.ILogger Create(string name, LoggerLevel level)
        {
            throw new NotSupportedException("Logger levels cannot be set at runtime. Please see Serilog's LoggerConfiguration.MinimumLevel.");
        }
    }


    public class AdapterSerilogLogger : Castle.Core.Logging.ILogger
    {
        protected internal Serilog.ILogger Logger { get; set; }

        protected internal AdapterSerilogFactory Factory { get; set; }

        public bool IsTraceEnabled => Logger.IsEnabled(LogEventLevel.Verbose);

        public bool IsDebugEnabled => Logger.IsEnabled(LogEventLevel.Debug);

        public bool IsErrorEnabled => Logger.IsEnabled(LogEventLevel.Error);

        public bool IsFatalEnabled => Logger.IsEnabled(LogEventLevel.Fatal);

        public bool IsInfoEnabled => Logger.IsEnabled(LogEventLevel.Information);

        public bool IsWarnEnabled => Logger.IsEnabled(LogEventLevel.Warning);

        public AdapterSerilogLogger(Serilog.ILogger logger, AdapterSerilogFactory factory)
        {
            Logger = logger;
            Factory = factory;
        }

        internal AdapterSerilogLogger()
        {
        }

        public override string ToString()
        {
            return Logger.ToString();
        }

        public Castle.Core.Logging.ILogger CreateChildLogger(string loggerName)
        {
            throw new NotImplementedException("Creating child loggers for Serilog is not supported");
        }

        public void Trace(string message, Exception exception)
        {
            Logger.Verbose(exception, message);
        }

        public void Trace(Func<string> messageFactory)
        {
            if (IsTraceEnabled)
            {
                Logger.Verbose(messageFactory());
            }
        }

        public void Trace(string message)
        {
            if (IsTraceEnabled)
            {
                Logger.Verbose(message);
            }
        }

        public void TraceFormat(Exception exception, IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsTraceEnabled)
            {
                Logger.Verbose(exception, string.Format(formatProvider, format, args));
            }
        }

        public void TraceFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsTraceEnabled)
            {
                Logger.Verbose(string.Format(formatProvider, format, args));
            }
        }

        public void TraceFormat(Exception exception, string format, params object[] args)
        {
            if (IsTraceEnabled)
            {
                Logger.Verbose(exception, format, args);
            }
        }

        public void TraceFormat(string format, params object[] args)
        {
            if (IsTraceEnabled)
            {
                Logger.Verbose(format, args);
            }
        }

        public void Debug(string message, Exception exception)
        {
            if (IsDebugEnabled)
            {
                Logger.Debug(exception, message);
            }
        }

        public void Debug(Func<string> messageFactory)
        {
            if (IsDebugEnabled)
            {
                Logger.Debug(messageFactory());
            }
        }

        public void Debug(string message)
        {
            if (IsDebugEnabled)
            {
                Logger.Debug(message);
            }
        }

        public void DebugFormat(Exception exception, IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsDebugEnabled)
            {
                Logger.Debug(exception, string.Format(formatProvider, format, args));
            }
        }

        public void DebugFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsDebugEnabled)
            {
                Logger.Debug(string.Format(formatProvider, format, args));
            }
        }

        public void DebugFormat(Exception exception, string format, params object[] args)
        {
            if (IsDebugEnabled)
            {
                Logger.Debug(exception, format, args);
            }
        }

        public void DebugFormat(string format, params object[] args)
        {
            if (IsDebugEnabled)
            {
                Logger.Debug(format, args);
            }
        }

        public void Error(string message, Exception exception)
        {
            if (IsErrorEnabled)
            {
                Logger.Error(exception, message);
            }
        }

        public void Error(Func<string> messageFactory)
        {
            if (IsErrorEnabled)
            {
                Logger.Error(messageFactory());
            }
        }

        public void Error(string message)
        {
            if (IsErrorEnabled)
            {
                Logger.Error(message);
            }
        }

        public void ErrorFormat(Exception exception, IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Error(exception, string.Format(formatProvider, format, args));
            }
        }

        public void ErrorFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Error(string.Format(formatProvider, format, args));
            }
        }

        public void ErrorFormat(Exception exception, string format, params object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Error(exception, format, args);
            }
        }

        public void ErrorFormat(string format, params object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Error(format, args);
            }
        }

        public void Fatal(string message, Exception exception)
        {
            if (IsFatalEnabled)
            {
                Logger.Fatal(exception, message);
            }
        }

        public void Fatal(Func<string> messageFactory)
        {
            if (IsFatalEnabled)
            {
                Logger.Fatal(messageFactory());
            }
        }

        public void Fatal(string message)
        {
            if (IsFatalEnabled)
            {
                Logger.Fatal(message);
            }
        }

        public void FatalFormat(Exception exception, IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Fatal(exception, string.Format(formatProvider, format, args));
            }
        }

        public void FatalFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Fatal(string.Format(formatProvider, format, args));
            }
        }

        public void FatalFormat(Exception exception, string format, params object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Fatal(exception, format, args);
            }
        }

        public void FatalFormat(string format, params object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Fatal(format, args);
            }
        }

        public void Info(string message, Exception exception)
        {
            if (IsInfoEnabled)
            {
                Logger.Information(exception, message);
            }
        }

        public void Info(Func<string> messageFactory)
        {
            if (IsInfoEnabled)
            {
                Logger.Information(messageFactory());
            }
        }

        public void Info(string message)
        {
            if (IsInfoEnabled)
            {
                Logger.Information(message);
            }
        }

        public void InfoFormat(Exception exception, IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Information(exception, string.Format(formatProvider, format, args));
            }
        }

        public void InfoFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Information(string.Format(formatProvider, format, args));
            }
        }

        public void InfoFormat(Exception exception, string format, params object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Information(exception, format, args);
            }
        }

        public void InfoFormat(string format, params object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Information(format, args);
            }
        }

        public void Warn(string message, Exception exception)
        {
            if (IsWarnEnabled)
            {
                Logger.Warning(exception, message);
            }
        }

        public void Warn(Func<string> messageFactory)
        {
            if (IsWarnEnabled)
            {
                Logger.Warning(messageFactory());
            }
        }

        public void Warn(string message)
        {
            if (IsWarnEnabled)
            {
                Logger.Warning(message);
            }
        }

        public void WarnFormat(Exception exception, IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Warning(exception, string.Format(formatProvider, format, args));
            }
        }

        public void WarnFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Warning(string.Format(formatProvider, format, args));
            }
        }

        public void WarnFormat(Exception exception, string format, params object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Warning(exception, format, args);
            }
        }

        public void WarnFormat(string format, params object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Warning(format, args);
            }
        }
    }
}
