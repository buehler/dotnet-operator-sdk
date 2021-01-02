using System;
using System.Reflection;

namespace KubeOps.Operator.Finalizer
{
    /// <summary>
    /// Exception that is thrown when no finalizer is registered for a given type.
    /// </summary>
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
