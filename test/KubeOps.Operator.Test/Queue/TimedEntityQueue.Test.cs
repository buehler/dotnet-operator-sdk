﻿using k8s.Models;

using KubeOps.Operator.Queue;

namespace KubeOps.Operator.Test.Queue;

public class TimedEntityQueueTest
{
    [Fact]
    public async Task Can_Enqueue_Multiple_Entities_With_Same_Name()
    {
        var queue = new TimedEntityQueue<V1Secret>();

        queue.Enqueue(CreateSecret("app-ns1", "secret-name"), TimeSpan.FromSeconds(1));
        queue.Enqueue(CreateSecret("app-ns2", "secret-name"), TimeSpan.FromSeconds(1));

        var items = new List<V1Secret>();

        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(TimeSpan.FromSeconds(2));

        var enumerator = queue.GetAsyncEnumerator(tokenSource.Token);

        try
        {
            while (await enumerator.MoveNextAsync())
            {
                items.Add(enumerator.Current);
            }
        }
        catch (OperationCanceledException)
        {
            // We expect to timeout watching the queue so that we can assert the items received
        }

        Assert.Equal(2, items.Count);
    }

    private V1Secret CreateSecret(string secretNamespace, string secretName)
    {
        var secret = new V1Secret();
        secret.EnsureMetadata();

        secret.Metadata.SetNamespace(secretNamespace);
        secret.Metadata.Name = secretName;

        return secret;
    }
}
