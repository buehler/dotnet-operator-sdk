using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; }
    }
}
