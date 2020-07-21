using System;
using System.Collections.Generic;

namespace KubeOps.Operator.Rbac
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class EntityRbacAttribute : Attribute
    {
        public EntityRbacAttribute(params Type[] entities)
        {
            Entities = entities;
        }

        public IEnumerable<Type> Entities { get; set; }

        public RbacVerb Verbs { get; set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class RbacRoleAttribute : Attribute
    {
        public RbacRoleAttribute(string prefix) => Prefix = prefix;

        public string Prefix { get; }
    }
}
