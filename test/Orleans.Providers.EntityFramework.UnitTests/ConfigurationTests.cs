using System;
using System.Threading.Tasks;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Providers.EntityFramework.UnitTests.Fixtures;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Internal;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Storage;
using Xunit;

namespace Orleans.Providers.EntityFramework.UnitTests
{
    [Collection(GrainStorageCollection.Name)]
    public class ConfigurationTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public ConfigurationTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public async Task ReadConfiguredCustomKeyStateShouldPass()
        {

            TestGrainState<ConfiguredEntityWithCustomGuidKey> grainState =
                Internal.Utils.CreateAndStoreGrainState<ConfiguredEntityWithCustomGuidKey>(_serviceProvider);


            TestGrainReference grainRef
                = TestGrainReference.Create<ConfiguredGrainWithCustomGuidKey>(
                    grainState.State.CustomKey);


            await _storage.ReadStateAsync(typeof(ConfiguredGrainWithCustomGuidKey).FullName,
                grainRef,
                grainState);
        }
        [Fact]
        public async Task ReadConfiguredCustomKeyStateShouldPassForGrainsWithSameStateType()
        {

            TestGrainState<ConfiguredEntityWithCustomGuidKey> grainState =
                Internal.Utils.CreateAndStoreGrainState<ConfiguredEntityWithCustomGuidKey>(_serviceProvider);


            TestGrainReference grainRef
                = TestGrainReference.Create<ConfiguredGrainWithCustomGuidKey2>(
                    grainState.State.CustomKey, grainState.State.CustomKeyExt);


            await _storage.ReadStateAsync(typeof(ConfiguredGrainWithCustomGuidKey2).FullName,
                grainRef,
                grainState);
        }

        [Fact]
        public async Task ReadUnconfiguredCustomKeyStateShouldFail()
        {

            TestGrainState<UnconfiguredEntityWithCustomGuidKey> grainState =
                Internal.Utils.CreateAndStoreGrainState<UnconfiguredEntityWithCustomGuidKey>(_serviceProvider);

            TestGrainReference grainRef
                = TestGrainReference.Create<UnconfiguredGrainWithCustomGuidKey>(
                    grainState.State.CustomKey, grainState.State.CustomKeyExt);

            await Assert.ThrowsAsync<GrainStorageConfigurationException>(() => _storage.ReadStateAsync(
                typeof(UnconfiguredGrainWithCustomGuidKey).FullName,
                grainRef,
                grainState));
        }

        [Fact]
        public async Task ReadInvalidConfiguredCustomKeyStateShouldFail()
        {

            TestGrainState<InvalidConfiguredEntityWithCustomGuidKey> grainState =
                Internal.Utils.CreateAndStoreGrainState<InvalidConfiguredEntityWithCustomGuidKey>(_serviceProvider);

            TestGrainReference grainRef
                = TestGrainReference.Create<InvalidConfiguredGrainWithGuidKey>(0);

            await Assert.ThrowsAsync<GrainStorageConfigurationException>(() => _storage.ReadStateAsync(
                typeof(InvalidConfiguredGrainWithGuidKey).FullName,
                grainRef,
                grainState));
        }
    }
}