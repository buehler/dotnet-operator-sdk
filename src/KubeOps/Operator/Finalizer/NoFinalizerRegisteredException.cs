using System;
using System.Reflection;

namespace KubeOps.Operator.Finalizer
{
    public class NoFinalizerRegisteredException : Exception
    {
        public NoFinalizerRegisteredException()
        {
        }

        public NoFinalizerRegisteredException(MemberInfo entityType)
            : this(@$"No finalizer is registered for type ""{entityType.Name}""")
        {
        }

        public NoFinalizerRegisteredException(string message)
            : base(message)
        {
        }

        public NoFinalizerRegisteredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
