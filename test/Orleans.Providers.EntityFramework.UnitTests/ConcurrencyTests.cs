using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    public class ConcurrencyTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public ConcurrencyTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public async Task StateShoudContainETag()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.CreateAndStoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider);

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            await _storage.ReadStateAsync(typeof(GrainWithIntegerKeyWithEtag).FullName,
                grainRef,
                grainState);

            string expected = BitConverter.ToString(grainState.State.ETag)
                .Replace("-", string.Empty);

            Assert.Equal(expected, grainState.ETag);
        }


        [Fact]
        public async Task WriteWithETagViolation()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.CreateAndStoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider);

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            // update the database
            EntityWithIntegerKeyWithEtag clone = grainState.State.Clone();
            clone.Title = "Updated";
            using (var context = _serviceProvider.GetRequiredService<TestDbContext>())
            {
                context.Entry(clone).State = EntityState.Modified;

                context.SaveChanges();
            }

            // This should fail
            grainState.State.Title = "Failing Update";
            await Assert.ThrowsAsync<InconsistentStateException>(() =>
                _storage.WriteStateAsync(typeof(GrainWithIntegerKeyWithEtag).FullName,
                    grainRef,
                    grainState));
        }

        [Fact]
        public async Task WriteWithETagSuccess()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.CreateAndStoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider);

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            grainState.State.Title = "Updated";

            await _storage.WriteStateAsync(typeof(GrainWithIntegerKeyWithEtag).FullName,
                grainRef,
                grainState);

            string expected = BitConverter.ToString(grainState.State.ETag)
                .Replace("-", string.Empty);

            Assert.Equal(expected, grainState.ETag);
        }

        [Fact]
        public async Task ReadTaggedEntityShouldSuccessForNullState()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                new TestGrainState<EntityWithIntegerKeyWithEtag>();

            TestGrainReference grainRef
                = TestGrainReference.Create<GrainWithIntegerKeyWithEtag>(0);

            await _storage.ReadStateAsync(typeof(GrainWithIntegerKeyWithEtag).FullName,
                grainRef,
                grainState);

            Assert.Null(grainState.ETag);
        }

        [Fact]
        public async Task ReadTaggedEntityShouldSuccessForNullEtag()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.StoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider,
                new EntityWithIntegerKeyWithEtag
                {
                    ETag = null
                });

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            await _storage.ReadStateAsync(typeof(GrainWithIntegerKeyWithEtag).FullName,
                grainRef,
                grainState);

            Assert.Null(grainState.ETag);
        }
    }
}