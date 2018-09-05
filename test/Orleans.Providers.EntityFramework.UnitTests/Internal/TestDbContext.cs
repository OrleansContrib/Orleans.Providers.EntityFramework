using Microsoft.EntityFrameworkCore;
using Orleans.Providers.EntityFramework.UnitTests.Models;

namespace Orleans.Providers.EntityFramework.UnitTests.Internal
{
    public class TestDbContext : DbContext
    {

        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<EntityWithGuidKey> GuidKeyEntities { get; set; }
        public DbSet<EntityWithGuidCompoundKey> GuidCompoundKeyEntities { get; set; }
        public DbSet<EntityWithIntegerKey> IntegerKeyEntities { get; set; }
        public DbSet<EntityWithIntegerCompoundKey> IntegerCompoundKeyEntities { get; set; }
        public DbSet<EntityWithStringKey> StringKeyEntities { get; set; }

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

            builder.Entity<EntityWithGuidCompoundKey>()
                .HasKey(e => new
                {
                    e.Id,
                    ExtKey = e.KeyExt
                });
            builder.Entity<EntityWithIntegerCompoundKey>()
                .HasKey(e => new
                {
                    e.Id,
                    ExtKey = e.KeyExt
                });
        }

    }
}