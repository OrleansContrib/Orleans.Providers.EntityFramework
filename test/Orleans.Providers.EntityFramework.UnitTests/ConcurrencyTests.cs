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
        public async Task WriteWithETagViolation()
        {
            GrainState<EntityWithIntegerKeyWithEtag> grainState =
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
            GrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.CreateAndStoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider);

            TestGrainReference grainRef
                = TestGrainReference.Create(grainState.State);

            grainState.State.Title = "Updated";

            await _storage.WriteStateAsync(typeof(GrainWithIntegerKeyWithEtag).FullName,
                grainRef,
                grainState);

        }
    }
}