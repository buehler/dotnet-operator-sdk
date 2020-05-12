using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KubeOps.Operator.Logging
{
    internal class StructuredConsoleLogger : ILogger
    {
        private const string OriginalFormat = "{OriginalFormat}";
        private readonly string _category;
        private readonly JsonSerializerSettings _jsonSettings;

        public StructuredConsoleLogger(string category, JsonSerializerSettings jsonSettings)
        {
            _category = category;
            _jsonSettings = jsonSettings;
        }

        public IDisposable BeginScope<TState>(TState state) => new LoggingNullScope();

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = new LogMessage
            {
                Category = _category,
                Message = formatter(state, exception),
                Timestamp = DateTime.UtcNow,
            };

            if (state is IReadOnlyList<KeyValuePair<string, object>> parameters)
            {
                message.Parameters = new Dictionary<string, object>();
                foreach (var (key, value) in parameters.Where(pair => pair.Key != OriginalFormat))
                {
                    message.Parameters.Add(key, value);
                }
            }

            if (exception != null)
            {
                message.ExceptionMessage = exception.Message;
            }

            Console.WriteLine(JsonConvert.SerializeObject(message, _jsonSettings));
        }

        private struct LogMessage
        {
            public string Category { get; set; }

            public DateTime Timestamp { get; set; }

            public string Message { get; set; }

            public string ExceptionMessage { get; set; }

            public IDictionary<string, object> Parameters { get; set; }
        }
    }
}
