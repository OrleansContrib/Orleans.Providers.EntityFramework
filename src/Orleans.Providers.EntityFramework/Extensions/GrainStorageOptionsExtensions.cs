using System;
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
            options.QueryExpressionGeneratorFunc = expressionFunc;
            return options;
        }
    }
}