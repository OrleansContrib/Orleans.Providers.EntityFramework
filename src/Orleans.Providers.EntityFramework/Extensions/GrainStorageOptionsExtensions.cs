using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Extensions
{
    public static class GrainStorageOptionsExtensions
    {
        public static GrainStorageOptions<TContext, TGrainState> UseQuery<TContext, TGrainState>(
            this GrainStorageOptions<TContext, TGrainState> options,
            Func<TContext, IQueryable<TGrainState>> queryFunc)
            where TContext : DbContext
            where TGrainState : class, new()
        {
            options.ReadQuery = queryFunc;
            return options;
        }

        public static GrainStorageOptions<TContext, TGrainState> ConfigureIsPersisted<TContext, TGrainState>(
            this GrainStorageOptions<TContext, TGrainState> options,
            Func<TGrainState, bool> isPersistedFunc)
            where TContext : DbContext
            where TGrainState : class, new()
        {
            options.IsPersistedFunc = isPersistedFunc;
            return options;
        }

        /// <summary>
        /// Configures the expression used to query grain state from database.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <param name="options"></param>
        /// <param name="expressionFunc"></param>
        /// <returns></returns>
        public static GrainStorageOptions<TContext, TGrainState> UseQueryExpression<TContext, TGrainState>(
            this GrainStorageOptions<TContext, TGrainState> options,
            Func<IAddressable, Expression<Func<TGrainState, bool>>> expressionFunc)
            where TContext : DbContext
            where TGrainState : class, new()
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expressionFunc == null) throw new ArgumentNullException(nameof(expressionFunc));

            options.QueryExpressionGeneratorFunc = expressionFunc;
            return options;
        }


        /// <summary>
        /// Instruct the storage that the current entity should use etags.
        /// If no valid properties were found on the entity and exception would be thrown.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static GrainStorageOptions<TContext, TGrainState> UseETag<TContext, TGrainState>(
            this GrainStorageOptions<TContext, TGrainState> options)
            where TContext : DbContext
            where TGrainState : class, new()
        {
            options.ShouldUseETag = true;
            return options;
        }

        public static GrainStorageOptions<TContext, TGrainState> UseETag<TContext, TGrainState,TProperty>(
            this GrainStorageOptions<TContext, TGrainState> options,
            Expression<Func<TGrainState, TProperty>> expression)
            where TContext : DbContext
            where TGrainState : class, new()
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new InvalidEnumArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.ETagPropertyName = memberExpression.Member.Name;
            options.ShouldUseETag = true;

            return options;
        }

        public static GrainStorageOptions<TContext, TGrainState> UseETag<TContext, TGrainState>(
            this GrainStorageOptions<TContext, TGrainState> options,
            string propertyName)
            where TContext : DbContext
            where TGrainState : class, new()
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            options.ETagPropertyName = propertyName;
            options.ShouldUseETag = true;

            return options;
        }
    }
}