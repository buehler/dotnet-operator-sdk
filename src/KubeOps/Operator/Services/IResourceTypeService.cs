using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace KubeOps.Operator.Services
{
    public interface IResourceTypeService
    {
        public void AddAssembly(Assembly assembly);

        public IEnumerable<Type> GetResourceTypesByAttribute<TAttribute>()
            where TAttribute : Attribute;

        IEnumerable<TAttribute> GetResourceAttributes<TAttribute>()
            where TAttribute : Attribute;
    }
}
