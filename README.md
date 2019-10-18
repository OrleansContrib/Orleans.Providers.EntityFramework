# Orleans.Providers.EntityFramework
An Entity Framework Core implementation of Orleans Grain Storage.

There are some nice to have features missing. I didn't needed them particularly but If you have suggestions or want to help out, it would be much appreciated.

[![AppVeyor](https://img.shields.io/appveyor/ci/alirezajm/orleans-providers-entityframework.svg)](https://ci.appveyor.com/project/alirezajm/orleans-providers-entityframework)
[![NuGet](https://img.shields.io/nuget/v/Orleans.Providers.EntityFramework.svg)](https://www.nuget.org/packages/Orleans.Providers.EntityFramework)

## Usage

Nuget: https://www.nuget.org/packages/Orleans.Providers.EntityFramework/

or

```dotnet add package Orleans.Providers.EntityFramework --version 0.13.0```

or 

```Install-Package Orleans.Providers.EntityFramework --version 0.13.0```


And configure the storage provider using SiloHostBuilder:

```c#
ISiloHostBuilder builder = new SiloHostBuilder();

builder.AddEfGrainStorage<FrogsDbContext>("ef");  

```

This requires your DbContext to be registered as well

```c#
services
    .AddDbContextPool<FatDbContext>(
        (sp, options) => {});
```
The GrainStorage will resolve and releases contexts per operation so you won't have many context in use.
Hence it's better to use the context pool provided in the entity framework package or use your own.

## Configuration

By default the provider will search for key properties on your data models that match your grain interfaces, 
but you can change the default behavior like so:

```c#
services.Configure<GrainStorageConventionOptions>(options =>
{
    options.DefaultGrainKeyPropertyName = "Id";
    options.DefaultPersistenceCheckPropertyName = "Id";
    options.DefaultGrainKeyExtPropertyName = "KeyExt";
});
```

**DefaultPersistenceCheckPropertyName** is used to check if a model needs to be inserted into the database or updated. 
The value of said property will be checked against the default value of the type.

The following sample model would work out of the box for a grain that implements IGrainWithGuidCompoundKey and requires no configuration:

```c#
public class Box {
  public Guid Id { get; set; }
  public string KeyExt { get; set; }
  public byte[] ETag { get; set; }
}
```

_If you use conventions (as described, configuring GrainStorageConventionOptions) you're context should contain DbSets for your models._

```c#
public DbSet<Box> Boxes { get; set; }
```

### Querying models using custom expressions
To configure a special model you can do:

```c#
public class SpecialBox {
  public long WeirdId { get; set; }
  public string Type { get; set; }
  public long ClusterIndexId { get; set; }
}

services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .UseQueryExpression(grainRef =>
            {
                long key = grainRef.GetPrimaryKeyLong(out string keyExt);
                return (box => box.WeirdId == key && box.Type == keyExt);
            })
    )
```

or 

```c#
services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .UseKey(box => box.WeirdId)
            .UseKeyExt(box => box.Type)
    )
```

The **UseQueryExpression** method instructs the sotrage to use the provided expression to query the database.

### Loading additional data on read state
You can load additional data while reading the state. Using the SpecialBox model:

```c#
services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .UseQuery(context => context.SpecialBoxes.AsNoTracking()
                .Include(box => box.Gems)
                .ThenInclude(gems => gems.Map))
    )
```

### Using custom persistence check

When using Guids as primary keys you're most likely to add a cluster index that is auto incremented. 
That field can be used to check if the state is already inserted into the database or not:

```c#
services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .ConfigureIsPersisted(box => box.ClusterIndexId > 0)
    )
```

or 

```c#
services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .CheckPersistenceOn(box => box.ClusterIndexId)
    )
```

If you use different cluster indices (In case of mssqlserver) than your primary keys you can configure the dafaults to write less configuration code:

```c#
services.Configure<GrainStorageConventionOptions>(options =>
{
    options.DefaultPersistenceCheckPropertyName = "ClusterIndexId";
});
```

### ETags

By default models are searched for Etags and if a property on a model is marked as **ConcurrencyToken** the storage will pick that up.

Using the fluent API that would be:

```c#
builder.Entity<SpecialBox>()
    .Property(e => e.ETag)
    .IsConcurrencyToken();

```

Models can be further configured using extensions:

```c#
services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .UseETag()
    )
```

Using **UseETag** overload with no params instructs the storage to find an ETag property. If no valid property was found, an exception would be thrown.

Use the following overload to explicitly configure the storage to use the provided property. If the property is not marked as **ConcurrencyCheck** an exception would be thrown.

```c#
services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .UseETag(box => box.ETag)
    )
```

## Controlling how the state is saved

When calling writeState, the state object is attached to a context and its state (EF entry state) would be set to Added or Modified.

There are two ways to change the behavior:

### GrainStorageContext 

```c#
GrainStorageContext<Box>.ConfigureEntryState(
    entry => entry.Property(e => e.Title).IsModified = true);
```

This way only the Title field would be updated.

Things to consider:

- When configuring the entry manually, the storage provider only attaches the state to the context and doesn't set the entry state. So for example if you call this ```GrainStorageContext<Box>.ConfigureEntryState(entry => {});``` the write operation does nothing.
- Because GrainStorageContext uses async locals you have to call ```GrainStorageContext<Box>.Clear()``` if you want to do multiple writes on the same asynchronous operation.

### IGrainStateEntryConfigurator

By implementing ```IGrainStateEntryConfigurator<TContext, TGrain, TEntity>```  and registering it.

The default implementation is ```DefaultGrainStateEntryConfigurator``` and it just does the following:

```c#
public void ConfigureSaveEntry(ConfigureSaveEntryContext<TContext, TEntity> context)
{
	EntityEntry<TEntity> entry = context.DbContext.Entry(context.Entity);

	entry.State = context.IsPersisted
		? EntityState.Modified
		: EntityState.Added;
}
```

## Precompiled Queries

By default all queries are precompiled, unless using `ConfigureReadState` extension.

You can disable precompilation using 

```c#
services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .PreCompileReadQuery(false)
    )
```



## Conventions

You can change the conventions by implementing `IGrainStorageConvention`  or inheriting from `GrainStorageConvention`  which is used for all types and `IGrainStorageConvention<TContext, TGrain, TEntity>` for a specific grain type which has no default implementation.



## Custom Grain State Setter/Getter

You can implement `IEntityTypeResolver` or inheriting from `EntityTypeResolver` so you can have different grain state and storage model. This is particularly useful if you have abstract states or models without public default constructors which is a constraint on orleans grain states.

For example you can have the following class

```c#
class GenericGrainState<TEntity> 
{
    public TEntity Value { get; set;}
}
```

Using a custom `EntityTypeResolver` you can tell the storage `TEntity` is the persistent model.


## Known Issues and Limitations

- As types has to be configured in dbcontext, arbitrary types can't use this provider. 
This specially causes issues with Orleans VersionStoreGrain internal grain, hence this GrainStorage can't 
be used as default grain storage. I'll handle that special case if I get the time needed.
