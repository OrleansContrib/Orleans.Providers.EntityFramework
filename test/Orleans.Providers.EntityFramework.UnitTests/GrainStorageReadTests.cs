using System;
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
    public class GrainStorageReadTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public GrainStorageReadTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public Task ReadGuidKeyState()
        {
            return TestReadAsync<GrainWithGuidKey, EntityWithGuidKey, Guid>();
        }

        [Fact]
        public Task ReadGuidCompoundKeyState()
        {
            return TestReadAsync<GrainWithGuidCompoundKey, EntityWithGuidCompoundKey, Guid>();
        }

        [Fact]
        public Task ReadIntegerKeyState()
        {
            return TestReadAsync<GrainWithIntegerKey, EntityWithIntegerKey, long>();
        }

        [Fact]
        public Task ReadIntegerCompoundKeyState()
        {
            return TestReadAsync<GrainWithIntegerCompoundKey, EntityWithIntegerCompoundKey, long>();
        }

        [Fact]
        public Task ReadStringKeyState()
        {
            return TestReadAsync<GrainWithStringKey, EntityWithStringKey, string>();
        }

        private async Task TestReadAsync<TGrain, TState, TKey>()
            where TState : Entity<TKey>, new()
            where TGrain : Grain<TState>
        {
            GrainState<TState> grainState = Internal.Utils.CreateAndStoreGrainState<TState>(_serviceProvider);

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            grainState.State = null;

            await _storage.ReadStateAsync(typeof(TGrain).FullName,
                grainRef,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(_serviceProvider, grainState.State);
        }
    }
}