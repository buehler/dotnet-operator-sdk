﻿using System.Runtime.CompilerServices;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Watcher;

using Microsoft.Extensions.Logging;

using Moq;

namespace KubeOps.Operator.Test.Watcher;

public sealed class ResourceWatcherTest
{
    [Fact]
    public async Task Restarting_Watcher_Should_Trigger_New_Watch()
    {
        // Arrange.
        var logger = Mock.Of<ILogger<ResourceWatcher<V1Pod>>>();
        var serviceProvider = Mock.Of<IServiceProvider>();
        var timedEntityQueue = new TimedEntityQueue<V1Pod>();
        var operatorSettings = new OperatorSettings() { Namespace = "unit-test" };
        var kubernetesClient = Mock.Of<IKubernetesClient>();

        Mock.Get(kubernetesClient)
            .Setup(client => client.WatchAsync<V1Pod>("unit-test", null, null, true, It.IsAny<CancellationToken>()))
            .Returns<string?, string?, string?, bool?, CancellationToken>((_, _, _, _, cancellationToken) => WaitForCancellationAsync<(WatchEventType, V1Pod)>(cancellationToken));

        var resourceWatcher = new ResourceWatcher<V1Pod>(logger, serviceProvider, timedEntityQueue, operatorSettings, kubernetesClient);

        // Act.
        // Start and stop the watcher.
        await resourceWatcher.StartAsync(CancellationToken.None);
        await resourceWatcher.StopAsync(CancellationToken.None);

        // Restart the watcher.
        await resourceWatcher.StartAsync(CancellationToken.None);

        // Assert.
        Mock.Get(kubernetesClient)
            .Verify(client => client.WatchAsync<V1Pod>("unit-test", null, null, true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static async IAsyncEnumerable<T> WaitForCancellationAsync<T>([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        yield return default!;
    }
}
