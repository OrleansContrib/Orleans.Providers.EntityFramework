using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;

namespace Orleans.Providers.EntityFramework
{
    public class PostConfigureGrainStorageOptions<TContext, TGrain, TGrainState>
        : IPostConfigureOptions<GrainStorageOptions<TContext, TGrainState>>
        where TContext : DbContext
        where TGrain : Grain<TGrainState>
        where TGrainState : class, new()
    {
        public IGrainStorageConvention<TContext, TGrainState> Convention { get; }
        public IGrainStorageConvention DefaultConvention { get; }

        public PostConfigureGrainStorageOptions(IServiceProvider serviceProvider)
        {
            DefaultConvention =
                (IGrainStorageConvention)serviceProvider.GetRequiredService(typeof(IGrainStorageConvention));
            Convention = (IGrainStorageConvention<TContext, TGrainState>)
                serviceProvider.GetService(typeof(IGrainStorageConvention<TContext, TGrainState>));
        }

        public void PostConfigure(string name, GrainStorageOptions<TContext, TGrainState> options)
        {
            if (options.ReadQuery == null)
                options.ReadQuery = Convention?.CreateDefaultQueryFunc()
                                    ?? DefaultConvention.CreateDefaultQueryFunc<TContext, TGrainState>();

            if (options.QueryExpressionGeneratorFunc == null)
                options.QueryExpressionGeneratorFunc
                    = Convention?.CreateGrainStateQueryExpressionGeneratorFunc()
                      ?? DefaultConvention
                          .CreateDefaultGrainStateQueryExpressionGeneratorFunc<TGrain, TGrainState>();

            if (options.IsPersistedFunc == null)
                options.IsPersistedFunc =
                    DefaultConvention.CreateIsPersistedFunc<TGrainState>();
        }
    }
}