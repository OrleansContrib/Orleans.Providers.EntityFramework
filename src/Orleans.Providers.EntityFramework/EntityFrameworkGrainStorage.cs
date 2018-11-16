using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework
{
    public class EntityFrameworkGrainStorage<TContext> : IGrainStorage
        where TContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeResolver _typeResolver;

        private readonly ConcurrentDictionary<string, IGrainStorage> _storage
            = new ConcurrentDictionary<string, IGrainStorage>();

        public EntityFrameworkGrainStorage(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _typeResolver = serviceProvider.GetRequiredService<ITypeResolver>();
        }

        public Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (!_storage.TryGetValue(grainType, out IGrainStorage storage))
                storage = CreateStorage(grainType, grainState);

            return storage.ReadStateAsync(grainType, grainReference, grainState);
        }

        public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (!_storage.TryGetValue(grainType, out IGrainStorage storage))
                storage = CreateStorage(grainType, grainState);

            return storage.WriteStateAsync(grainType, grainReference, grainState);
        }

        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (!_storage.TryGetValue(grainType, out IGrainStorage storage))
                storage = CreateStorage(grainType, grainState);

            return storage.ClearStateAsync(grainType, grainReference, grainState);
        }

        private IGrainStorage CreateStorage(
            string grainType
            , IGrainState grainState)
        {
            // todo: hack, the declared type of the grain state is only accessible like so
            Type stateType = grainState.GetType().IsGenericType
                ? grainState.GetType().GenericTypeArguments[0]
                : grainState.State.GetType();

            Type grainImplType = _typeResolver.ResolveType(grainType);

            Type storageType = typeof(GrainStorage<,,>)
                .MakeGenericType(typeof(TContext), grainImplType, stateType);

            IGrainStorage storage;

            try
            {
                storage = (IGrainStorage)Activator.CreateInstance(storageType, grainType, _serviceProvider);
            }
            catch (Exception e)
            {
                if (e.InnerException == null)
                    throw;
                // Unwrap target invocation exception

                throw e.InnerException;
            }


            _storage.TryAdd(grainType, storage);
            return storage;
        }
    }
}