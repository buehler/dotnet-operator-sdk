using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using k8s.Models;
using k8s.Versioning;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Operator.Services;

namespace KubeOps.Operator.Util
{
    internal class CrdBuilder : ICrdBuilder
    {
        private readonly IEnumerable<EntityType> _entityTypes;

        public CrdBuilder(IEnumerable<EntityType> entityTypes)
        {
            _entityTypes = entityTypes;
        }

        public IEnumerable<V1CustomResourceDefinition> BuildCrds()
        {
            var resourceTypes = _entityTypes
                .Select(type => type.InstanceType)
                .Distinct()
                .Where(type => type.GetCustomAttributes<KubernetesEntityAttribute>().Any());

            return resourceTypes
                .Where(type => !type.GetCustomAttributes<IgnoreEntityAttribute>().Any())
                .Select(type => (type.CreateCrd(), type.GetCustomAttributes<StorageVersionAttribute>().Any()))
                .GroupBy(grp => grp.Item1.Metadata.Name)
                .Select(
                    group =>
                    {
                        if (group.Count(def => def.Item2) > 1)
                        {
                            throw new Exception("There are multiple stored versions on an entity.");
                        }

                        var crd = group.First().Item1;
                        crd.Spec.Versions = group
                            .SelectMany(
                                c => c.Item1.Spec.Versions.Select(
                                    v =>
                                    {
                                        v.Served = true;
                                        v.Storage = c.Item2;
                                        return v;
                                    }))
                            .OrderByDescending(v => v.Name, KubernetesVersionComparer.Instance)
                            .ToList();

                        // when only one version exists, or when no StorageVersion attributes are found
                        // the first version in the list is the stored one.
                        if (crd.Spec.Versions.Count == 1 || group.Count(def => def.Item2) == 0)
                        {
                            crd.Spec.Versions[0].Storage = true;
                        }

                        return crd;
                    });
        }
    }
}
