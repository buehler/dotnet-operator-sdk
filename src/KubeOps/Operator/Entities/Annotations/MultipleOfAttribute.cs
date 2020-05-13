using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MultipleOfAttribute : Attribute
    {
        public MultipleOfAttribute(double value)
        {
            Value = value;
        }

        public double Value { get; }
    }
}
