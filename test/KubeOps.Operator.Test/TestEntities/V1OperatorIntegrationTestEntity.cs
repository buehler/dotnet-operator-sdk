﻿using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace KubeOps.Operator.Test.TestEntities;

[KubernetesEntity(Group = "operator.test", ApiVersion = "v1", Kind = "OperatorIntegrationTest")]
public class V1OperatorIntegrationTestEntity : CustomKubernetesEntity<V1OperatorIntegrationTestEntity.EntitySpec,
    V1OperatorIntegrationTestEntity.EntityStatus>
{
    public V1OperatorIntegrationTestEntity()
    {
        ApiVersion = "operator.test/v1";
        Kind = "OperatorIntegrationTest";
    }

    public V1OperatorIntegrationTestEntity(string name, string username, string ns) : this()
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
