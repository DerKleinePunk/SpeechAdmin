using System;
using Microsoft.Extensions.Logging;

namespace SpeechAdmin.Services
{
    /// <summary>
    /// Null logger implementation for when no logger is available
    /// </summary>
    internal class NullLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Do nothing
        }

        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
                // Do nothing
            }
        }
    }
}
