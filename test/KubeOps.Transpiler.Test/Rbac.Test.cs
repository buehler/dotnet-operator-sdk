﻿using FluentAssertions;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Rbac;

namespace KubeOps.Transpiler.Test;

public class RbacTest : TranspilerTestBase
{
    public RbacTest(MlcProvider provider) : base(provider)
    {
    }

    [Fact]
    public void Should_Create_Generic_Policy()
    {
        var role = _mlc
            .Transpile(_mlc.GetContextType<GenericRbacTest>().GetCustomAttributesData<GenericRbacAttribute>()).ToList()
            .First();
        role.ApiGroups.Should().Contain("group");
        role.Resources.Should().Contain("configmaps");
        role.NonResourceURLs.Should().Contain("url");
        role.NonResourceURLs.Should().Contain("foobar");
        role.Verbs.Should().Contain(new[] { "get", "delete" });
    }

    [Fact]
    public void Should_Calculate_Max_Verbs_For_Types()
    {
        var role = _mlc
            .Transpile(_mlc.GetContextType<RbacTest1>().GetCustomAttributesData<EntityRbacAttribute>()).ToList()
            .First();
        role.Resources.Should().Contain("rbactest1s");
        role.Verbs.Should().Contain(new[] { "get", "update", "delete" });
    }

    [Fact]
    public void Should_Correctly_Calculate_All_Verb()
    {
        var role = _mlc
            .Transpile(_mlc.GetContextType<RbacTest2>().GetCustomAttributesData<EntityRbacAttribute>()).ToList()
            .First();
        role.Resources.Should().Contain("rbactest2s");
        role.Verbs.Should().Contain("*").And.HaveCount(1);
    }

    [Fact]
    public void Should_Group_Same_Types_Together()
    {
        var roles = _mlc
            .Transpile(_mlc.GetContextType<RbacTest3>().GetCustomAttributesData<EntityRbacAttribute>()).ToList();
        roles.Should()
            .Contain(
                rule => rule.Resources.Contains("rbactest1s"));
        roles.Should()
            .Contain(
                rule => rule.Resources.Contains("rbactest2s"));
        roles.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Group_Types_With_Same_Verbs_Together()
    {
        var roles = _mlc
            .Transpile(_mlc.GetContextType<RbacTest4>().GetCustomAttributesData<EntityRbacAttribute>()).ToList();
        roles.Should()
            .Contain(
                rule => rule.Resources.Contains("rbactest1s") &&
                        rule.Resources.Contains("rbactest4s") &&
                        rule.Verbs.Contains("get") &&
                        rule.Verbs.Contains("update"));
        roles.Should()
            .Contain(
                rule => rule.Resources.Contains("rbactest2s") &&
                        rule.Resources.Contains("rbactest3s") &&
                        rule.Verbs.Contains("delete"));
        roles.Should().HaveCount(2);
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Delete)]
    public class RbacTest1 : CustomKubernetesEntity
    {
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
    public class RbacTest2 : CustomKubernetesEntity
    {
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
    public class RbacTest3 : CustomKubernetesEntity
    {
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
    [EntityRbac(typeof(RbacTest3), Verbs = RbacVerb.Delete)]
    [EntityRbac(typeof(RbacTest4), Verbs = RbacVerb.Get | RbacVerb.Update)]
    public class RbacTest4 : CustomKubernetesEntity
    {
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [GenericRbac(Urls = new[] { "url", "foobar" }, Resources = new[] { "configmaps" }, Groups = new[] { "group" },
        Verbs = RbacVerb.Delete | RbacVerb.Get)]
    public class GenericRbacTest : CustomKubernetesEntity
    {
    }
}
