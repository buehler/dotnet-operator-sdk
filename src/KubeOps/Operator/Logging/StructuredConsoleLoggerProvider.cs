using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KubeOps.Operator.Logging
{
    internal class StructuredConsoleLoggerProvider : ILoggerProvider
    {
        private readonly JsonSerializerSettings _jsonSettings;

        private readonly IDictionary<string, StructuredConsoleLogger> _loggers =
            new ConcurrentDictionary<string, StructuredConsoleLogger>();

        public StructuredConsoleLoggerProvider(JsonSerializerSettings jsonSettings)
        {
            _jsonSettings = jsonSettings;
            _jsonSettings.NullValueHandling = NullValueHandling.Ignore;
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (!_loggers.ContainsKey(categoryName))
            {
                _loggers[categoryName] = new StructuredConsoleLogger(categoryName, _jsonSettings);
            }

            return _loggers[categoryName];
        }
    }
}
