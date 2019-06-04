using System;
using System.Reflection;
using Orleans.Providers.EntityFramework.Exceptions;

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
                throw new GrainStorageConfigurationException(
                    $"Could not find \"{propertyName}\" property on type \"{statetype.FullName}\". Either configure the state locator predicate manually or update your model.");

            if (!idProperty.CanRead)
                throw new GrainStorageConfigurationException(
                    $"The property \"{propertyName}\" of type \"{statetype.FullName}\" must have a public getter.");

            return idProperty;
        }

        public static Func<T, TProperty> GetAccessorDelegate<T, TProperty>(PropertyInfo pInfo)
        {
            return (Func<T, TProperty>)Delegate.CreateDelegate(
                typeof(Func<T, TProperty>),
                null,
                pInfo.GetMethod);
        }
    }
}