using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ItemsAttribute : Attribute
    {
        public long MinItems { get; set; } = -1;

        public long MaxItems { get; set; } = -1;
    }
}
