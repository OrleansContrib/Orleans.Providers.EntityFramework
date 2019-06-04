using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework
{
    public class GrainStoragePostConfigureOptions<TContext, TGrain, TGrainState>
        : IPostConfigureOptions<GrainStorageOptions<TContext, TGrain, TGrainState>>
        where TContext : DbContext
        where TGrain : Grain<TGrainState>
        where TGrainState : class, new()
    {
        public IGrainStorageConvention<TContext, TGrain, TGrainState> Convention { get; }
        public IGrainStorageConvention DefaultConvention { get; }

        public GrainStoragePostConfigureOptions(IServiceProvider serviceProvider)
        {
            DefaultConvention =
                (IGrainStorageConvention)serviceProvider.GetRequiredService(typeof(IGrainStorageConvention));
            Convention = (IGrainStorageConvention<TContext, TGrain, TGrainState>)
                serviceProvider.GetService(typeof(IGrainStorageConvention<TContext, TGrain, TGrainState>));
        }

        public void PostConfigure(string name, GrainStorageOptions<TContext, TGrain, TGrainState> options)
        {
            if (!string.Equals(name, typeof(TGrain).FullName))
                throw new Exception("Post configure on wrong grain type.");

            if (options.DbSetAccessor == null)
                options.DbSetAccessor = Convention?.CreateDefaultDbSetAccessorFunc()
                                    ?? DefaultConvention.CreateDefaultDbSetAccessorFunc<TContext, TGrainState>();


            if (Convention != null)
                Convention.SetDefaultKeySelector(options);
            else
                DefaultConvention.SetDefaultKeySelectors(options);


            if (options.IsPersistedFunc == null)
                options.IsPersistedFunc =
                    DefaultConvention.CreateIsPersistedFunc<TGrainState>(options);

            // Configure ETag
            if (options.ShouldUseETag)
            {
                if (!string.IsNullOrWhiteSpace(options.ETagPropertyName))
                    DefaultConvention.ConfigureETag(options.ETagPropertyName, options);
            }

            if (options.ReadStateAsync == null)
            {
                if (options.PreCompileReadQuery)
                {
                    options.ReadStateAsync
                        = Convention?.CreatePreCompiledDefaultReadStateFunc(options)
                          ?? DefaultConvention
                              .CreatePreCompiledDefaultReadStateFunc(options);
                }
                else
                {
                    options.ReadStateAsync
                        = Convention?.CreateDefaultReadStateFunc()
                          ?? DefaultConvention
                              .CreateDefaultReadStateFunc(options);
                }
            }

            DefaultConvention.FindAndConfigureETag(options, options.ShouldUseETag);

            // todo: Validate options

            options.IsConfigured = true;
        }
    }
}