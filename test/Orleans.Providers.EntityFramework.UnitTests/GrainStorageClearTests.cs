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
    public class GrainStorageClearTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public GrainStorageClearTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public Task ClearGuidKeyState()
        {
            return TestClearAsync<GrainWithGuidKey, EntityWithGuidKey, Guid>();

        }

        [Fact]
        public Task ClearGuidCompoundKeyState()
        {
            return TestClearAsync<GrainWithGuidCompoundKey, EntityWithGuidCompoundKey, Guid>();
        }

        [Fact]
        public Task ClearIntegerKeyState()
        {
            return TestClearAsync<GrainWithIntegerKey, EntityWithIntegerKey, long>();
        }

        [Fact]
        public Task ClearIntegerCompoundKeyState()
        {
            return TestClearAsync<GrainWithIntegerCompoundKey, EntityWithIntegerCompoundKey, long>();

        }

        [Fact]
        public Task ClearStringKeyState()
        {
            return TestClearAsync<GrainWithStringKey, EntityWithStringKey, string>();
        }


        private async Task TestClearAsync<TGrain, TState, TKey>()
            where TState : Entity<TKey>, new()
            where TGrain : Grain<TState>
        {
            TestGrainState<TState> grainState = Internal.Utils.CreateAndStoreGrainState<TState>(_serviceProvider);

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            await _storage.ClearStateAsync(typeof(TGrain).FullName,
                grainRef,
                grainState
            );

            var actual = Internal.Utils.FetchEntityFromDb(_serviceProvider, grainState.State);
            Assert.Null(actual);
        }
    }
}