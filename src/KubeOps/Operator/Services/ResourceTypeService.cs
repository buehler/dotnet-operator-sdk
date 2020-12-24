using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace KubeOps.Operator.Services
{
    public class ResourceTypeService : IResourceTypeService
    {
        private readonly ICollection<Assembly> _assemblies;

        public ResourceTypeService(params Assembly[] assemblies)
        {
            _assemblies = new HashSet<Assembly>(assemblies);
        }

        public void AddAssembly(Assembly assembly)
        {
            _assemblies.Add(assembly);
        }

        public IEnumerable<Type> GetResourceTypesByAttribute<TAttribute>()
            where TAttribute : Attribute =>
            _assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttributes<TAttribute>().Any());

        public IEnumerable<TAttribute> GetResourceAttributes<TAttribute>()
            where TAttribute : Attribute =>
            _assemblies.SelectMany(
                    assembly => assembly.GetTypes())
                .SelectMany(type => type.GetCustomAttributes<TAttribute>(true));
    }
}
