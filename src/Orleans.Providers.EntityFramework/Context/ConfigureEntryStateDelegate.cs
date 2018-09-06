using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Orleans.Providers.EntityFramework
{
    public delegate void ConfigureEntryStateDelegate<TGrainState>(EntityEntry<TGrainState> entry)
        where TGrainState : class;
}