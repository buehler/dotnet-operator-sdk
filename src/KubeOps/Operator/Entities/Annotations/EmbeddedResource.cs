using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EmbeddedResourceAttribute : Attribute
    {
    }
}
