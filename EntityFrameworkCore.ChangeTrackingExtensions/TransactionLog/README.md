## <a name="ef-transaction-logs"></a> Transaction Logs
Write all inserted / updated / deleted entities (serialized to JSON) to the separete table named `TransactionLog`.

To capture transaction logs an entity must inherit from empty `ITransactionLoggable { }` interface.

And the `DbContext` should overload `SaveChanges()` method with `SaveChangesWithTransactionLog()` wrapper,  
and register the `TransactionLog` entity in `ModelBuilder`.

```cs
class Post : ITransactionLoggable
{
    public string Content { get; set; }
}

// for EntityFramework 6
class MyDbContext : DbContext
{
    public DbSet<Post> Posts { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.UseTransactionLog();
    }

    public override int SaveChanges()
    {
        return this.SaveChangesWithTransactionLog(base.SaveChanges);
    }

    // override the most general SaveChangesAsync
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return this.SaveChangesWithTransactionLogAsync(base.SaveChangesAsync, cancellationToken);
    }
}

// for EntityFramework Core
class MyCoreDbContext : DbContext
{
    public DbSet<Post> Posts { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.UseTransactionLog();
    }

    // override the most general SaveChanges
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return this.SaveChangesWithTransactionLog(base.SaveChanges, acceptAllChangesOnSuccess);
    }

    // override the most general SaveChangesAsync
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
    {
        return this.SaveChangesWithTransactionLogAsync(
            base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);
    }
}

```

After that the transaction logs can be accessed via `TransactionLog` entity:

```cs
class TransactionLog
{
    // Auto incremented primary key.
    public long Id { get; set; }
    
    // An ID of all changes that captured during single DbContext.SaveChanges() call.
    public Guid TransactionId { get; set; }

    // UTC timestamp of DbContext.SaveChanges() call.
    public DateTime CreatedUtc { get; set; }

    // "INS", "UPD" or "DEL". Not null.
    public string Operation { get; set; }

    // Schema for captured entity. Can be null for SQLite.
    public string Schema { get; set; }

    // Table for captured entity. Not null.
    public string TableName { get; set; }

    // Assembly qualified type name of captured entity. Not null.
    public string EntityType { get; set; }

    // The captured entity serialized to JSON by Jil serializer. Not null.
    public string EntityJson { get; set; }

    // Lazily deserialized entity object.
    // Type for deserialization is taken from EntityType property.
    // All navigation properties and collections will be empty.
    public object Entity { get; }

    // Get strongly typed entity from transaction log.
    // Can be null if TEntity and type from EntityType property are incompatible.
    // All navigation properties and collections will be empty.
    public TEntity GetEntity<TEntity>();
}
```

<br>
