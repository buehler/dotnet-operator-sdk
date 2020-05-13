using System;

namespace KubeOps.Operator.Entities.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PatternAttribute : Attribute
    {
        public PatternAttribute(string regexPattern)
        {
            RegexPattern = regexPattern;
        }

        public string RegexPattern { get; }
    }
}
