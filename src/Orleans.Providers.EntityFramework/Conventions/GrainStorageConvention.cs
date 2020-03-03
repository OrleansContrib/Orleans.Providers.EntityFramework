using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Providers.EntityFramework.Internal;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Conventions
{
    public class GrainStorageConvention : IGrainStorageConvention
    {
        private readonly GrainStorageConventionOptions _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GrainStorageConvention(IOptions<GrainStorageConventionOptions> options,
            IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _options = options.Value;
        }

        public virtual Action<IGrainState, TEntity> GetSetterFunc<TGrainState, TEntity>() where TEntity : class
        {
            return (state, entity) => state.State = entity;
        }

        public virtual Func<IGrainState, TEntity> GetGetterFunc<TGrainState, TEntity>() where TEntity : class
        {
            return state => state.State as TEntity;
        }


        #region Default Query

        public virtual Func<TContext, IQueryable<TEntity>> CreateDefaultDbSetAccessorFunc<TContext, TEntity>()
            where TContext : DbContext
            where TEntity : class
        {
            Type contextType = typeof(TContext);

            // Find a dbSet<TEntity> as default
            PropertyInfo dbSetPropertyInfo =
                contextType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(pInfo => pInfo.PropertyType == typeof(DbSet<TEntity>));

            if (dbSetPropertyInfo == null)
                throw new GrainStorageConfigurationException(
                    $"Could not find A property of type \"{typeof(DbSet<TEntity>).FullName}\" " +
                    $"on context with type \"{typeof(TContext).FullName}\"");

            var dbSetDelegate = (Func<TContext, IQueryable<TEntity>>)Delegate.CreateDelegate(
                typeof(Func<TContext, IQueryable<TEntity>>),
                null,
                dbSetPropertyInfo.GetMethod);

            // set queries as no tracking
            MethodInfo noTrackingMethodInfo = (typeof(GrainStorageConvention).GetMethod(nameof(AsNoTracking))
                                               ?? throw new Exception("Impossible"))
                .MakeGenericMethod(typeof(TContext), typeof(TEntity));

            // create final delegate which chains dbSet getter and no tracking delegates
            return (Func<TContext, IQueryable<TEntity>>)Delegate.CreateDelegate(
                typeof(Func<TContext, IQueryable<TEntity>>),
                dbSetDelegate,
                noTrackingMethodInfo);
        }

        public static IQueryable<TEntity> AsNoTracking<TContext, TEntity>(
            Func<TContext, IQueryable<TEntity>> func,
            TContext context)
            where TContext : DbContext
            where TEntity : class
            => func(context).AsNoTracking();

        public virtual Func<TContext, IAddressable, Task<TEntity>>
            CreateDefaultReadStateFunc<TContext, TGrain, TEntity>(
                GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (typeof(IGrainWithGuidKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector == null)
                    throw new GrainStorageConfigurationException($"GuidKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                var readQuery = ExpressionHelper.CreateQuery<TContext, TGrain, TEntity, Guid>(options);
                return (TContext context, IAddressable grainRef) =>
                {
                    Guid key = grainRef.GetPrimaryKey();
                    return readQuery(context, key);
                };
            }

            if (typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector == null)
                    throw new GrainStorageConfigurationException($"GuidKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                var query = ExpressionHelper.CreateCompoundQuery<TContext, TGrain, TEntity, Guid>(options);

                return (TContext context, IAddressable grainRef) =>
                {
                    Guid key = grainRef.GetPrimaryKey(out string keyExt);
                    return query(context, key, keyExt);
                };
            }

            if (typeof(IGrainWithIntegerKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.LongKeySelector == null)
                    throw new GrainStorageConfigurationException($"LongKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                var query = ExpressionHelper.CreateQuery<TContext, TGrain, TEntity, long>(options);
                return (TContext context, IAddressable grainRef) =>
                {
                    long key = grainRef.GetPrimaryKeyLong();
                    return query(context, key);
                };
            }

            if (typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.LongKeySelector == null)
                    throw new GrainStorageConfigurationException($"LongKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                var query = ExpressionHelper.CreateCompoundQuery<TContext, TGrain, TEntity, long>(options);
                return (TContext context, IAddressable grainRef) =>
                {
                    long key = grainRef.GetPrimaryKeyLong(out string keyExt);
                    return query(context, key, keyExt);
                };
            }

            if (typeof(IGrainWithStringKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                var query = ExpressionHelper.CreateQuery<TContext, TGrain, TEntity, string>(options);

                return (TContext context, IAddressable grainRef) =>
                {
                    string keyExt = grainRef.GetPrimaryKeyString();
                    return query(context, keyExt);
                };
            }

            throw new InvalidOperationException($"Unexpected grain type \"{typeof(TGrain).FullName}\"");
        }

        public virtual Func<TContext, IAddressable, Task<TEntity>>
            CreatePreCompiledDefaultReadStateFunc<TContext, TGrain, TEntity>(
                GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (typeof(IGrainWithGuidKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector == null)
                    throw new GrainStorageConfigurationException($"GuidKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                Func<TContext, Guid, Task<TEntity>> compiledQuery
                    = ExpressionHelper.CreateCompiledQuery<TContext, TGrain, TEntity, Guid>(options);

                return (TContext context, IAddressable grainRef) =>
                {
                    Guid key = grainRef.GetPrimaryKey();
                    return compiledQuery(context, key);
                };
            }

            if (typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector == null)
                    throw new GrainStorageConfigurationException($"GuidKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                Func<TContext, Guid, string, Task<TEntity>> compiledQuery
                    = ExpressionHelper.CreateCompiledCompoundQuery<TContext, TGrain, TEntity, Guid>(options);

                return (TContext context, IAddressable grainRef) =>
                {
                    Guid key = grainRef.GetPrimaryKey(out string keyExt);
                    return compiledQuery(context, key, keyExt);
                };
            }

            if (typeof(IGrainWithIntegerKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.LongKeySelector == null)
                    throw new GrainStorageConfigurationException($"LongKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                Func<TContext, long, Task<TEntity>> compiledQuery
                    = ExpressionHelper.CreateCompiledQuery<TContext, TGrain, TEntity, long>(options);

                return (TContext context, IAddressable grainRef) =>
                {
                    long key = grainRef.GetPrimaryKeyLong();
                    return compiledQuery(context, key);
                };
            }

            if (typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.LongKeySelector == null)
                    throw new GrainStorageConfigurationException($"LongKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                Func<TContext, long, string, Task<TEntity>> compiledQuery
                    = ExpressionHelper.CreateCompiledCompoundQuery<TContext, TGrain, TEntity, long>(options);

                return (TContext context, IAddressable grainRef) =>
                {
                    long key = grainRef.GetPrimaryKeyLong(out string keyExt);
                    return compiledQuery(context, key, keyExt);
                };
            }

            if (typeof(IGrainWithStringKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TEntity>).FullName}");

                Func<TContext, string, Task<TEntity>> compiledQuery
                    = ExpressionHelper.CreateCompiledQuery<TContext, TGrain, TEntity, string>(options);

                return (TContext context, IAddressable grainRef) =>
                {
                    string keyExt = grainRef.GetPrimaryKeyString();
                    return compiledQuery(context, keyExt);
                };
            }

            throw new InvalidOperationException($"Unexpected grain type \"{typeof(TGrain).FullName}\"");
        }

        public virtual void SetDefaultKeySelectors<TContext, TGrain, TEntity>(
            GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.KeyPropertyName == null)
                options.KeyPropertyName = _options.DefaultGrainKeyPropertyName;

            if (options.KeyExtPropertyName == null)
                options.KeyExtPropertyName = _options.DefaultGrainKeyExtPropertyName;


            PropertyInfo idProperty = ReflectionHelper.GetPropertyInfo<TEntity>(
                options.KeyPropertyName ?? _options.DefaultGrainKeyPropertyName);

            Type idType = idProperty.PropertyType;

            if (typeof(IGrainWithGuidKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector != null)
                    return;

                if (idType != typeof(Guid))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a Guid key " +
                        $"but the type {typeof(TEntity).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");


                options.GuidKeySelector = ReflectionHelper.GetAccessorExpression<TEntity, Guid>(idProperty);
                return;
            }


            if (typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                PropertyInfo keyExtProperty
                    = ReflectionHelper.GetPropertyInfo<TEntity>(
                        options.KeyExtPropertyName ?? _options.DefaultGrainKeyExtPropertyName);

                if (idType != typeof(Guid))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a Guid key " +
                        $"but the type {typeof(TEntity).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                if (keyExtProperty.PropertyType != typeof(string))
                    throw new GrainStorageConfigurationException($"Can not use property \"{keyExtProperty.Name}\" " +
                                                                 $"on grain state type \"{typeof(TEntity)}\". " +
                                                                 "KeyExt property must be of type string.");

                if (options.GuidKeySelector == null)
                    options.GuidKeySelector = ReflectionHelper.GetAccessorExpression<TEntity, Guid>(idProperty);
                if (options.KeyExtSelector == null)
                    options.KeyExtSelector = ReflectionHelper.GetAccessorExpression<TEntity, string>(keyExtProperty);

                return;
            }

            if (typeof(IGrainWithIntegerKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.LongKeySelector != null)
                    return;

                if (idType != typeof(long))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a long key " +
                        $"but the type {typeof(TEntity).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                options.LongKeySelector = ReflectionHelper.GetAccessorDelegate<TEntity, long>(idProperty);
                return;
            }

            if (typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                PropertyInfo keyExtProperty
                    = ReflectionHelper.GetPropertyInfo<TEntity>(
                        options.KeyExtPropertyName ?? _options.DefaultGrainKeyExtPropertyName);

                if (keyExtProperty.PropertyType != typeof(string))
                    throw new GrainStorageConfigurationException($"Can not use property \"{keyExtProperty.Name}\" " +
                                                                 $"on grain state type \"{typeof(TEntity)}\". " +
                                                                 "KeyExt property must be of type string.");

                if (options.LongKeySelector == null)
                    options.LongKeySelector = ReflectionHelper.GetAccessorDelegate<TEntity, long>(idProperty);
                if (options.KeyExtSelector == null)
                    options.KeyExtSelector = ReflectionHelper.GetAccessorExpression<TEntity, string>(keyExtProperty);
                return;
            }

            if (typeof(IGrainWithStringKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.KeyExtSelector != null)
                    return;

                if (idType != typeof(string))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a string key " +
                        $"but the type {typeof(TEntity).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                options.KeyExtSelector = ReflectionHelper.GetAccessorExpression<TEntity, string>(idProperty);
                return;
            }

            throw new InvalidOperationException($"Unexpected grain type \"{typeof(TGrain).FullName}\"");
        }

        #endregion

        #region IsPersisted

        /// <summary>
        /// Creates a method that tests the value of the Id property to default of its type.
        /// </summary>
        /// <param name="options"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public virtual Func<TEntity, bool> CreateIsPersistedFunc<TEntity>(GrainStorageOptions options)
            where TEntity : class
        {
            PropertyInfo idProperty
                = ReflectionHelper.GetPropertyInfo<TEntity>(
                    options.PersistenceCheckPropertyName ?? _options.DefaultPersistenceCheckPropertyName);

            if (!idProperty.CanRead)
                throw new GrainStorageConfigurationException(
                    $"Property \"{idProperty.Name}\" of type \"{idProperty.PropertyType.FullName}\" " +
                    "must have a public getter.");

            MethodInfo methodInfo = typeof(GrainStorageConvention).GetMethod(
                idProperty.PropertyType.IsValueType
                    ? nameof(IsNotDefaultValueType)
                    : nameof(IsNotDefaultReferenceType),
                BindingFlags.Static | BindingFlags.Public);
            if (methodInfo == null)
                throw new Exception("Impossible");

            return (Func<TEntity, bool>)
                Delegate.CreateDelegate(typeof(Func<TEntity, bool>),
                    idProperty,
                    methodInfo.MakeGenericMethod(typeof(TEntity), idProperty.PropertyType));
        }

        public static bool IsNotDefaultValueType<TEntity, TProperty>(
            PropertyInfo propertyInfo, TEntity state)
            where TProperty : struct
        {
            return !((TProperty)propertyInfo.GetValue(state)).Equals(default(TProperty));
        }

        public static bool IsNotDefaultReferenceType<TEntity, TProperty>(
            PropertyInfo propertyInfo, TEntity state)
            where TProperty : class
        {
            return !((TProperty)propertyInfo.GetValue(state)).Equals(default(TProperty));
        }

        #endregion

        #region ETag

        public virtual void FindAndConfigureETag<TContext, TGrain, TEntity>(
            GrainStorageOptions<TContext, TGrain, TEntity> options,
            bool throwIfNotFound)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                IEntityType entityType = context.Model.FindEntityType(typeof(TEntity));

                if (entityType == null)
                    return;

                if (!FindAndConfigureETag(entityType, options) && throwIfNotFound)
                    throw new GrainStorageConfigurationException(
                        $"Could not find a valid ETag property on type \"{typeof(TEntity).FullName}\".");
            }
        }

        public virtual void ConfigureETag<TContext, TGrain, TEntity>(
            string propertyName,
            GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (options == null) throw new ArgumentNullException(nameof(options));

            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                IEntityType entityType = context.Model.FindEntityType(typeof(TEntity));

                if (entityType == null)
                    return;

                ConfigureETag(entityType, propertyName, options);
            }
        }

        private static bool FindAndConfigureETag<TContext, TGrain, TEntity>(
            IEntityType entityType,
            GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IEnumerable<IProperty> properties = entityType.GetProperties();

            foreach (IProperty property in properties)
            {
                if (!property.IsConcurrencyToken)
                    continue;

                ConfigureETag(property, options);

                return true;
            }

            return false;
        }


        private static void ConfigureETag<TContext, TGrain, TEntity>(
            IEntityType entityType,
            string propertyName,
            GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IProperty property = entityType.FindProperty(propertyName);

            if (property == null)
                throw new GrainStorageConfigurationException(
                    $"Property {propertyName} on model{typeof(TEntity).FullName} not found.");

            ConfigureETag(property, options);
        }


        private static void ConfigureETag<TContext, TGrain, TEntity>(
            IProperty property,
            GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (!property.IsConcurrencyToken)
                throw new GrainStorageConfigurationException($"Property {property.Name} is not a concurrency token.");

            options.CheckForETag = true;
            options.ETagPropertyName = property.Name;
            options.ETagProperty = property;
            options.ETagType = property.ClrType;

            options.GetETagFunc = CreateGetETagFunc<TEntity>(property.Name);
            options.ConvertETagObjectToStringFunc
                = CreateConvertETagObjectToStringFunc();
        }

        private static Func<TEntity, string> CreateGetETagFunc<TEntity>(string propertyName)
        {
            PropertyInfo propertyInfo = ReflectionHelper.GetPropertyInfo<TEntity>(propertyName);

            var getterDelegate = (Func<TEntity, object>)Delegate.CreateDelegate(
                typeof(Func<TEntity, object>),
                null,
                propertyInfo.GetMethod);

            return state => ConvertETagObjectToString(getterDelegate(state));
        }

        private static Func<object, string> CreateConvertETagObjectToStringFunc()
        {
            return ConvertETagObjectToString;
        }

        private static string ConvertETagObjectToString(object obj)
        {
            if (obj == null)
                return null;
            switch (obj)
            {
                case byte[] bytes:
                    return ByteToHexBitFiddle(bytes);
                default:
                    return obj.ToString();
            }
        }

        private static string ByteToHexBitFiddle(byte[] bytes)
        {
            var c = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }

            return new string(c);
        }

        #endregion
    }
}