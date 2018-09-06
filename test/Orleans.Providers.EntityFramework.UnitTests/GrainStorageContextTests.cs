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
    public class GrainStorageContextTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public GrainStorageContextTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public async Task SinglePropertyWrite()
        {
            GrainState<EntityWithIntegerKey> grainState =
                Internal.Utils.CreateAndStoreGrainState<EntityWithIntegerKey>(_serviceProvider);


            grainState.State.Title = "Should get updated";
            grainState.State.KeyExt = "Should not get updated";


            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            GrainStorageContext<EntityWithIntegerKey>.ConfigureEntryState(
                entry => entry
                    .Property(e => e.Title)
                    .IsModified = true
            );

            await _storage.WriteStateAsync(typeof(GrainWithIntegerKey).FullName,
            grainRef,
            grainState);


            var actual = (EntityWithIntegerKey)
                Internal.Utils.FetchEntityFromDb(_serviceProvider, grainState.State);

            Assert.Equal("Should get updated", actual?.Title);
            Assert.NotEqual("Should not get updated", actual?.KeyExt);
        }
    }
}