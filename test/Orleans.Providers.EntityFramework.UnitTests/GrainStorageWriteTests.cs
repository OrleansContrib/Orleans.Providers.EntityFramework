using System;
using System.Threading.Tasks;
using Orleans.Providers.EntityFramework.UnitTests.Fixtures;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Internal;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Storage;
using Xunit;

namespace Orleans.Providers.EntityFramework.UnitTests
{
    [Collection(GrainStorageCollection.Name)]
    public class GrainStorageWriteTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public GrainStorageWriteTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public Task WriteGuidKeyState()
        {
            return TestWriteAsync<GrainWithGuidKey, EntityWithGuidKey, Guid>();
        }

        [Fact]
        public Task WriteGuidCompoundKeyState()
        {
            return TestWriteAsync<GrainWithGuidCompoundKey, EntityWithGuidCompoundKey, Guid>();
        }

        [Fact]
        public Task WriteIntegerKeyState()
        {
            return TestWriteAsync<GrainWithIntegerKey, EntityWithIntegerKey, long>();
        }

        [Fact]
        public Task WriteIntegerCompoundKeyState()
        {
            return TestWriteAsync<GrainWithIntegerCompoundKey, EntityWithIntegerCompoundKey, long>();
        }

        [Fact]
        public Task WriteStringKeyState()
        {
            return TestWriteAsync<GrainWithStringKey, EntityWithStringKey, string>();
        }

        [Fact]
        public async Task WriteCustomGetterGrainState()
        {
            var entity = new EntityWithGuidKey();
            var state = new GrainStateWrapper<EntityWithGuidKey>()
            {
                Value = entity
            };

            var grainState = new TestGrainState<GrainStateWrapper<EntityWithGuidKey>>()
            {
                State = state
            };

            TestGrainReference grainRef
                = TestGrainReference.Create(entity);

            await _storage.WriteStateAsync(typeof(GrainWithCustomStateGuidKey).FullName,
                grainRef,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(
                _serviceProvider, grainState.State?.Value);

        }

        private async Task TestWriteAsync<TGrain, TState, TKey>()
            where TState : Entity<TKey>, new()
            where TGrain : Grain<TState>
        {
            TestGrainState<TState> grainState = CreateGrainState<TState>();

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            await _storage.WriteStateAsync(typeof(TGrain).FullName,
                grainRef,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(_serviceProvider, grainState.State);
        }

        private static TestGrainState<TEntity> CreateGrainState<TEntity>()
            where TEntity : class, new()
        {
            return new TestGrainState<TEntity>
            {
                State = new TEntity()
            };
        }
    }
}