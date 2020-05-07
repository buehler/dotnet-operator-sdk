using System;

namespace Dos.Operator.Rbac
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GenericRbacAttribute : Attribute
    {
        public string[] Groups { get; set; } = { };

        public string[] Resources { get; set; } = { };

        public string[] Urls { get; set; } = { };

        public RbacVerb Verbs { get; set; }
    }
}
