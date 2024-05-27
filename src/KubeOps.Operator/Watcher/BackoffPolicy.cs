namespace KubeOps.Operator.Watcher;

/// <summary>
/// Simple exponential backoff logic.
/// </summary>
public class BackoffPolicy(CancellationToken stoppingToken, Func<int, TimeSpan> policy)
{
    private int _retries = 0;

    /// <summary>
    /// Default exponential backoff algorithm
    /// </summary>
    public static Func<int, TimeSpan> ExponentialWithJitter(int maxExp = 5, int jitterMillis = 1000)
        => retries => TimeSpan.FromSeconds(Math.Pow(2, Math.Clamp(retries, 0, maxExp)))
            .Add(TimeSpan.FromMilliseconds(new Random().Next(0, jitterMillis)));

    /// <summary>
    /// Clear all counters.
    /// </summary>
    public void Clear()
    {
        _retries = 0;
    }

    /// <summary>
    /// Adds a delay.
    /// </summary>
    /// <param name="ex"><see cref="Exception"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    public async Task WaitOnException(Exception ex)
    {
        try
        {
            _retries++;
            await Task.Delay(WaitTime(), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Do nothing
        }
    }

    private TimeSpan WaitTime()
        => policy(_retries);
}
