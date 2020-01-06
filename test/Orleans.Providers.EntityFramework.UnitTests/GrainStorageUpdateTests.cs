using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.EntityFramework.UnitTests.Fixtures;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Internal;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Storage;
using Xunit;

namespace Orleans.Providers.EntityFramework.UnitTests
{
    [Collection(GrainStorageCollection.Name)]
    public class GrainStorageUpdateTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public GrainStorageUpdateTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public Task UpdateGuidKeyState()
        {
            return TestUpdateAsync<GrainWithGuidKey, EntityWithGuidKey, Guid>();

        }

        [Fact]
        public Task UpdateGuidCompoundKeyState()
        {
            return TestUpdateAsync<GrainWithGuidCompoundKey, EntityWithGuidCompoundKey, Guid>();
        }

        [Fact]
        public Task UpdateIntegerKeyState()
        {
            return TestUpdateAsync<GrainWithIntegerKey, EntityWithIntegerKey, long>();
        }

        [Fact]
        public Task UpdateIntegerCompoundKeyState()
        {
            return TestUpdateAsync<GrainWithIntegerCompoundKey, EntityWithIntegerCompoundKey, long>();

        }

        [Fact]
        public Task UpdateStringKeyState()
        {
            return TestUpdateAsync<GrainWithStringKey, EntityWithStringKey, string>();
        }

        [Fact]
        public async Task UpdateCustomGetterGrainState()
        {
            var entity = new EntityWithGuidKey();
            Internal.Utils.StoreGrainState(_serviceProvider, entity);
            entity.Title += "UPDATED";
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

        private async Task TestUpdateAsync<TGrain, TState, TKey>()
            where TState : Entity<TKey>, new()
            where TGrain : Grain<TState>
        {
            TestGrainState<TState> grainState = Internal.Utils.CreateAndStoreGrainState<TState>(_serviceProvider);
            grainState.State.Title += "UPDATED";

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            await _storage.WriteStateAsync(typeof(TGrain).FullName,
                grainRef,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(_serviceProvider, grainState.State);
        }
    }
}