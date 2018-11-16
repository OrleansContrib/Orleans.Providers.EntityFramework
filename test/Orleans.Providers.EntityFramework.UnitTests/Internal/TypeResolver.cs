using System;
using System.Linq;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.UnitTests.Internal
{
    public class TypeResolver : ITypeResolver
    {
        public Type ResolveType(string name)
        {
            Type grainImplType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(name, false))
                .FirstOrDefault(t => t != null);
            if (grainImplType == null)
                throw new Exception($"Could not resolve \"{name}\" type.");

            return grainImplType;
        }

        public bool TryResolveType(string name, out Type type)
        {
            type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(name, false))
                .FirstOrDefault(t => t != null);

            return type != null;
        }
    }
}