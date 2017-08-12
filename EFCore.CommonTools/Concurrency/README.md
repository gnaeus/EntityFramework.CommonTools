## <a name="ef-concurrency-checks"></a> Concurrency Checks
By default EF and EFCore uses `EntityEntry.OriginalValues["RowVersion"]` for concurrency checks
([see docs](https://docs.microsoft.com/en-us/ef/core/saving/concurrency)).

With this behaviour the concurrency conflict may occur only between the `SELECT` statement
that loads entities to the `DbContext` and the `UPDATE` statement from `DbContext.SaveChanges()`.

But sometimes we want check concurrency conflicts between two or more edit operations that comes from client-side. For example:

* user_1 loads the editor form
* user_2 loads the same editor form
* user_1 saves his changes
* user_2 saves his changes __and gets concurrency conflict__.

To provide this behaviour, an entity should implement the following interface:
```cs
interface IConcurrencyCheckable<TRowVersion>
{
    TRowVersion RowVersion { get; set; }
}
```
And the `DbContext` should overload `SaveChanges()` method with `UpdateConcurrentEntities()` extension:
```cs
class MyDbContext : DbContext
{
    public override int SaveChanges()
    {
        this.UpdateConcurrentEntities();
        return base.SaveChanges();
    }
}
```

<br>

There are also three different behaviours for `IConcurrencyCheckable<T>`:

#### `IConcurrencyCheckable<byte[]>`
`RowVersion` property should be decorated by `[Timestamp]` attribute.  
`RowVersion` column should have `ROWVERSION` type in SQL Server.  
The default behaviour. Supported only by Microsoft SQL Server.

```cs
class MyEntity : IConcurrencyCheckable<Guid>
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

#### `IConcurrencyCheckable<Guid>`
`RowVersion` property should be decorated by `[ConcurrencyCheck]` attribute.  
It's value is populated by `Guid.NewGuid()` during each `DbContext.SaveChanges()` call at client-side.  
No specific database support is needed.

```cs
class MyEntity : IConcurrencyCheckable<Guid>
{
    [ConcurrencyCheck]
    public Guid RowVersion { get; set; }
}
```

#### `IConcurrencyCheckable<long>`
`RowVersion` property should be decorated by `[ConcurrencyCheck]` and `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` attributes.

```cs
class MyEntity : IConcurrencyCheckable<long>
{
    [ConcurrencyCheck]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public long RowVersion { get; set; }
}
```

`RowVersion` column should be updated by trigger in DB. Example for SQLite:
```sql
CREATE TABLE MyEntities ( RowVersion INTEGER DEFAULT 0 );

CREATE TRIGGER TRG_MyEntities_UPD
AFTER UPDATE ON MyEntities
    WHEN old.RowVersion = new.RowVersion
BEGIN
    UPDATE MyEntities
    SET RowVersion = RowVersion + 1;
END;
```

<br>

But sometimes we want to ignore `DbUpdateConcurrencyException`.
And there are two extension methods for this.

__`static void SaveChangesIgnoreConcurrency(this DbContext dbContext, int retryCount = 3)`__  
Save changes regardless of `DbUpdateConcurrencyException`.

__`static async Task SaveChangesIgnoreConcurrencyAsync(this DbContext dbContext, int retryCount = 3)`__  
Save changes regardless of `DbUpdateConcurrencyException`.

<br>
