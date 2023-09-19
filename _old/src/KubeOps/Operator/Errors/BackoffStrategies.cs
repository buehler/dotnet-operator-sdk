namespace KubeOps.Operator.Errors;

/// <summary>
/// Delegate that describes a backoff strategy on errors for the operator.
/// </summary>
/// <param name="retryCount">The retry count for a particular object.</param>
/// <returns>A <see cref="TimeSpan"/> that is waited upon before retrying.</returns>
public delegate TimeSpan BackoffStrategy(int retryCount);

/// <summary>
/// Class that holds <see cref="BackoffStrategy"/> that are used in
/// defaults.
/// </summary>
public static class BackoffStrategies
{
    private static readonly Random Rnd = new();

    /// <summary>
    /// Default <see cref="BackoffStrategy"/> for the operator. It is used
    /// in conjunction with the max retries settings to determine the backoff
    /// with a random component for the retry timer.
    /// It takes the exponential calculation of the retry count (2^x) and
    /// adds a random number of milliseconds (0-1000) to it.
    /// </summary>
    public static BackoffStrategy ExponentialBackoffStrategy => retryCount => TimeSpan
        .FromSeconds(Math.Pow(2, retryCount))
        .Add(TimeSpan.FromMilliseconds(Rnd.Next(0, 1000)));
}
