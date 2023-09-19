﻿using k8s;
using k8s.Models;

namespace Operator.Entities;

[KubernetesEntity]
public class V1TestEntity : IKubernetesObject<V1ObjectMeta>
{
    public string ApiVersion { get; set; }
    public string Kind { get; set; }
    public V1ObjectMeta Metadata { get; set; }
}