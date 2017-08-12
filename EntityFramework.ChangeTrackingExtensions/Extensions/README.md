## <a name="ef-6-only"></a> DbContext Extensions (EF 6 only)

__`static IDisposable WithoutChangeTracking(this DbContext dbContext)`__  
Disposable token for `using(...)` statement where `DbContext.Configuration.AutoDetectChanges` is disabled.

```cs
// here AutoDetectChanges is enabled
using (dbContext.WithoutChangeTracking())
{
    // inside this block AutoDetectChanges is disabled
}
// here AutoDetectChanges is enabled again
```

<br>

__`static IDisposable WithChangeTrackingOnce(this DbContext dbContext)`__  
Run `DbChangeTracker.DetectChanges()` once and return disposable token for `using(...)` statement
where `DbContext.Configuration.AutoDetectChanges` is disabled.

```cs
// here AutoDetectChanges is enabled
using (dbContext.WithChangeTrackingOnce())
{
    // inside this block AutoDetectChanges is disabled
}
// here AutoDetectChanges is enabled again
```

<br>

__`static TableAndSchema GetTableAndSchemaName(this DbContext context, Type entityType)`__  
Get corresponding table name and schema by `entityType`.

__`static TableAndSchema[] GetTableAndSchemaNames(this DbContext context, Type entityType)`__  
Get corresponding table name and schema by `entityType`.
Use it if entity is splitted between multiple tables.

```cs
struct TableAndSchema
{
    public string TableName;
    public string Schema;
}
```

<br>
