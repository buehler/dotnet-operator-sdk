using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace KubeOps.Operator.Web.Test.TestApp;

[KubernetesEntity(Group = "integration.test", ApiVersion = "v1", Kind = "IntegrationTest")]
public class V1IntegrationTestEntity : CustomKubernetesEntity<V1IntegrationTestEntity.EntitySpec,
    V1IntegrationTestEntity.EntityStatus>
{
    public V1IntegrationTestEntity()
    {
        ApiVersion = "integration.test/v1";
        Kind = "IntegrationTest";
    }

    public V1IntegrationTestEntity(string name, string username, string ns) : this()
    {
        Metadata.Name = name;
        Spec.Username = username;
        Metadata.NamespaceProperty = ns;
    }

    public override string ToString() => $"Test Entity ({Metadata.Name}): {Spec.Username}";

    public class EntitySpec
    {
        public string Username { get; set; } = string.Empty;
    }

    public class EntityStatus
    {
        public string Status { get; set; } = string.Empty;
    }
}
