using System;
using System.Collections.Generic;

namespace Dos.Operator.Rbac
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
}
