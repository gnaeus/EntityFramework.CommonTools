using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
{
    public class TestLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new DebugLogger();
        }

        public void Dispose() { }

        private class DebugLogger : ILogger
        {
            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Debug.WriteLine(formatter(state, exception));
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }

        public static LoggerFactory MakeLoggerFactory()
        {
            var factory = new LoggerFactory();
            factory.AddProvider(new TestLoggerProvider());
            return factory;
        }
    }
}
