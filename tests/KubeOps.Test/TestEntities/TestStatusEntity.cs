using System;
using System.Collections.Generic;
using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Test.TestEntities
{
    public class TestStatusEntitySpec
    {
        public string SpecString { get; set; } = string.Empty;
    }

    public class TestStatusEntityStatus
    {
        public string StatusString { get; set; } = string.Empty;
        public List<ComplexStatusObject> StatusList { get; set; }
    }

    public class ComplexStatusObject
    {
        public string ObjectName { get; set; }
        public DateTime LastModified { get; set; }
    }

    [KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
    public class TestStatusEntity : CustomKubernetesEntity<TestStatusEntitySpec, TestStatusEntityStatus>
    {
    }
}
