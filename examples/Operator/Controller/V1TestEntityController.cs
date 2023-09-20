﻿using KubeOps.Abstractions.Controller;

using Microsoft.Extensions.Logging;

using Operator.Entities;

namespace Operator.Controller;

public class V1TestEntityController : IEntityController<V1TestEntity>
{
    private readonly ILogger<V1TestEntityController> _logger;

    public V1TestEntityController(ILogger<V1TestEntityController> logger)
    {
        _logger = logger;
    }

    public async Task ReconcileAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Reconciling entity {EntityName}.", entity.Metadata.Name);
    }

    public async Task DeletedAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Deleting entity {EntityName}.", entity.Metadata.Name);
    }
}
