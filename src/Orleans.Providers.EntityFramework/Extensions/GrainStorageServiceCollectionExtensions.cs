using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework.Extensions
{
    public static class GrainStorageServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureGrainStorageOptions<TContext, TGrain, TEntity>(
            this IServiceCollection services,
            Action<GrainStorageOptions<TContext, TGrain, TEntity>> configureOptions = null)
            where TContext : DbContext
            where TGrain : Grain<TEntity>
            where TEntity : class, new()
        {
            return services
                .AddSingleton<IPostConfigureOptions<GrainStorageOptions<TContext, TGrain, TEntity>>,
                    GrainStoragePostConfigureOptions<TContext, TGrain, TEntity, TEntity>>()
                .Configure<GrainStorageOptions<TContext, TGrain, TEntity>>(typeof(TGrain).FullName, options =>
                {
                    configureOptions?.Invoke(options);
                });
        }

        public static IServiceCollection ConfigureGrainStorageOptions<TContext, TGrain, TGrainState, TEntity>(
            this IServiceCollection services,
            Action<GrainStorageOptions<TContext, TGrain, TEntity>> configureOptions = null)
            where TContext : DbContext
            where TGrain : Grain<TGrainState>
            where TGrainState : new()
            where TEntity : class
        {
            return services
                .AddSingleton<IPostConfigureOptions<GrainStorageOptions<TContext, TGrain, TEntity>>,
                    GrainStoragePostConfigureOptions<TContext, TGrain, TGrainState, TEntity>>()
                .Configure<GrainStorageOptions<TContext, TGrain, TEntity>>(typeof(TGrain).FullName, options =>
                {
                    configureOptions?.Invoke(options);
                });
        }

        public static IServiceCollection AddEfGrainStorage<TContext>(
            this IServiceCollection services,
            string providerName = ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME)
            where TContext : DbContext
        {
            services.TryAddSingleton(typeof(IEntityTypeResolver), typeof(EntityTypeResolver));
            services.TryAddSingleton(typeof(IGrainStorageConvention), typeof(GrainStorageConvention));
            services.AddSingleton(typeof(EntityFrameworkGrainStorage<TContext>));

            services.TryAddSingleton<IGrainStorage>(sp =>
                sp.GetServiceByName<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));

            services.AddSingletonNamedService<IGrainStorage>(providerName,
                (sp, name) => sp.GetRequiredService<EntityFrameworkGrainStorage<TContext>>());

            return services;
        }

    }
}