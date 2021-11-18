using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace UnitTests
{
    public sealed class ConsoleLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var str = $"{DateTime.Now} : {state.ToString()}";
            Debug.WriteLine(str);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}