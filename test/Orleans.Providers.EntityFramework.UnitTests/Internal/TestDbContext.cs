using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Update;
using Orleans.Providers.EntityFramework.UnitTests.Models;

namespace Orleans.Providers.EntityFramework.UnitTests.Internal
{
    public class TestDbContext : DbContext
    {
        protected static readonly Random Random = new Random(234);

        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<EntityWithGuidKey> GuidKeyEntities { get; set; }
        public DbSet<EntityWithGuidCompoundKey> GuidCompoundKeyEntities { get; set; }
        public DbSet<EntityWithIntegerKey> IntegerKeyEntities { get; set; }
        public DbSet<EntityWithIntegerCompoundKey> IntegerCompoundKeyEntities { get; set; }
        public DbSet<EntityWithStringKey> StringKeyEntities { get; set; }

        public DbSet<EntityWithIntegerKeyWithEtag> ETagEntities { get; set; }

        public DbSet<ConfiguredEntityWithCustomGuidKey> ConfiguredEntities { get; set; }
        public DbSet<UnconfiguredEntityWithCustomGuidKey> UnconfiguredEntities { get; set; }

        public DbSet<InvalidConfiguredEntityWithCustomGuidKey> InvalidEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<EntityWithGuidKey>()
                .Property(e => e.IsPersisted)
                .HasConversion<bool>(
                    isPersisted => true,
                    value => true);

            builder.Entity<EntityWithGuidCompoundKey>()
                .Property(e => e.IsPersisted)
                .HasConversion<bool>(
                    isPersisted => true,
                    value => true);

            builder.Entity<EntityWithIntegerKey>()
                .Property(e => e.IsPersisted)
                .HasConversion<bool>(
                    isPersisted => true,
                    value => true);

            builder.Entity<EntityWithIntegerCompoundKey>()
                .Property(e => e.IsPersisted)
                .HasConversion<bool>(
                    isPersisted => true,
                    value => true);

            builder.Entity<EntityWithStringKey>()
                .Property(e => e.IsPersisted)
                .HasConversion<bool>(
                    isPersisted => true,
                    value => true);

            builder.Entity<EntityWithIntegerKeyWithEtag>()
                .Property(e => e.IsPersisted)
                .HasConversion<bool>(
                    isPersisted => true,
                    value => true);
            
            builder.Entity<EntityWithIntegerKeyWithEtag>()
                .Property(e => e.ETag)
                .HasConversion<byte[]>(
                    value => BitConverter.GetBytes(Random.Next()),
                    storedValue => storedValue
                )
                .IsConcurrencyToken();

            builder.Entity<EntityWithGuidCompoundKey>()
                .HasKey(e => new
                {
                    e.Id,
                    e.KeyExt
                });
            builder.Entity<EntityWithIntegerCompoundKey>()
                .HasKey(e => new
                {
                    e.Id,
                    e.KeyExt
                });

            builder.Entity<ConfiguredEntityWithCustomGuidKey>()
                .HasKey(e => new
                {
                    e.CustomKey,
                    e.CustomKeyExt
                });

            builder.Entity<UnconfiguredEntityWithCustomGuidKey>()
                .HasKey(e => new
                {
                    e.CustomKey,
                    e.CustomKeyExt
                });

            builder.Entity<InvalidConfiguredEntityWithCustomGuidKey>()
                .HasKey(e => new
                {
                    e.CustomKey,
                    e.CustomKeyExt
                });
        }

        public override int SaveChanges()
        {
            MockConcurrencyChecks();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = new CancellationToken())
        {
            MockConcurrencyChecks();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void MockConcurrencyChecks()
        {
            // Check etags as the in-memory storage doesn't
            foreach (EntityWithIntegerKeyWithEtag entitiy in this.ETagEntities.Local)
            {
                var entry = this.Entry(entitiy);

                if (entry.State != EntityState.Modified)
                    continue;

                EntityWithIntegerKeyWithEtag storedEntity = this.ETagEntities.AsNoTracking()
                    .Single(e => e.Id == entitiy.Id);

                if (!storedEntity.ETag.SequenceEqual(entitiy.ETag))
                    throw new DbUpdateConcurrencyException("ETag violation",
                        entry.GetInfrastructure().StateManager.Entries.Select(
                            e => (IUpdateEntry) e).ToList());

                entitiy.ETag = BitConverter.GetBytes(Random.Next());
            }
        }
    }
}