using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Logging;

using UserConfigOperator.Entities;

namespace UserConfigOperator.Controller;

[EntityRbac(typeof(V1UserConfig), typeof(V1ConfigMap), Verbs = RbacVerb.All)]
public class V1UserConfigController : IEntityController<V1UserConfig>
{
    private readonly ILogger<V1UserConfigController> _logger;
    private readonly IKubernetesClient<V1ConfigMap> _client;

    public V1UserConfigController(
        ILogger<V1UserConfigController> logger,
        IKubernetesClient<V1ConfigMap> client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task ReconcileAsync(V1UserConfig entity)
    {
        var config = await _client.GetAsync(entity.Name());
        if (config is not null)
        {
            _logger.LogInformation("Config with name {name} already exists.", entity.Name());
            return;
        }

        var configMap = new V1ConfigMap(metadata: new(name: entity.Name(), namespaceProperty: entity.Namespace()))
            .Initialize();
        configMap.AddOwnerReference(new(
            entity.ApiVersion,
            entity.Kind,
            entity.Metadata.Name,
            entity.Metadata.Uid));
        await _client.CreateAsync(configMap);
        _logger.LogInformation("Config with name {name} created.", entity.Name());
    }
}
