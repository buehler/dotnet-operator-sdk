using System;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace KubeOps.Operator.Errors
{
    internal class ExponentialBackoffHandler : IDisposable
    {
        private const double MaxRetrySeconds = 64;
        private readonly Action? _retryHandler;
        private readonly Func<Task>? _asyncRetryHandler;
        private readonly Random _rnd = new Random();

        private Timer? _retryTimer;
        private Timer? _resetTimer;

        private int _tryCount = -1;

        public event EventHandler? RetryHandler;

        public ExponentialBackoffHandler()
        {
        }

        public ExponentialBackoffHandler(Action retryHandler)
        {
            _retryHandler = retryHandler;
        }

        public ExponentialBackoffHandler(Func<Task> asyncRetryHandler)
        {
            _asyncRetryHandler = asyncRetryHandler;
        }

        public TimeSpan Retry(TimeSpan? resetTimer = null)
        {
            DisposeTimer(_resetTimer);
            if (resetTimer != null)
            {
                TimedReset(resetTimer.Value);
            }

            var span = ExponentialBackoff(Interlocked.Increment(ref _tryCount));
            DisposeTimer(_retryTimer);
            _retryTimer = new Timer(span.TotalMilliseconds);
            _retryTimer.Elapsed += (_, __) =>
            {
                RetryHandler?.Invoke(this, EventArgs.Empty);
                _retryHandler?.Invoke();
                _asyncRetryHandler?.Invoke();
                DisposeTimer(_retryTimer);
            };
            _retryTimer.Start();

            return span;
        }

        public void Reset()
        {
            DisposeTimer(_resetTimer);
            DisposeTimer(_retryTimer);
            Interlocked.Exchange(ref _tryCount, -1);
        }

        public void TimedReset(TimeSpan resetTimer)
        {
            _resetTimer = new Timer(resetTimer.TotalMilliseconds);
            _resetTimer.Elapsed += (_, __) =>
            {
                Interlocked.Exchange(ref _tryCount, -1);
                DisposeTimer(_resetTimer);
            };
            _resetTimer.Start();
        }

        public void Dispose()
        {
            Reset();
            foreach (var handler in RetryHandler?.GetInvocationList() ?? new Delegate[] { })
            {
                RetryHandler -= (EventHandler) handler;
            }
        }

        private static void DisposeTimer(Timer? timer)
        {
            timer?.Stop();
            timer?.Dispose();
        }

        private TimeSpan ExponentialBackoff(int retryCount) => TimeSpan
            .FromSeconds(Math.Min(Math.Pow(2, retryCount), MaxRetrySeconds))
            .Add(TimeSpan.FromMilliseconds(_rnd.Next(0, 1000)));
    }
}
