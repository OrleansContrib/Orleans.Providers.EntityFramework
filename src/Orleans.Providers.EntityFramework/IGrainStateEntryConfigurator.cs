using Microsoft.EntityFrameworkCore;

namespace Orleans.Providers.EntityFramework
{
    public interface IGrainStateEntryConfigurator<TContext, TGrain, TEntity>
        where TContext : DbContext
        where TEntity : class
    {
        void ConfigureSaveEntry(ConfigureSaveEntryContext<TContext, TEntity> context);
    }
}