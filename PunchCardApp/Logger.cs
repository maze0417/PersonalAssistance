using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PunchCardApp
{
    public interface ILoggerReader
    {
        string GetLoggedMessage();
    }

    public class Logger : ILogger, ILoggerReader, IDisposable
    {
        private readonly StringBuilder _logMessage = new StringBuilder();

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logMessage.AppendLine($"{DateTime.Now} : {formatter(state, exception)}");
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return this;
        }

        string ILoggerReader.GetLoggedMessage()
        {
            return _logMessage.ToString();
        }

        public void Dispose()
        {
            _logMessage.Clear();
        }
    }
}