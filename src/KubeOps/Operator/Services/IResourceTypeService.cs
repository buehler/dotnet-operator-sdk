using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
