using System;
using System.Reflection;

namespace Orleans.Providers.EntityFramework.Utils
{
    internal static class ReflectionHelper
    {
        public static PropertyInfo GetPropertyInfo<T>(string propertyName)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            Type statetype = typeof(T);

            PropertyInfo idProperty
                = statetype.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            if (idProperty == null)
                throw new Exception(
                    $"Could not find \"{propertyName}\" property on type \"{statetype.FullName}\". Either configure the state locator predicate manually or update your model.");

            if (!idProperty.CanRead)
                throw new Exception(
                    $"The property \"{propertyName}\" of type \"{statetype.FullName}\" must have a public getter.");

            return idProperty;
        }
    }
}