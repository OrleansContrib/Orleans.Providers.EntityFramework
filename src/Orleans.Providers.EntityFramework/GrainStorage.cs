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
    internal class GrainStorage<TContext, TGrain, TGrainState> : IGrainStorage
        where TContext : DbContext
        where TGrain : Grain<TGrainState>
        where TGrainState : class, new()
    {
        private readonly GrainStorageOptions<TContext, TGrain, TGrainState> _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GrainStorage<TContext, TGrain, TGrainState>> _logger;
        private readonly IServiceProvider _serviceProvider;

        public GrainStorage(
            GrainStorageOptions<TContext, TGrain, TGrainState> options,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<GrainStorage<TContext, TGrain, TGrainState>> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public GrainStorage(string grainType, IServiceProvider serviceProvider)
        {
            if (grainType == null) throw new ArgumentNullException(nameof(grainType));

            _serviceProvider = serviceProvider
                               ?? throw new ArgumentNullException(nameof(serviceProvider));

            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<GrainStorage<TContext, TGrain, TGrainState>>()
                      ?? NullLogger<GrainStorage<TContext, TGrain, TGrainState>>.Instance;

            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            _options = GetOrCreateDefaultOptions(grainType);
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {

            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                TGrainState state = await _options.ReadStateAsync(context, grainReference).ConfigureAwait(false);

                grainState.State = state;

                if (state != null && _options.CheckForETag)
                    grainState.ETag = _options.GetETagFunc(state);
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var state = (TGrainState)grainState.State;
            bool isPersisted = _options.IsPersistedFunc(state);

            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                EntityEntry<TGrainState> entry = context.Entry(state);

                if (GrainStorageContext<TGrainState>.IsConfigured)
                {
                    GrainStorageContext<TGrainState>.ConfigureStateDelegate(entry);
                }
                else
                {
                    entry.State = isPersisted
                        ? EntityState.Modified
                        : EntityState.Added;
                }

                try
                {
                    await context.SaveChangesAsync()
                        .ConfigureAwait(false);

                    if (_options.CheckForETag)
                        grainState.ETag = _options.GetETagFunc(state);
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
            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                EntityEntry<TGrainState> entry = context.Entry((TGrainState)grainState.State);

                entry.State = EntityState.Deleted;
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }


        private GrainStorageOptions<TContext, TGrain, TGrainState> GetOrCreateDefaultOptions(string grainType)
        {
            var options
                = _serviceProvider.GetOptionsByName<GrainStorageOptions<TContext, TGrain, TGrainState>>(grainType);

            if (options.IsConfigured)
                return options;

            // Try generating a default options for the grain

            Type optionsType = typeof(GrainStoragePostConfigureOptions<,,>).MakeGenericType(
                typeof(TContext),
                typeof(TGrain),
                typeof(TGrainState));

            var postConfigure = (IPostConfigureOptions<GrainStorageOptions<TContext, TGrain, TGrainState>>)
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