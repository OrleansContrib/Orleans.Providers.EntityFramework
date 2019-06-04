using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Hosting;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework.Extensions
{
    public static class GrainStorageSiloHostBuilderExtensions
    {
        public static ISiloHostBuilder AddEfGrainStorageAsDefault<TContext>(this ISiloHostBuilder builder)
            where TContext : DbContext
        {
            return builder.AddEfGrainStorage<TContext>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME);
        }

        public static ISiloHostBuilder AddEfGrainStorage<TContext>(this ISiloHostBuilder builder,
            string providerName)
            where TContext : DbContext
        {

            return builder
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IGrainStorageConvention, GrainStorageConventionV2>()
                        .AddSingleton<EntityFrameworkGrainStorage<TContext>>();

                    services.TryAddSingleton<IGrainStorage>(sp =>
                        sp.GetServiceByName<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
                    services.AddSingletonNamedService<IGrainStorage>(providerName,
                        (sp, name) => sp.GetRequiredService<EntityFrameworkGrainStorage<TContext>>());
                });
        }
    }
}