using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Xunit;

namespace Orleans.Providers.EntityFramework.UnitTests.Internal
{
    public static class Utils
    {
        /// <summary>
        /// Fetches an entity from database
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <param name="entity">The object used to extract keys</param>
        public static object FetchEntityFromDb<TKey>(IServiceProvider serviceProvider,
            Entity<TKey> entity)
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

                object actual;
                switch (entity)
                {
                    case Entity<Guid> _:
                        if (entity.HasKeyExt)
                            actual = context.Set<EntityWithGuidCompoundKey>()
                                .FirstOrDefault(e => e.Id.Equals(entity.Id) && e.KeyExt == entity.KeyExt);
                        else
                            actual = context.Set<EntityWithGuidKey>()
                                .FirstOrDefault(e => e.Id.Equals(entity.Id) && e.KeyExt == entity.KeyExt);

                        break;
                    case Entity<long> _:
                        if (entity.HasKeyExt)
                            actual = context.Set<EntityWithIntegerCompoundKey>()
                                .FirstOrDefault(e => e.Id.Equals(entity.Id) && e.KeyExt == entity.KeyExt);
                        else
                            actual = context.Set<EntityWithIntegerKey>()
                                .FirstOrDefault(e => e.Id.Equals(entity.Id) && e.KeyExt == entity.KeyExt);
                        break;
                    case Entity<string> _:
                        actual = context.Set<EntityWithStringKey>()
                            .FirstOrDefault(e => e.Id.Equals(entity.Id) && e.KeyExt == entity.KeyExt);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(entity));

                }

                return actual;
            }
        }

        public static void AssertEntityEqualityVsDb<TKey>(IServiceProvider serviceProvider,
            Entity<TKey> expected)
        {
            object actual = FetchEntityFromDb(serviceProvider, expected);
            Assert.Equal(expected, actual);
        }


        public static GrainState<TEntity> CreateAndStoreGrainState<TEntity>(IServiceProvider serviceProvider)
            where TEntity : class, new()
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                var entity = new TEntity();
                // Somehow ef is ignoring IsPersisted property value conversion
                var d = (dynamic)entity;
                d.IsPersisted = true;
                context.Add(entity);
                context.SaveChanges();
                return new GrainState<TEntity>
                {
                    State = entity
                };
            }
        }
    }
}