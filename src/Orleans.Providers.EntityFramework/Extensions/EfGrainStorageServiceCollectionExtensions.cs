using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Orleans.Providers.EntityFramework.Extensions
{
    public static class EfGrainStorageServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureGrainStorageOptions<TContext, TGrain, TGrainState>(
            this IServiceCollection services,
            Action<GrainStorageOptions<TContext, TGrainState>> configureOptions = null)
            where TContext : DbContext
            where TGrain : Grain<TGrainState>
            where TGrainState : class, new()
        {
            return services
                .AddSingleton<IPostConfigureOptions<GrainStorageOptions<TContext, TGrainState>>,
                    PostConfigureGrainStorageOptions<TContext, TGrain, TGrainState>>()
                .Configure<GrainStorageOptions<TContext, TGrainState>>(typeof(TGrain).FullName, options =>
                {
                    configureOptions?.Invoke(options);
                });
        }


    }
}