using System;
using System.Collections.Generic;
using System.Reflection;

namespace KubeOps.Operator.Services
{
    internal interface IResourceTypeService
    {
        void AddAssembly(Assembly assembly);

        IEnumerable<Type> GetResourceTypesByAttribute<TAttribute>()
            where TAttribute : Attribute;

        IEnumerable<TAttribute> GetResourceAttributes<TAttribute>()
            where TAttribute : Attribute;
    }
}
