using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Conventions
{
    public interface IGrainStorageConvention
    {
        /// <summary>
        /// Creates a method that returns an IQueryable'<typeparam name="TGrainState"></typeparam>
        ///  against <typeparam name="TContext"></typeparam> type.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <returns></returns>
        Func<TContext, IQueryable<TGrainState>>
            CreateDefaultQueryFunc<TContext, TGrainState>()
            where TContext : DbContext
            where TGrainState : class, new();


        /// <summary>
        /// Creates a method that generates an expression to be used by entity framework to
        /// fetch a single state using default property (Id). The default implementation 
        /// behaiviour is configurable using <see cref="GrainStorageConventionOptions"/>.
        /// </summary>
        /// <typeparam name="TGrain"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <returns></returns>
        Func<IAddressable, Expression<Func<TGrainState, bool>>>
            CreateDefaultGrainStateQueryExpressionGeneratorFunc<TGrain, TGrainState>()
            where TGrain : Grain<TGrainState>
            where TGrainState : new();

        /// <summary>
        /// Creates a method that generates an expression to be used by entity framework to 
        /// fetch a single state for a GUID or long keyed grain.
        /// </summary>
        /// <typeparam name="TGrainState">Type of grain state</typeparam>
        /// <param name="getGrainIdFunc">Function returing the Id of the state.</param>
        /// <param name="stateIdPropertyName">Name of the Id property.</param>
        /// <returns></returns>
        Func<IAddressable, Expression<Func<TGrainState, bool>>>
            CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                Func<IAddressable, ValueType> getGrainIdFunc,
                string stateIdPropertyName);

        /// <summary>
        /// Creates a method that generates an expression to be used by entity framework to 
        /// fetch a single state for a string keyed grain.
        /// </summary>
        /// <typeparam name="TGrainState">Type of grain state</typeparam>
        /// <param name="getGrainIdFunc">Function returing the Id of the state.</param>
        /// <param name="stateIdPropertyName">Name of the Id property.</param>
        /// <returns></returns>
        Func<IAddressable, Expression<Func<TGrainState, bool>>>
            CreateGrainStateQueryExpressionGeneratorFunc<TGrainState>(
                Func<IAddressable, string> getGrainIdFunc,
                string stateIdPropertyName);

        // todo: support composite key grains

        /// <summary>
        /// Creates a method that determines if a state object is persisted in the database.
        /// This is used to decide wether an insert or an update operation is needed.
        /// </summary>
        /// <typeparam name="TGrainState"></typeparam>
        /// <returns></returns>
        Func<TGrainState, bool> CreateIsPersistedFunc<TGrainState>();

        /// <summary>
        /// Tries to find and configure an ETag property on the state model
        /// </summary>
        /// <param name="options"></param>
        /// <param name="throwIfNotFound">Indicates if failure of finding an ETag property should throw</param>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        void FindAndConfigureETag<TContext,TGrainState>(GrainStorageOptions<TContext, TGrainState> options,
            bool throwIfNotFound)
            where TContext : DbContext;

        /// <summary>
        /// Confitures the ETag property using the provided propery name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="options"></param>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        void ConfigureETag<TContext, TGrainState>(string propertyName,GrainStorageOptions<TContext, TGrainState> options)
            where TContext : DbContext;
    }

    public interface IGrainStorageConvention<in TContext, TGrainState>
        where TContext : DbContext
        where TGrainState : class, new()
    {
        /// <summary>
        /// Creates a method that returns an IQueryable'<typeparam name="TGrainState"></typeparam>
        ///  against <typeparam name="TContext"></typeparam> type.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TGrainState"></typeparam>
        /// <returns></returns>
        Func<TContext, IQueryable<TGrainState>>
            CreateDefaultQueryFunc();

        /// <summary>
        /// Creates a method that generates an expression to be used by entity framework to 
        /// fetch a single state.
        /// </summary>
        /// <typeparam name="TGrainState">Type of grain state</typeparam>
        /// <returns></returns>
        Func<IAddressable, Expression<Func<TGrainState, bool>>>
            CreateGrainStateQueryExpressionGeneratorFunc();
    }
}