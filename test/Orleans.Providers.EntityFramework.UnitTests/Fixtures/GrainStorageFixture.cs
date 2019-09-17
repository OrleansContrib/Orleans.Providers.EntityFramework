using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Providers.EntityFramework.Extensions;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Internal;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Runtime;
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

                // Entity framework
                .AddEntityFrameworkInMemoryDatabase()
                .AddDbContextPool<TestDbContext>(builder =>
                {
                    builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
                })
                // Orleans stuff
                .AddSingleton<ITypeResolver, TypeResolver>()

                .ConfigureGrainStorageOptions<TestDbContext, ConfiguredGrainWithCustomGuidKey,
                    ConfiguredEntityWithCustomGuidKey>(
                    options =>
                    {
                        options
                            .UseKey(entity => entity.CustomKey);
                    })

                // Storage
                .AddEfGrainStorage<TestDbContext>()
                .AddSingleton<IGrainStorage, EntityFrameworkGrainStorage<TestDbContext>>()
                .AddSingleton<IGrainStorageConvention, TestGrainStorageConvention>()
                .AddSingleton<IEntityTypeResolver, TestEntityTypeResolver>()

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

    public class TestEntityTypeResolver : EntityTypeResolver
    {
        public override Type ResolveEntityType(string grainType, IGrainState grainState)
        {
            Type stateType = ResolveStateType(grainType, grainState);

            if (stateType == typeof(GrainStateWrapper<EntityWithGuidKey>))
                return typeof(EntityWithGuidKey);

            return stateType;
        }
    }

    public class TestGrainStorageConvention : GrainStorageConvention
    {
        public TestGrainStorageConvention(
            IOptions<GrainStorageConventionOptions> options, IServiceScopeFactory serviceScopeFactory) : base(options, serviceScopeFactory)
        {
        }

        public override Func<IGrainState, TEntity> GetGetterFunc<TGrainState, TEntity>()
        {
            if (typeof(TGrainState) == typeof(GrainStateWrapper<TEntity>))
                return state =>
                    (state.State as GrainStateWrapper<TEntity>)?.Value;

            return stat => stat.State as TEntity;
        }

        public override Action<IGrainState, TEntity> GetSetterFunc<TGrainState, TEntity>()
        {
            if (typeof(TGrainState) == typeof(GrainStateWrapper<TEntity>))
                return (state, entity) =>
                {
                    if (state.State is GrainStateWrapper<TEntity> wrapper)
                        wrapper.Value = entity;
                    else
                        state.State = new GrainStateWrapper<TEntity>()
                        {
                            Value = entity
                        };
                };

            return (state, entity) => state.State = entity;
        }
    }
}