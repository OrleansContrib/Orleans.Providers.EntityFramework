using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Conventions
{
    public interface IGrainStorageConvention
    {
        /// <summary>
        /// Creates a method that returns an IQueryable'<typeparam name="TEntity"></typeparam>
        ///  against <typeparam name="TContext"></typeparam> type.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        Func<TContext, IQueryable<TEntity>>
            CreateDefaultDbSetAccessorFunc<TContext, TEntity>()
            where TContext : DbContext
            where TEntity : class;

        Func<TContext, IAddressable, Task<TEntity>>
            CreateDefaultReadStateFunc<TContext, TGrain, TEntity>(
                GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext;

        Func<TContext, IAddressable, Task<TEntity>>
            CreatePreCompiledDefaultReadStateFunc<TContext, TGrain, TEntity>(
                GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext;

        void SetDefaultKeySelectors<TContext, TGrain, TEntity>(
            GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext;

        // todo: support composite key grains

        /// <summary>
        /// Creates a method that determines if a state object is persisted in the database.
        /// This is used to decide whether an insert or an update operation is needed.
        /// </summary>
        /// <param name="options"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        Func<TEntity, bool> CreateIsPersistedFunc<TEntity>(GrainStorageOptions options);

        /// <summary>
        /// Tries to find and configure an ETag property on the state model
        /// </summary>
        /// <param name="options"></param>
        /// <param name="throwIfNotFound">Indicates if failure of finding an ETag property should throw</param>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TGrain"></typeparam>
        void FindAndConfigureETag<TContext, TGrain, TEntity>(
            GrainStorageOptions<TContext, TGrain, TEntity> options,
            bool throwIfNotFound)
            where TContext : DbContext;

        /// <summary>
        /// Configures the ETag property using the provided property name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="options"></param>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TGrain"></typeparam>
        void ConfigureETag<TContext, TGrain, TEntity>(
            string propertyName,
            GrainStorageOptions<TContext, TGrain, TEntity> options)
            where TContext : DbContext;
    }

    public interface IGrainStorageConvention<TContext, TGrain, TEntity>
        where TContext : DbContext
        where TEntity : class
    {
        /// <summary>
        /// Creates a method that returns an IQueryable'<typeparam name="TGrainState"></typeparam>
        ///  against <typeparam name="TContext"></typeparam> type.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <returns></returns>
        Func<TContext, IQueryable<TEntity>>
            CreateDefaultDbSetAccessorFunc();

        /// <summary>
        /// Creates a method that generates an expression to be used by entity framework to 
        /// fetch a single state.
        /// </summary>
        /// <typeparam name="TGrainState">Type of grain state</typeparam>
        /// <returns></returns>
        Func<IAddressable, Expression<Func<TEntity, bool>>>
            CreateGrainStateQueryExpressionGeneratorFunc();

        Func<TContext, IAddressable, Task<TEntity>> CreateDefaultReadStateFunc();

        Func<TContext, IAddressable, Task<TEntity>> CreatePreCompiledDefaultReadStateFunc(
             GrainStorageOptions<TContext, TGrain, TEntity> options);

        void SetDefaultKeySelector(GrainStorageOptions<TContext, TGrain, TEntity> options);
    }
}