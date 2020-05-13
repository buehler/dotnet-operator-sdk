using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeMaximumAttribute : Attribute
    {
        public double Maximum { get; set; } = -1;

        public bool ExclusiveMaximum { get; set; }
    }
}
