using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ExternalDocsAttribute : Attribute
    {
        public ExternalDocsAttribute(string url, string? description = null)
        {
            Description = description;
            Url = url;
        }

        public string? Description { get; }

        public string Url { get; }
    }
}
