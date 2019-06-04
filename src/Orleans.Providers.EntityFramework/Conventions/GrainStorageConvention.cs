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
    public class GrainStorageConvention : IGrainStorageConvention
    {
        private readonly GrainStorageConventionOptions _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GrainStorageConvention(IOptions<GrainStorageConventionOptions> options, IServiceScopeFactory serviceScopeFactory)
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
            throw new NotImplementedException();
        }

        public Func<TContext, IAddressable, Task<TGrainState>>
            CreatePreCompiledDefaultReadStateFunc<TContext, TGrain, TGrainState>(
                GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
        {
            throw new NotImplementedException();
        }

        public void SetDefaultKeySelectors<TContext, TGrain, TGrainState>(
            GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
        {
            throw new NotImplementedException();
        }



        #endregion

        #region Query Expressions 

        public virtual Func<IAddressable, Expression<Func<TGrainState, bool>>>
            CreateDefaultGrainStateQueryExpressionGeneratorFunc<TGrain, TGrainState>(
                GrainStorageOptions options)
            where TGrain : Grain<TGrainState>
            where TGrainState : new()
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            PropertyInfo idProperty = ReflectionHelper.GetPropertyInfo<TGrainState>(
                options.KeyPropertyName ?? _options.DefaultGrainKeyPropertyName);

            Type idType = idProperty.PropertyType;

            if (typeof(IGrainWithGuidKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (idType != typeof(Guid))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a Guid key " +
                        $"but the type {typeof(TGrainState).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                return CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                    grainRef => grainRef.GetPrimaryKey(),
                    idProperty.Name);

            }

            if (typeof(IGrainWithIntegerKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (idType != typeof(long))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a long key " +
                        $"but the type {typeof(TGrainState).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                return CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                    grainRef => grainRef.GetPrimaryKeyLong(),
                    idProperty.Name);

            }

            if (typeof(IGrainWithStringKey).IsAssignableFrom(typeof(TGrain)))
            {
                if (idType != typeof(string))
                    throw new GrainStorageConfigurationException(
                        $"Incompatible grain and state. \"{typeof(TGrain).FullName}\" expects a string key " +
                        $"but the type {typeof(TGrainState).FullName}.{idProperty.Name} " +
                        $"is of type {idType.FullName}.");

                return CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                grainRef => grainRef.GetPrimaryKeyString(),
                idProperty.Name);
            }

            if (typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(typeof(TGrain)))
            {
                PropertyInfo keyExtProperty
                    = ReflectionHelper.GetPropertyInfo<TGrainState>(
                        options.KeyExtPropertyName ?? _options.DefaultGrainKeyExtPropertyName);

                if (keyExtProperty.PropertyType != typeof(string))
                    throw new GrainStorageConfigurationException($"Can not use property \"{keyExtProperty.Name}\" " +
                                        $"on grain state type \"{typeof(TGrainState)}\". " +
                                        "KeyExt property must be of type string.");

                return CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                    (IAddressable grainRef, out string keyExt) =>
                        grainRef.GetPrimaryKey(out keyExt),
                    idProperty.Name,
                    keyExtProperty.Name);
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

                return CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                    (IAddressable grainRef, out string keyExt) =>
                        grainRef.GetPrimaryKeyLong(out keyExt),
                    idProperty.Name,
                    keyExtProperty.Name);
            }

            throw new GrainStorageConfigurationException($"Unexpected grain type \"{typeof(TGrain)}\".");
        }

        #region ValueTypes: long and guid

        public virtual Func<IAddressable, Expression<Func<TGrainState, bool>>>
            CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                Func<IAddressable, ValueType> getGrainIdValueTypeFunc,
                string stateIdPropertyName)
        {
            if (getGrainIdValueTypeFunc == null) throw new ArgumentNullException(nameof(getGrainIdValueTypeFunc));
            if (stateIdPropertyName == null) throw new ArgumentNullException(nameof(stateIdPropertyName));


            // Create a delegate which executes getGrainIdValueTypeFunc and passes it to the expression generator
            MethodInfo createExpressionMethodInfo
                = this.GetType().GetMethod(nameof(CreateValueTypeKeyGrainStateQueryExpressionFuncProxy),
                    BindingFlags.Static | BindingFlags.Public);
            if (createExpressionMethodInfo == null)
                throw new Exception("Impossible");


            Tuple<Func<IAddressable, ValueType>, string> target
                = Tuple.Create(getGrainIdValueTypeFunc, stateIdPropertyName);

            // Create the final delegate
            var createDelegate = (Func<IAddressable, Expression<Func<TGrainState, bool>>>)
                Delegate.CreateDelegate(typeof(Func<IAddressable, Expression<Func<TGrainState, bool>>>),
                    target,
                    createExpressionMethodInfo.MakeGenericMethod(typeof(TGrainState)));

            return createDelegate;
        }

        public static Expression<Func<TGrainState, bool>>
            CreateValueTypeKeyGrainStateQueryExpressionFuncProxy<TGrainState>(
                Tuple<Func<IAddressable, ValueType>, string> arg,
                IAddressable grainRef)
        {
            ValueType grainId = arg.Item1(grainRef);

            return CreateExpression<TGrainState>(grainId, arg.Item2);
        }

        #endregion

        #region Compound Types

        public virtual Func<IAddressable, Expression<Func<TGrainState, bool>>>
            CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                GetCompoundKeyDelegate getGrainCompoundKeyFunc,
                string stateIdPropertyName,
                string stateKeyExtPropertyName)
        {
            if (getGrainCompoundKeyFunc == null) throw new ArgumentNullException(nameof(getGrainCompoundKeyFunc));
            if (stateIdPropertyName == null) throw new ArgumentNullException(nameof(stateIdPropertyName));

            // Create a delegate which executes getGrainCompoundKeyFunc and passes it to the expression generator
            MethodInfo createExpressionMethodInfo
                = this.GetType().GetMethod(nameof(CreateValueTypeCompoundKeyGrainStateQueryExpressionFuncProxy),
                    BindingFlags.Static | BindingFlags.Public);
            if (createExpressionMethodInfo == null)
                throw new Exception("Impossible");

            Tuple<GetCompoundKeyDelegate, string, string> target
                = Tuple.Create(getGrainCompoundKeyFunc, stateIdPropertyName, stateKeyExtPropertyName);

            // Create the final delegate
            var createDelegate = (Func<IAddressable, Expression<Func<TGrainState, bool>>>)
                Delegate.CreateDelegate(typeof(Func<IAddressable, Expression<Func<TGrainState, bool>>>),
                    target,
                    createExpressionMethodInfo.MakeGenericMethod(typeof(TGrainState)));

            return createDelegate;
        }

        public static Expression<Func<TGrainState, bool>>
            CreateValueTypeCompoundKeyGrainStateQueryExpressionFuncProxy<TGrainState>(
                Tuple<GetCompoundKeyDelegate, string, string> arg,
                IAddressable grainRef)
        {
            ValueType grainId = arg.Item1(grainRef, out string extKey);

            return CreateExpression<TGrainState>(grainId, arg.Item2,
                extKey, arg.Item3);
        }

        #endregion

        #region String keys

        public Func<IAddressable, Expression<Func<TGrainState, bool>>>
            CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                Func<IAddressable, string> getGrainIdStringFunc,
                string stateIdPropertyName)
        {
            if (getGrainIdStringFunc == null) throw new ArgumentNullException(nameof(getGrainIdStringFunc));
            if (stateIdPropertyName == null) throw new ArgumentNullException(nameof(stateIdPropertyName));


            MethodInfo createExpressionMethodInfo
                = this.GetType().GetMethod(nameof(CreateStringKeyGrainStateQueryExpressionFuncProxy),
                    BindingFlags.Static | BindingFlags.Public);
            if (createExpressionMethodInfo == null)
                throw new Exception("Impossible");


            Tuple<Func<IAddressable, string>, string> target
                = Tuple.Create(getGrainIdStringFunc, stateIdPropertyName);

            var createDelegate = (Func<IAddressable, Expression<Func<TGrainState, bool>>>)
                Delegate.CreateDelegate(typeof(Func<IAddressable, Expression<Func<TGrainState, bool>>>),
                    target,
                    createExpressionMethodInfo.MakeGenericMethod(typeof(TGrainState)));

            return createDelegate;
        }

        public static Expression<Func<TGrainState, bool>>
            CreateStringKeyGrainStateQueryExpressionFuncProxy<TGrainState>(
                Tuple<Func<IAddressable, string>, string> arg,
                IAddressable grainRef)
        {
            string grainId = arg.Item1(grainRef);

            return CreateExpression<TGrainState>(grainId, arg.Item2);
        }

        #endregion

        private static Expression<Func<TGrainState, bool>> CreateExpression<TGrainState>(
            object grainId,
            string idPropertyName)
        {
            if (idPropertyName == null) throw new ArgumentNullException(nameof(idPropertyName));

            ParameterExpression stateParam = Expression.Parameter(typeof(TGrainState), "state");
            Expression idProperty = Expression.Property(stateParam, idPropertyName);


            ConstantExpression grainIdConstant = Expression.Constant(grainId);

            Expression expression = Expression.Equal(idProperty, grainIdConstant);

            return Expression.Lambda<Func<TGrainState, bool>>(expression, stateParam);
        }

        private static Expression<Func<TGrainState, bool>> CreateExpression<TGrainState>(
            object grainId,
            string idPropertyName,
            string keyExt,
            string keyExtPropertyName)
        {
            if (idPropertyName == null) throw new ArgumentNullException(nameof(idPropertyName));
            if (keyExt == null) throw new ArgumentNullException(nameof(keyExt));
            if (keyExtPropertyName == null) throw new ArgumentNullException(nameof(keyExtPropertyName));

            ParameterExpression stateParam = Expression.Parameter(typeof(TGrainState), "state");
            Expression idProperty = Expression.Property(stateParam, idPropertyName);
            Expression keyExtProperty = Expression.Property(stateParam, keyExtPropertyName);


            ConstantExpression grainIdConstant = Expression.Constant(grainId);
            ConstantExpression grainKeyExtConstant = Expression.Constant(keyExt);

            Expression idEqualityExpression = Expression.Equal(idProperty, grainIdConstant);
            Expression keyExtEqualityExpression = Expression.Equal(keyExtProperty, grainKeyExtConstant);

            BinaryExpression expression = Expression.AndAlso(idEqualityExpression, keyExtEqualityExpression);
            return Expression.Lambda<Func<TGrainState, bool>>(expression, stateParam);
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