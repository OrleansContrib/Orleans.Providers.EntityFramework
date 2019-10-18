using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework
{
    internal class GrainStorage<TContext, TGrain, TGrainState, TEntity> : IGrainStorage
        where TContext : DbContext
        where TGrain : Grain<TGrainState>
        where TGrainState : class, new()
        where TEntity : class
    {
        private readonly GrainStorageOptions<TContext, TGrain, TEntity> _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GrainStorage<TContext, TGrain, TGrainState, TEntity>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGrainStateEntryConfigurator<TContext, TGrain, TEntity> _entryConfigurator;

        public GrainStorage(string grainType, IServiceProvider serviceProvider)
        {
            if (grainType == null) throw new ArgumentNullException(nameof(grainType));

            _serviceProvider = serviceProvider
                               ?? throw new ArgumentNullException(nameof(serviceProvider));

            _entryConfigurator = (IGrainStateEntryConfigurator<TContext, TGrain, TEntity>)serviceProvider.GetRequiredService(
                typeof(IGrainStateEntryConfigurator<TContext, TGrain, TEntity>));

            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<GrainStorage<TContext, TGrain, TGrainState, TEntity>>()
                      ?? NullLogger<GrainStorage<TContext, TGrain, TGrainState, TEntity>>.Instance;

            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            _options = GetOrCreateDefaultOptions(grainType);
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                TEntity entity = await _options.ReadStateAsync(context, grainReference)
                    .ConfigureAwait(false);

                _options.SetEntity(grainState, entity);

                if (entity != null && _options.CheckForETag)
                    grainState.ETag = _options.GetETagFunc(entity);
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            TEntity entity = _options.GetEntity(grainState);

            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                if (GrainStorageContext<TEntity>.IsConfigured)
                {
                    EntityEntry<TEntity> entry = context.Entry(entity);
                    GrainStorageContext<TEntity>.ConfigureStateDelegate(entry);
                }
                else
                {
                    bool isPersisted = _options.IsPersistedFunc(entity);

                    _entryConfigurator.ConfigureSaveEntry(
                        new ConfigureSaveEntryContext<TContext, TEntity>(
                            context, entity)
                        {
                            IsPersisted = isPersisted
                        });
                }

                try
                {
                    await context.SaveChangesAsync()
                        .ConfigureAwait(false);

                    if (_options.CheckForETag)
                        grainState.ETag = _options.GetETagFunc(entity);
                }
                catch (DbUpdateConcurrencyException e)
                {
                    if (!_options.CheckForETag)
                        throw new InconsistentStateException(e.Message, e);

                    object storedETag = e.Entries.First().OriginalValues[_options.ETagProperty];
                    throw new InconsistentStateException(e.Message,
                        _options.ConvertETagObjectToStringFunc(storedETag),
                        grainState.ETag,
                        e);
                }
            }
        }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            TEntity entity = _options.GetEntity(grainState);
            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                context.Remove(entity);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }


        private GrainStorageOptions<TContext, TGrain, TEntity> GetOrCreateDefaultOptions(string grainType)
        {
            var options
                = _serviceProvider.GetOptionsByName<GrainStorageOptions<TContext, TGrain, TEntity>>(grainType);

            if (options.IsConfigured)
                return options;

            // Try generating a default options for the grain

            Type optionsType = typeof(GrainStoragePostConfigureOptions<,,,>)
                .MakeGenericType(
                    typeof(TContext),
                    typeof(TGrain),
                    typeof(TGrainState),
                    typeof(TEntity));

            var postConfigure = (IPostConfigureOptions<GrainStorageOptions<TContext, TGrain, TEntity>>)
                Activator.CreateInstance(optionsType, _serviceProvider);

            postConfigure.PostConfigure(grainType, options);

            _logger.LogInformation($"GrainStorageOptions is not configured for grain {grainType} " +
                                   "and default options will be used. If default configuration is not desired, " +
                                   "consider configuring options for grain using " +
                                   "using IServiceCollection.ConfigureGrainStorageOptions<TContext, TGrain, TState> extension method.");

            return options;
        }
    }
}