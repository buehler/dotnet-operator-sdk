using System;

namespace KubeOps.Operator.Rbac
{
    [Flags]
    public enum RbacVerb
    {
        None = 0,
        All = 1 << 0,
        Get = 1 << 1,
        List = 1 << 2,
        Watch = 1 << 3,
        Create = 1 << 4,
        Update = 1 << 5,
        Patch = 1 << 6,
        Delete = 1 << 7,
    }
}
