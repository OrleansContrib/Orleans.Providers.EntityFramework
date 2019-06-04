using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Providers.EntityFramework.Utils;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Conventions
{
    public class GrainStorageConventionV2 : IGrainStorageConvention
    {
        private readonly GrainStorageConventionOptions _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GrainStorageConventionV2(IOptions<GrainStorageConventionOptions> options, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _options = options.Value;
        }

        #region Default Query

        public virtual Func<TContext, IQueryable<TGrainState>> CreateDefaultDbSetAccessorFunc<TContext, TGrainState>()
            where TContext : DbContext
            where TGrainState : class, new()
        {
            Type contextType = typeof(TContext);

            // Find a dbSet<TGrainState> as default
            PropertyInfo dbSetPropertyInfo =
                contextType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(pInfo => pInfo.PropertyType == typeof(DbSet<TGrainState>));

            if (dbSetPropertyInfo == null)
                throw new GrainStorageConfigurationException($"Could not find A property of type \"{typeof(DbSet<TGrainState>).FullName}\" " +
                                    $"on context with type \"{typeof(TContext).FullName}\"");

            var dbSetDelegate = (Func<TContext, IQueryable<TGrainState>>)Delegate.CreateDelegate(
                typeof(Func<TContext, IQueryable<TGrainState>>),
                null,
                dbSetPropertyInfo.GetMethod);

            // set queries as no tracking
            MethodInfo noTrackingMethodInfo = (this.GetType().GetMethod(nameof(AsNoTracking))
                                        ?? throw new Exception("Impossible"))
                .MakeGenericMethod(typeof(TContext), typeof(TGrainState));

            // create final delegate which chains dbSet getter and no tracking delegates
            return (Func<TContext, IQueryable<TGrainState>>)Delegate.CreateDelegate(
                typeof(Func<TContext, IQueryable<TGrainState>>),
                dbSetDelegate,
                noTrackingMethodInfo);
        }

        public Func<IAddressable, Expression<Func<TGrainState, bool>>> CreateDefaultGrainStateQueryExpressionGeneratorFunc<TGrain, TGrainState>(GrainStorageOptions options) where TGrain : Grain<TGrainState> where TGrainState : new()
        {
            throw new NotImplementedException();
        }

        public Func<IAddressable, Expression<Func<TGrainState, bool>>> CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(Func<IAddressable, ValueType> getGrainIdFunc, string stateIdPropertyName)
        {
            throw new NotImplementedException();
        }

        public Func<IAddressable, Expression<Func<TGrainState, bool>>> CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(Func<IAddressable, string> getGrainIdFunc, string stateIdPropertyName)
        {
            throw new NotImplementedException();
        }

        public static IQueryable<TGrainState> AsNoTracking<TContext, TGrainState>(
            Func<TContext, IQueryable<TGrainState>> func,
            TContext context)
            where TContext : DbContext
            where TGrainState : class, new()
            => func(context).AsNoTracking();

        public Func<TContext, IAddressable, Task<TGrainState>>
            CreateDefaultReadStateFunc<TContext, TGrain, TGrainState>(
                GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
        {
            if (typeof(IGrainWithGuidKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector == null)
                    throw new GrainStorageConfigurationException($"GuidKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                return (TContext context, IAddressable grainRef) =>
                {
                    Guid key = grainRef.GetPrimaryKey();
                    return options.DbSetAccessor(context)
                        .SingleOrDefaultAsync(
                            state => options.GuidKeySelector(state).Equals(key));
                };
            }

            if (typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector == null)
                    throw new GrainStorageConfigurationException($"GuidKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                return (TContext context, IAddressable grainRef) =>
                {
                    Guid key = grainRef.GetPrimaryKey(out string keyExt);
                    return
                        options.DbSetAccessor(context)
                            .SingleOrDefaultAsync(state =>
                                options.GuidKeySelector(state).Equals(key)
                                && options.KeyExtSelector(state).Equals(keyExt));
                };
            }

            if (typeof(IGrainWithIntegerKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.LongKeySelector == null)
                    throw new GrainStorageConfigurationException($"LongKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                return (TContext context, IAddressable grainRef) =>
                {
                    long key = grainRef.GetPrimaryKeyLong();
                    return options.DbSetAccessor(context)
                        .SingleOrDefaultAsync(state => options.LongKeySelector(state).Equals(key));

                };
            }

            if (typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.LongKeySelector == null)
                    throw new GrainStorageConfigurationException($"LongKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                return (TContext context, IAddressable grainRef) =>
                {
                    long key = grainRef.GetPrimaryKeyLong(out string keyExt);
                    return options.DbSetAccessor(context)
                        .SingleOrDefaultAsync(state =>
                            options.LongKeySelector(state).Equals(key)
                            && options.KeyExtSelector(state).Equals(keyExt));
                };
            }

            if (typeof(IGrainWithStringKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                var compiledQuery = EF.CompileAsyncQuery((TContext context, string keyExt)
                    => options.DbSetAccessor(context)
                        .SingleOrDefault(state =>
                             options.KeyExtSelector(state).Equals(keyExt)));

                return (TContext context, IAddressable grainRef) =>
                {
                    string keyExt = grainRef.GetPrimaryKeyString();
                    return options.DbSetAccessor(context)
                        .SingleOrDefaultAsync(state =>
                             options.KeyExtSelector(state).Equals(keyExt));
                };
            }

            throw new InvalidOperationException($"Unexpected grain type \"{typeof(TGrain).FullName}\"");
        }

        public Func<TContext, IAddressable, Task<TGrainState>>
            CreatePreCompiledDefaultReadStateFunc<TContext, TGrain, TGrainState>(
                GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
        {
            if (typeof(IGrainWithGuidKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector == null)
                    throw new GrainStorageConfigurationException($"GuidKeySelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                var compiledQuery = EF.CompileAsyncQuery((TContext context, Guid grainKey)
                    => options.DbSetAccessor(context)
                        .SingleOrDefault(state => options.GuidKeySelector(state).Equals(grainKey)));

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
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                var compiledQuery = EF.CompileAsyncQuery((TContext context, Guid grainKey, string keyExt)
                    => options.DbSetAccessor(context)
                        .SingleOrDefault(state =>
                            options.GuidKeySelector(state).Equals(grainKey)
                            && options.KeyExtSelector(state).Equals(keyExt)));

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
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                var compiledQuery = EF.CompileAsyncQuery((TContext context, long grainKey)
                    => options.DbSetAccessor(context)
                        .SingleOrDefault(state => options.LongKeySelector(state).Equals(grainKey)));

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
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");
                if (options.KeyExtSelector == null)
                    throw new GrainStorageConfigurationException($"KeyExtSelector is not defined for " +
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                var compiledQuery = EF.CompileAsyncQuery((TContext context, long grainKey, string keyExt)
                    => options.DbSetAccessor(context)
                        .SingleOrDefault(state =>
                            options.LongKeySelector(state).Equals(grainKey)
                            && options.KeyExtSelector(state).Equals(keyExt)));

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
                                                                 $"{typeof(GrainStorageOptions<TContext, TGrain, TGrainState>).FullName}");

                var compiledQuery = EF.CompileAsyncQuery((TContext context, string keyExt)
                    => options.DbSetAccessor(context)
                        .SingleOrDefault(state =>
                             options.KeyExtSelector(state).Equals(keyExt)));

                return (TContext context, IAddressable grainRef) =>
                {
                    string keyExt = grainRef.GetPrimaryKeyString();
                    return compiledQuery(context, keyExt);
                };
            }

            throw new InvalidOperationException($"Unexpected grain type \"{typeof(TGrain).FullName}\"");
        }

        public void SetDefaultKeySelectors<TContext, TGrain, TGrainState>(
            GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
        {

            if (options == null) throw new ArgumentNullException(nameof(options));

            PropertyInfo idProperty = ReflectionHelper.GetPropertyInfo<TGrainState>(
                options.KeyPropertyName ?? _options.DefaultGrainKeyPropertyName);

            Type idType = idProperty.PropertyType;

            if (typeof(IGrainWithGuidKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.GuidKeySelector != null)
                    return;

                if (idType != typeof(Guid))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a Guid key " +
                        $"but the type {typeof(TGrainState).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");


                options.GuidKeySelector = ReflectionHelper.GetAccessorDelegate<TGrainState, Guid>(idProperty);
                return;
            }


            if (typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                PropertyInfo keyExtProperty
                    = ReflectionHelper.GetPropertyInfo<TGrainState>(
                        options.KeyExtPropertyName ?? _options.DefaultGrainKeyExtPropertyName);

                if (idType != typeof(Guid))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a Guid key " +
                        $"but the type {typeof(TGrainState).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                if (keyExtProperty.PropertyType != typeof(string))
                    throw new GrainStorageConfigurationException($"Can not use property \"{keyExtProperty.Name}\" " +
                                        $"on grain state type \"{typeof(TGrainState)}\". " +
                                        "KeyExt property must be of type string.");

                if (options.GuidKeySelector == null)
                    options.GuidKeySelector = ReflectionHelper.GetAccessorDelegate<TGrainState, Guid>(idProperty);
                if (options.KeyExtSelector == null)
                    options.KeyExtSelector = ReflectionHelper.GetAccessorDelegate<TGrainState, string>(keyExtProperty);

                return;
            }

            if (typeof(IGrainWithIntegerKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.LongKeySelector != null)
                    return;

                if (idType != typeof(long))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a long key " +
                        $"but the type {typeof(TGrainState).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                options.LongKeySelector = ReflectionHelper.GetAccessorDelegate<TGrainState, long>(idProperty);
                return;
            }

            if (typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                PropertyInfo keyExtProperty
                    = ReflectionHelper.GetPropertyInfo<TGrainState>(
                        options.KeyExtPropertyName ?? _options.DefaultGrainKeyExtPropertyName);

                if (keyExtProperty.PropertyType != typeof(string))
                    throw new GrainStorageConfigurationException($"Can not use property \"{keyExtProperty.Name}\" " +
                                        $"on grain state type \"{typeof(TGrainState)}\". " +
                                        "KeyExt property must be of type string.");

                if (options.LongKeySelector == null)
                    options.LongKeySelector = ReflectionHelper.GetAccessorDelegate<TGrainState, long>(idProperty);
                if (options.KeyExtSelector == null)
                    options.KeyExtSelector = ReflectionHelper.GetAccessorDelegate<TGrainState, string>(keyExtProperty);
                return;
            }

            if (typeof(IGrainWithStringKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (options.KeyExtSelector != null)
                    return;

                if (idType != typeof(string))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a string key " +
                        $"but the type {typeof(TGrainState).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                options.KeyExtSelector = ReflectionHelper.GetAccessorDelegate<TGrainState, string>(idProperty);
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
        /// <typeparam name="TGrainState"></typeparam>
        /// <returns></returns>
        public virtual Func<TGrainState, bool> CreateIsPersistedFunc<TGrainState>(GrainStorageOptions options)
        {
            PropertyInfo idProperty
                = ReflectionHelper.GetPropertyInfo<TGrainState>(
                    options.PersistenceCheckPropertyName ?? _options.DefaultPersistenceCheckPropertyName);

            if (!idProperty.CanRead)
                throw new GrainStorageConfigurationException(
                    $"Property \"{idProperty.Name}\" of type \"{idProperty.PropertyType.FullName}\" " +
                    "must have a public getter.");

            MethodInfo methodInfo = this.GetType().GetMethod(
                idProperty.PropertyType.IsValueType
                    ? nameof(IsNotDefaultValueType)
                    : nameof(IsNotDefaultReferenceType),
                BindingFlags.Static | BindingFlags.Public);
            if (methodInfo == null)
                throw new Exception("Impossible");

            return (Func<TGrainState, bool>)
                Delegate.CreateDelegate(typeof(Func<TGrainState, bool>),
                    idProperty,
                    methodInfo.MakeGenericMethod(typeof(TGrainState), idProperty.PropertyType));
        }

        public static bool IsNotDefaultValueType<TGrainState, TProperty>(
            PropertyInfo propertyInfo, TGrainState state)
            where TProperty : struct
        {
            return !((TProperty)propertyInfo.GetValue(state)).Equals(default(TProperty));
        }

        public static bool IsNotDefaultReferenceType<TGrainState, TProperty>(
            PropertyInfo propertyInfo, TGrainState state)
            where TProperty : class
        {
            return !((TProperty)propertyInfo.GetValue(state)).Equals(default(TProperty));
        }

        #endregion

        #region ETag

        public void FindAndConfigureETag<TContext, TGrain, TGrainState>(
            GrainStorageOptions<TContext, TGrain, TGrainState> options,
            bool throwIfNotFound)
            where TContext : DbContext
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                IEntityType entityType = context.Model.FindEntityType(typeof(TGrainState));

                if (entityType == null)
                    return;

                if (!FindAndConfigureETag(entityType, options) && throwIfNotFound)
                    throw new GrainStorageConfigurationException(
                        $"Could not find a valid ETag property on type \"{typeof(TGrainState).FullName}\".");
            }
        }

        public void ConfigureETag<TContext, TGrain, TGrainState>(
            string propertyName,
            GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (options == null) throw new ArgumentNullException(nameof(options));

            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                IEntityType entityType = context.Model.FindEntityType(typeof(TGrainState));

                if (entityType == null)
                    return;

                ConfigureETag(entityType, propertyName, options);
            }
        }

        private static bool FindAndConfigureETag<TContext, TGrain, TGrainState>(
            IEntityType entityType,
            GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
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


        private static void ConfigureETag<TContext, TGrain, TGrainState>(
            IEntityType entityType,
            string propertyName,
            GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IProperty property = entityType.FindProperty(propertyName);

            if (property == null)
                throw new GrainStorageConfigurationException(
                    $"Property {propertyName} on model{typeof(TGrainState).FullName} not found.");

            ConfigureETag(property, options);
        }


        private static void ConfigureETag<TContext, TGrain, TGrainState>(
            IProperty property,
            GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (!property.IsConcurrencyToken)
                throw new GrainStorageConfigurationException($"Property {property.Name} is not a concurrency token.");

            options.CheckForETag = true;
            options.ETagPropertyName = property.Name;
            options.ETagProperty = property;
            options.ETagType = property.ClrType;

            options.GetETagFunc = CreateGetETagFunc<TGrainState>(property.Name);
            options.ConvertETagObjectToStringFunc
                = CreateConvertETagObjectToStringFunc();
        }

        private static Func<TGrainState, string> CreateGetETagFunc<TGrainState>(string propertyName)
        {
            PropertyInfo propertyInfo = ReflectionHelper.GetPropertyInfo<TGrainState>(propertyName);

            var getterDelegate = (Func<TGrainState, object>)Delegate.CreateDelegate(
                typeof(Func<TGrainState, object>),
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