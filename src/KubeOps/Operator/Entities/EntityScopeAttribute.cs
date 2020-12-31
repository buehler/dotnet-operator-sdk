using System;

namespace KubeOps.Operator.Entities
{
    /// <summary>
    /// Defines the scope of an entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EntityScopeAttribute : Attribute
    {
        public EntityScopeAttribute(EntityScope scope = default)
        {
            Scope = scope;
        }

        /// <summary>
        /// The defined scope.
        /// </summary>
        public EntityScope Scope { get; set; }
    }
}
