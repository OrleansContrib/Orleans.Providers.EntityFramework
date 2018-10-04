using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Providers.EntityFramework.Extensions;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Internal;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework.UnitTests.Fixtures
{
    public class GrainStorageFixture
    {
        public IServiceProvider ServiceProvider { get; }
        public IGrainStorage Storage { get; }

        public GrainStorageFixture()
        {
            var services = new ServiceCollection();

            services
                .AddEntityFrameworkInMemoryDatabase()
                .AddDbContextPool<TestDbContext>(builder =>
                {
                    builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
                })

                .AddSingleton<IGrainStorageConvention, GrainStorageConvention>()
                // Simple key grains with default key properties on state model
                // should work out of the box without configuration.

                .ConfigureGrainStorageOptions<TestDbContext, ConfiguredGrainWithCustomGuidKey,
                    ConfiguredEntityWithCustomGuidKey>(
                    options => options
                        .UseKey(entity => entity.CustomKey))

                .ConfigureGrainStorageOptions<TestDbContext, ConfiguredGrainWithCustomGuidKey2,
                    ConfiguredEntityWithCustomGuidKey>(
                    options => options
                        .UseKey(entity => entity.CustomKey)
                        .UseKeyExt(entity => entity.CustomKeyExt))

                .ConfigureGrainStorageOptions<TestDbContext, InvalidConfiguredGrainWithGuidKey,
                    InvalidConfiguredEntityWithCustomGuidKey>(
                    options => options
                        .UseKey(entity => entity.CustomKey)
                        .UseKeyExt(entity => entity.CustomKeyExt))

                .AddSingleton<IGrainStorage, EntityFrameworkGrainStorage<TestDbContext>>()
                .Configure<GrainStorageConventionOptions>(options =>
                {
                    options.DefaultGrainKeyPropertyName = nameof(EntityWithGuidKey.Id);
                    options.DefaultGrainKeyExtPropertyName = nameof(EntityWithGuidKey.KeyExt);
                    options.DefaultPersistenceCheckPropertyName = nameof(EntityWithGuidKey.IsPersisted);
                });

            ServiceProvider = services.BuildServiceProvider();

            Storage = ServiceProvider.GetRequiredService<IGrainStorage>();


            using (IServiceScope scope = ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                // this is required to make sure data are seeded
                context.Database.EnsureCreated();
            }
        }
    }
}