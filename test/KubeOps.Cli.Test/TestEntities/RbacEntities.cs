using k8s.Models;

using KubeOps.Abstractions.Rbac;

namespace KubeOps.Cli.Test.TestEntities;

[KubernetesEntity(Group = "test", ApiVersion = "v1")]
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Delete)]
public class RbacTest1 : Base
{
}

[KubernetesEntity(Group = "test", ApiVersion = "v1")]
[EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.All)]
[EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
public class RbacTest2 : Base
{
}

[KubernetesEntity(Group = "test", ApiVersion = "v1")]
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
[EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
public class RbacTest3 : Base
{
}

[KubernetesEntity(Group = "test", ApiVersion = "v1")]
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
[EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
[EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
[EntityRbac(typeof(RbacTest3), Verbs = RbacVerb.Delete)]
[EntityRbac(typeof(RbacTest4), Verbs = RbacVerb.Get | RbacVerb.Update)]
public class RbacTest4 : Base
{
}
