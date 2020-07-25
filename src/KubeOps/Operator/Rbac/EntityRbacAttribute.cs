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



    /// <summary>
    /// This attribute controls the operator's role name, primarily for the generation of kubernetes yamls
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class OperatorSpecAttribute : Attribute
    {
        public OperatorSpecAttribute(string name) => Name = name;

        public string Name { get; }

        /// <summary>
        /// For custom container registries, like something.azurecr.io this will be prefixed on the image name for the generated deployment
        /// </summary>
        public string? ContainerRegistry { get; set; }
        public string? ImagePullSecretName { get; set; }
    }

    /// <summary>
    /// This attribute controls the operator's role name
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class RbacRoleAttribute : Attribute
    {
        public RbacRoleAttribute(string prefix) => Prefix = prefix;

        public string Prefix { get; }
    }
}
