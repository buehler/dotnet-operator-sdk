using System;

namespace KubeOps.Operator.Logging
{
    internal class LoggingNullScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
