using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;

namespace Orleans.Providers.EntityFramework
{
    public class GrainStoragePostConfigureOptions<TContext, TGrain, TGrainState>
        : IPostConfigureOptions<GrainStorageOptions<TContext, TGrain, TGrainState>>
        where TContext : DbContext
        where TGrain : Grain<TGrainState>
        where TGrainState : class, new()
    {
        public IGrainStorageConvention<TContext, TGrainState> Convention { get; }
        public IGrainStorageConvention DefaultConvention { get; }

        public GrainStoragePostConfigureOptions(IServiceProvider serviceProvider)
        {
            DefaultConvention =
                (IGrainStorageConvention)serviceProvider.GetRequiredService(typeof(IGrainStorageConvention));
            Convention = (IGrainStorageConvention<TContext, TGrainState>)
                serviceProvider.GetService(typeof(IGrainStorageConvention<TContext, TGrainState>));
        }

        public void PostConfigure(string name, GrainStorageOptions<TContext, TGrain, TGrainState> options)
        {
            if (!string.Equals(name, typeof(TGrain).FullName))
                throw new Exception("Post configure on wrong grain type.");

            if (options.ReadQuery == null)
                options.ReadQuery = Convention?.CreateDefaultQueryFunc()
                                    ?? DefaultConvention.CreateDefaultQueryFunc<TContext, TGrainState>();

            if (options.QueryExpressionGeneratorFunc == null)
                options.QueryExpressionGeneratorFunc
                    = Convention?.CreateGrainStateQueryExpressionGeneratorFunc()
                      ?? DefaultConvention
                          .CreateDefaultGrainStateQueryExpressionGeneratorFunc<TGrain, TGrainState>(options);

            if (options.IsPersistedFunc == null)
                options.IsPersistedFunc =
                    DefaultConvention.CreateIsPersistedFunc<TGrainState>(options);

            // Configure ETag
            if (options.ShouldUseETag)
            {
                if (!string.IsNullOrWhiteSpace(options.ETagPropertyName))
                    DefaultConvention.ConfigureETag(options.ETagPropertyName, options);
            }

            DefaultConvention.FindAndConfigureETag(options, options.ShouldUseETag);

            // todo: Validate options

            options.IsConfigured = true;
        }
    }
}