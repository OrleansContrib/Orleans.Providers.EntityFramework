using System;

namespace Orleans.Providers.EntityFramework
{
    public interface IEntityTypeResolver
    {
        Type ResolveEntityType(string grainType, IGrainState grainState);
        Type ResolveStateType(string grainType, IGrainState grainState);
    }
}