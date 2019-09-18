using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Extensions
{
    public static class GrainStorageOptionsExtensions
    {
        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseQuery<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Func<TContext, IQueryable<TGrainState>> queryFunc)
            where TContext : DbContext
            where TGrainState : class
        {
            options.DbSetAccessor = queryFunc;
            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> ConfigureIsPersisted<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Func<TGrainState, bool> isPersistedFunc)
            where TContext : DbContext
            where TGrainState : class
        {
            options.IsPersistedFunc = isPersistedFunc;
            return options;
        }

        /// <summary>
        /// Instructs the storage provider to precompile read query.
        /// This will lead to better performance for complex queries.
        /// Default is to precompile.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrain"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <param name="options"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static GrainStorageOptions<TContext, TGrain, TGrainState> PreCompileReadQuery<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            bool value = true)
            where TContext : DbContext
            where TGrainState : class
        {
            options.PreCompileReadQuery = value;
            return options;
        }


        /// <summary>
        /// Overrides the default implementation used to query grain state from database.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <typeparam name="TGrain"></typeparam>
        /// <param name="options"></param>
        /// <param name="readStateAsyncFunc"></param>
        /// <returns></returns>
        public static GrainStorageOptions<TContext, TGrain, TGrainState> ConfigureReadState<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Func<TContext, IAddressable, Task<TGrainState>> readStateAsyncFunc)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.ReadStateAsync = readStateAsyncFunc ?? throw new ArgumentNullException(nameof(readStateAsyncFunc));
            return options;
        }


        /// <summary>
        /// Instruct the storage that the current entity should use etags.
        /// If no valid properties were found on the entity and exception would be thrown.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <typeparam name="TGrain"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseETag<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options)
            where TContext : DbContext
            where TGrainState : class
        {
            options.ShouldUseETag = true;
            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseETag<TContext, TGrain, TGrainState, TProperty>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Expression<Func<TGrainState, TProperty>> expression)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.ETagPropertyName = memberExpression.Member.Name;
            options.ShouldUseETag = true;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseETag<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            string propertyName)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            options.ETagPropertyName = propertyName;
            options.ShouldUseETag = true;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseKey<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Expression<Func<TGrainState, Guid>> expression)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.KeyPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseKey<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Expression<Func<TGrainState, long>> expression)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new GrainStorageConfigurationException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.KeyPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseKey<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Expression<Func<TGrainState, string>> expression)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.KeyPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseKey<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            string propertyName)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            options.KeyPropertyName = propertyName;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseKeyExt<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Expression<Func<TGrainState, string>> expression)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.KeyExtPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> UseKeyExt<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            string propertyName)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            options.KeyExtPropertyName = propertyName;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> CheckPersistenceOn<TContext, TGrain, TGrainState, TProperty>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            Expression<Func<TGrainState, TProperty>> expression)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.PersistenceCheckPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrain, TGrainState> CheckPersistenceOn<TContext, TGrain, TGrainState>(
            this GrainStorageOptions<TContext, TGrain, TGrainState> options,
            string propertyName)
            where TContext : DbContext
            where TGrainState : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            options.PersistenceCheckPropertyName = propertyName;

            return options;
        }
    }
}