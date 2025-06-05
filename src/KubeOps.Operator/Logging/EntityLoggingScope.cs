using System.Collections;

using k8s;
using k8s.Models;

namespace KubeOps.Operator.Logging;

#pragma warning disable CA1710
internal sealed record EntityLoggingScope : IReadOnlyCollection<KeyValuePair<string, object>>
#pragma warning restore CA1710
{
    private EntityLoggingScope(IReadOnlyDictionary<string, object> state)
    {
        Values = state;
    }

    public int Count => Values.Count;

    private string? CachedFormattedString { get; set; }

    private IReadOnlyDictionary<string, object> Values { get; }

    public static EntityLoggingScope CreateFor<TEntity>(WatchEventType eventType, TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => new(
            new Dictionary<string, object>
            {
                { "EventType", eventType },
                { nameof(entity.Kind), entity.Kind },
                { "Namespace", entity.Namespace() },
                { "Name", entity.Name() },
                { "ResourceVersion", entity.ResourceVersion() },
            });

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        => Values.GetEnumerator();

    public override string ToString()
        => CachedFormattedString ??= $"{{ {string.Join(", ", Values.Select(kvp => $"{kvp.Key} = {kvp.Value}"))} }}";

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
