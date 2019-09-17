using Microsoft.EntityFrameworkCore;
using Orleans.Hosting;

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
                    services.AddEfGrainStorage<TContext>(providerName);
                });
        }
    }
}