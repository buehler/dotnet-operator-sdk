using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeMinimumAttribute : Attribute
    {
        public double Minimum { get; set; }

        public bool ExclusiveMinimum { get; set; }
    }
}
