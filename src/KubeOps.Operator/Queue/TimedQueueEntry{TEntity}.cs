using System.Collections.Concurrent;

namespace KubeOps.Operator.Queue;

internal sealed record TimedQueueEntry<TEntity> : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly TimeSpan _requeueIn;
    private readonly TEntity _entity;

    public TimedQueueEntry(TEntity entity, TimeSpan requeueIn)
    {
        _requeueIn = requeueIn;
        _entity = entity;
    }

    /// <summary>
    /// A <see cref="CancellationToken"/> that is triggered after calling <see cref="Cancel"/>.
    /// </summary>
    public CancellationToken Token => _cts.Token;

    public void Dispose() => _cts.Dispose();

    /// <summary>
    /// Cancels the execution of <see cref="AddAfterDelay"/> and disposes any associated resources.
    /// </summary>
    public void Cancel()
    {
        _cts.Cancel();
        Dispose();
    }

    /// <summary>
    /// Adds the entity to <paramref name="collection"/> after <see cref="_requeueIn"/>.
    /// Can be canceled with <see cref="Cancel"/>.
    /// </summary>
    /// <param name="collection">The collection to add the entry to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddAfterDelay(BlockingCollection<TEntity> collection)
    {
        try
        {
            await Task.Delay(_requeueIn, _cts.Token);
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            collection.TryAdd(_entity);
        }
        catch (TaskCanceledException)
        {
            // Ignore canceled tasks
        }
        catch (ObjectDisposedException)
        {
            // And also if the object is disposed.
        }
    }
}
