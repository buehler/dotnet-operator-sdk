using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LengthAttribute : Attribute
    {
        public int MinLength { get; set; } = -1;

        public int MaxLength { get; set; } = -1;
    }
}
