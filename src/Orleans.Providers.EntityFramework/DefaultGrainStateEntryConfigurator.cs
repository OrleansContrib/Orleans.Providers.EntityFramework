using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Orleans.Providers.EntityFramework
{
    public class DefaultGrainStateEntryConfigurator<TContext, TGrain, TEntity>
        : IGrainStateEntryConfigurator<TContext, TGrain, TEntity>
        where TContext : DbContext
        where TEntity : class
    {
        public void ConfigureSaveEntry(ConfigureSaveEntryContext<TContext, TEntity> context)
        {
            EntityEntry<TEntity> entry = context.DbContext.Entry(context.Entity);

            entry.State = context.IsPersisted
                ? EntityState.Modified
                : EntityState.Added;
        }
    }
}