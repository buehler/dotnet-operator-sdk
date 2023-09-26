using System.Reflection;

using FluentAssertions;

using KubeOps.Abstractions.Rbac;
using KubeOps.Transpiler.Test.TestEntities;

namespace KubeOps.Transpiler.Test;

public class RbacTest
{
    [Fact]
    public void Should_Calculate_Max_Verbs_For_Types()
    {
        var role = Rbac.Transpile(typeof(RbacTest1).GetCustomAttributes<EntityRbacAttribute>()).First();
        role.Resources.Should().Contain("rbactest1s");
        role.Verbs.Should().Contain(new[] { "get", "update", "delete" });
    }

    [Fact]
    public void Should_Correctly_Calculate_All_Verb()
    {
        var role = Rbac.Transpile(typeof(RbacTest2).GetCustomAttributes<EntityRbacAttribute>()).First();
        role.Resources.Should().Contain("rbactest2s");
        role.Verbs.Should().Contain("*").And.HaveCount(1);
    }

    [Fact]
    public void Should_Group_Same_Types_Together()
    {
        var roles = Rbac.Transpile(typeof(RbacTest3).GetCustomAttributes<EntityRbacAttribute>()).ToList();
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
        var roles = Rbac.Transpile(typeof(RbacTest4).GetCustomAttributes<EntityRbacAttribute>()).ToList();
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
}
