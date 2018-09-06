using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework
{
    public class EntityFrameworkGrainStorage<TContext> : IGrainStorage
        where TContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EntityFrameworkGrainStorage<TContext>> _logger;

        private readonly ConcurrentDictionary<string, GrainStorageDescriptor> _stateTypeDescriptors
         = new ConcurrentDictionary<string, GrainStorageDescriptor>();

        public EntityFrameworkGrainStorage(
            IServiceProvider serviceProvider,
            IServiceScopeFactory scopeFactory,
            ILogger<EntityFrameworkGrainStorage<TContext>> logger)
        {
            _serviceProvider = serviceProvider;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (!_stateTypeDescriptors.TryGetValue(grainType, out GrainStorageDescriptor descriptor))
                descriptor = CreateGrainStorageDescriptor(grainType, grainReference, grainState);

            return descriptor.ReadStateAsyncDelegate(grainType, grainReference,
                grainState, descriptor.GrainStorageOptions);
        }

        public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (!_stateTypeDescriptors.TryGetValue(grainType, out GrainStorageDescriptor descriptor))
                descriptor = CreateGrainStorageDescriptor(grainType, grainReference, grainState);

            return descriptor.WriteStateAsyncDelegate(grainType, grainReference,
                grainState, descriptor.GrainStorageOptions);
        }

        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (!_stateTypeDescriptors.TryGetValue(grainType, out GrainStorageDescriptor descriptor))
                descriptor = CreateGrainStorageDescriptor(grainType, grainReference, grainState);

            return descriptor.ClearStateAsyncDelegate(grainType, grainReference,
                grainState, descriptor.GrainStorageOptions);
        }

        private GrainStorageDescriptor CreateGrainStorageDescriptor(
            string grainType,
            GrainReference grainReference
            , IGrainState grainState)
        {
            // todo: hack, the declared type of the grain state is only accessible like so
            Type stateType = grainState.GetType().IsGenericType
                ? grainState.GetType().GenericTypeArguments[0]
                : grainState.State.GetType();

            var descriptor = new GrainStorageDescriptor();

            // Construct read state method
            var readStateAsyncMethodInfo = this.GetType()
                                               .GetMethod(nameof(ReadStateAsync),
                                                   BindingFlags.NonPublic | BindingFlags.Instance)
                                           ?? throw new Exception("Impossible");
            descriptor.ReadStateAsyncDelegate =
                (ReadWriteStateAsyncDelegate)Delegate.CreateDelegate(
                    typeof(ReadWriteStateAsyncDelegate),
                    this, readStateAsyncMethodInfo.MakeGenericMethod(stateType));

            // Construct write state method
            var writeStateAsyncMethodInfo = this.GetType()
                                                .GetMethod(nameof(WriteStateAsync),
                                                    BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?? throw new Exception("Impossible");
            descriptor.WriteStateAsyncDelegate =
                (ReadWriteStateAsyncDelegate)Delegate.CreateDelegate(
                    typeof(ReadWriteStateAsyncDelegate),
                    this, writeStateAsyncMethodInfo.MakeGenericMethod(stateType));

            // Construct clear state method
            var clearStateAsyncMethodInfo = this.GetType()
                                                .GetMethod(nameof(ClearStateAsync),
                                                    BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?? throw new Exception("Impossible");
            descriptor.ClearStateAsyncDelegate =
                (ReadWriteStateAsyncDelegate)Delegate.CreateDelegate(
                    typeof(ReadWriteStateAsyncDelegate),
                    this, clearStateAsyncMethodInfo.MakeGenericMethod(stateType));

            // Construct get options method 
            var getGrainStorageOptionsMethodInfo = this.GetType()
                                                       .GetMethod(nameof(GetGrainStorageOptions),
                                                           BindingFlags.NonPublic | BindingFlags.Instance)
                                                   ?? throw new Exception("Impossible");

            var getGrainStorageOptionsImpl = getGrainStorageOptionsMethodInfo
                .MakeGenericMethod(stateType);
            try
            {
                descriptor.GrainStorageOptions = getGrainStorageOptionsImpl.Invoke(this,
                    new object[] { grainType });
            }
            catch (Exception e)
            {
                // ReSharper disable once PossibleNullReferenceException
                throw e.InnerException;
            }

            // Store for later use
            _stateTypeDescriptors.TryAdd(grainType, descriptor);

            return descriptor;
        }

        private async Task ReadStateAsync<TGrainState>(
            string grainType,
            IAddressable grainReference,
            IGrainState grainState,
            object storageOptions)
            where TGrainState : class
        {
            var options = (GrainStorageOptions<TContext, TGrainState>)storageOptions;

            Expression<Func<TGrainState, bool>> expression
                = options.QueryExpressionGeneratorFunc(grainReference);

            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                grainState.State = await
                    options.ReadQuery(context)
                        .SingleOrDefaultAsync(expression);
            }
        }

        private async Task WriteStateAsync<TGrainState>(string grainType,
            IAddressable grainReference,
            IGrainState grainState,
            object storageOptions)
            where TGrainState : class
        {
            var options = (GrainStorageOptions<TContext, TGrainState>)storageOptions;
            var state = ((TGrainState)grainState.State);
            bool isPersisted = options.IsPersistedFunc(state);

            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                EntityEntry<TGrainState> entry = context.Entry((TGrainState)grainState.State);

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

                await context.SaveChangesAsync();
            }
        }

        private async Task ClearStateAsync<TGrainState>(
            string grainType,
            IAddressable grainReference,
            IGrainState grainState,
            object storageOptions)
            where TGrainState : class
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                EntityEntry<TGrainState> entry = context.Entry((TGrainState)grainState.State);

                entry.State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }
        }

        private GrainStorageOptions<TContext, TGrainState> GetGrainStorageOptions<TGrainState>(
            string grainType)
            where TGrainState : class, new()
        {
            var options
                = _serviceProvider.GetOptionsByName<GrainStorageOptions<TContext, TGrainState>>(grainType);

            if (options.ReadQuery != null)
                return options;

            // Try generating a default options for the grain
            Type grainImplType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(grainType, false))
                .FirstOrDefault(t => t != null);
            if (grainImplType == null)
                throw new Exception($"Could not load \"{grainType}\" type. Try configuring grain options.");

            Type optionsType = typeof(GrainStoragePostConfigureOptions<,,>).MakeGenericType(
                typeof(TContext),
                grainImplType,
                typeof(TGrainState));

            var postConfigure = (IPostConfigureOptions<GrainStorageOptions<TContext, TGrainState>>)
                Activator.CreateInstance(optionsType, _serviceProvider);

            options = new GrainStorageOptions<TContext, TGrainState>();
            postConfigure.PostConfigure(grainType, options);

            _logger.LogWarning($"GrainStorageOptions is not configured for grain {grainType} " +
                               "and default options will be used. Consider configuring options for grain using " +
                               "using IServiceCollection.ConfigureGrainStorageOptions<TContext, TGrain, TState> extension method.");

            return options;
        }
    }
}