# EntityFramework.CommonTools <img alt="logo" src="icon.png" width="128" height="128" align="right" />
Extension for EntityFramework and EntityFramework Core that provides: Expandable Extension Methods, Complex Types as JSON, Auditing, Concurrency Checks, Specifications and serializable Transacton Logs.

[![Build status](https://ci.appveyor.com/api/projects/status/85f7aqrh2plkl7yn?svg=true)](https://ci.appveyor.com/project/gnaeus/entityframework-commontools)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/gnaeus/EntityFramework.CommonTools/master/LICENSE)
[![NuGet version](https://img.shields.io/nuget/v/EntityFramework.CommonTools.svg)](https://www.nuget.org/packages/EntityFramework.CommonTools)
[![NuGet version](https://img.shields.io/nuget/v/EntityFrameworkCore.CommonTools.svg)](https://www.nuget.org/packages/EntityFrameworkCore.CommonTools)

## Documentation
 * [Expandable IQueryable Extensions](#ef-querying)
 * [JSON Complex Types](#ef-json-field)
 * [Specification Pattern](#ef-specification)
 * [Auditable Entities](#ef-auditable-entities)
 * [Concurrency Checks](#ef-concurrency-checks)
 * [Transaction Logs](#ef-transaction-logs)
 * [DbContext Extensions (EF 6 only)](#ef-6-only)
 * [Usage with EntityFramework Core](#ef-core-usage)
 * [Usage with EntityFramework 6](#ef-6-usage)
 * [Changelog](#changelog)

### NuGet
```
PM> Install-Package EntityFramework.CommonTools

PM> Install-Package EntityFrameworkCore.CommonTools
```

<br>

## Attaching ExpressionVisitor to IQueryable

With `.AsVisitable()` extension we can attach any `ExpressionVisitor` to `IQueryable<T>`.

```cs
public static IQueryable<T> AsVisitable<T>(
    this IQueryable<T> queryable, params ExpressionVisitor[] visitors);
```

## <a name="ef-querying"></a> Expandable extension methods for IQueryable

We can use extension methods for `IQueryable<T>` to incapsulate custom buisiness logic.  
But if we call these methods from `Expression<TDelegate>`, we get runtime error.

```cs
public static IQueryable<Post> FilterByAuthor(this IQueryable<Post> posts, int authorId)
{
    return posts.Where(p => p.AuthorId = authorId);
}

public static IQueryable<Comment> FilterTodayComments(this IQueryable<Comment> comments)
{
    DateTime today = DateTime.Now.Date;

    return comments.Where(c => c.CreationTime > today)
}

Comment[] comments = context.Posts
    .FilterByAuthor(authorId)   // it's OK
    .SelectMany(p => p.Comments
        .AsQueryable()
        .FilterTodayComments()) // will throw Error
    .ToArray();
```

With `.AsExpandable()` extension we can use extension methods everywhere.

```cs
Comment[] comments = context.Posts
    .AsExpandable()
    .FilterByAuthor(authorId)   // it's OK
    .SelectMany(p => p.Comments
        .FilterTodayComments()) // it's OK too
    .ToArray();
```

Expandable extension methods should return `IQueryable` and should have `[Expandable]` attribute.

```cs
[Expandable]
public static IQueryable<Post> FilterByAuthor(this IEnumerable<Post> posts, int authorId)
{
    return posts.AsQueryable().Where(p => p.AuthorId = authorId);
}

[Expandable]
public static IQueryable<Comment> FilterTodayComments(this IEnumerable<Comment> comments)
{
    DateTime today = DateTime.Now.Date;

    return comments.AsQueryable().Where(c => c.CreationTime > today)
}
```

### [Benchmarks](./EFCore.CommonTools.Benchmarks/Querying/DatabaseQueryBenchmark.cs)
```
          Method |        Median |     StdDev | Scaled | Scaled-SD |
---------------- |-------------- |----------- |------- |---------- |
        RawQuery |   555.6202 μs | 15.1837 μs |   1.00 |      0.00 |
 ExpandableQuery |   644.6258 μs |  3.7793 μs |   1.15 |      0.03 | <<<
  NotCachedQuery | 2,277.7138 μs | 10.9754 μs |   4.06 |      0.10 |
```

<br>

## <a name="ef-json-field"></a> JSON Complex Types
There is an utility struct named `JsonField`, that helps to persist any Complex Type as JSON string in single table column.

```cs
struct JsonField<TObject>
    where TObject : class
{
    public string Json { get; set; }
    public TObject Object { get; set; }
}
```

Usage:
```cs
class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Login { get; set; }

    private JsonField<Address> _address;
    // used by EntityFramework
    public string AddressJson
    {
        get { return _address.Json; }
        set { _address.Json = value; }
    }
    // used by application code
    public Address Address
    {
        get { return _address.Object; }
        set { _address.Object = value; }
    }

    // collection initialization by default
    private JsonField<ICollection<string>> _phones = new HashSet<string>();
    public string PhonesJson
    {
        get { return _phones.Json; }
        set { _phones.Json = value; }
    }
    public ICollection<string> Phones
    {
        get { return _phones.Object; }
        set { _phones.Object = value; }
    }
}

[NotMapped]
class Address
{
    public string City { get; set; }
    public string Street { get; set; }
    public string Building { get; set; }
}
```

If we update these Complex Type properties, the following SQL is generated during `SaveChanges`:
```sql
UPDATE Users
SET AddressJson = '{"City":"Moscow","Street":"Arbat","Building":"10"}',
    PhonesJson = '["+7 (123) 456-7890","+7 (098) 765-4321"]'
WHERE Id = 1;
```

The `AddressJson` property is serialized from `Address` only when it accessed by EntityFramework.  
And the `Address` property is materialized from `AddressJson` only when EntityFramework writes to `AddressJson`.

If we want to initialize some JSON collection in entity consctuctor, for example:
```cs
class MyEntity
{
    public ICollection<MyObject> MyObjects { get; set; } = new HashSet<MyObject>();
}
```
We can use the following implicit conversion:
```cs
class MyEntity
{
    private JsonField<ICollection<MyObject>> _myObjects = new HashSet<MyObject>();
}
```
It uses the following implicit operator:
```cs
struct JsonField<TObject>
{
    public static implicit operator JsonField<TObject>(TObject defaultValue);
}
```

The only caveat is that `TObject` object should not contain reference loops.  
Because `JsonField` uses [Jil](https://github.com/kevin-montrose/Jil) (the fastest .NET JSON serializer) behind the scenes.

<br>

## <a name="ef-specification"></a> Specification Pattern

Generic implementation of [Specification Pattern](https://en.wikipedia.org/wiki/Specification_pattern).

```cs
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);

    Expression<Func<T, bool>> ToExpression();
}

public class Specification<T> : ISpecification<T>
{
    public Specification(Expression<Func<T, bool>> predicate);
}
```

We can define named specifications:
```cs
class UserIsActiveSpec : Specification<User>
{
    public UserIsActiveSpec()
        : base(u => !u.IsDeleted) { }
}

class UserByLoginSpec : Specification<User>
{
    public UserByLoginSpec(string login)
        : base(u => u.Login == login) { }
}
```

Then we can combine specifications with conditional logic operators `&&`, `||` and `!`:
```cs
class CombinedSpec
{
    public CombinedSpec(string login)
        : base(new UserIsActiveSpec() && new UserByLoginSpec(login)) { }
}
```

Also we can test it:
```cs
var user = new User { Login = "admin", IsDeleted = false };
var spec = new CombinedSpec("admin");

Assert.IsTrue(spec.IsSatisfiedBy(user));
```

And use with `IEnumerable<T>`:

```cs
var users = Enumerable.Empty<User>();
var spec = new UserByLoginSpec("admin");

var admin = users.FirstOrDefault(spec.IsSatisfiedBy);

// or even
var admin = users.FirstOrDefault(spec);
```

Or even with `IQueryable<T>`:
```cs
var spec = new UserByLoginSpec("admin");

var admin = context.Users.FirstOrDefault(spec.ToExpression());

// or even
var admin = context.Users.FirstOrDefault(spec);

// and also inside Expression
var adminFiends = context.Users
    .AsVisitable(new SpecificationExpander())
    .Where(u => u.Firends.Any(spec.ToExpression()))
    .ToList();

// or even
var adminFiends = context.Users
    .AsVisitable(new SpecificationExpander())
    .Where(u => u.Firends.Any(spec))
    .ToList();
```

<br>

## <a name="ef-auditable-entities"></a> Auditable Entities
Automatically update info about who and when create / modify / delete the entity during `context.SaveCahnges()`

```cs
class User
{
    public int Id { get;set; }
    public string Login { get; set; }
}

class Post : IFullAuditable<int>
{
    public int Id { get; set; }
    public string Content { get; set; }
    
    // IFullAuditable<int> members
    public bool IsDeleted { get; set; }
    public int CreatorUserId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public int? UpdaterUserId { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public int? DeleterUserId { get; set; }
    public DateTime? DeletedUtc { get; set; }
}

class MyContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Post> Posts { get; set; }

    public void SaveChanges(int editorUserId)
    {
        this.UpdateAuditableEntities(editorUserId);
        base.SaveChanges();
    }
}
```

<br>

Also you can track only the creation, deletion and so on by implementing the following interfaces:

#### `ISoftDeletable`
Used to standardize soft deleting entities. Soft-delete entities are not actually deleted,
marked as `IsDeleted == true` in the database, but can not be retrieved to the application.

```cs
interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
```

#### `ICreationTrackable`
An entity can implement this interface if `CreatedUtc` of this entity must be stored.
`CreatedUtc` is automatically set when saving Entity to database.

```cs
interface ICreationTrackable
{
    DateTime CreatedUtc { get; set; }
}
```

#### `ICreationAuditable<TUserId>`
This interface is implemented by entities that is wanted to store creation information (who and when created).
Creation time and creator user are automatically set when saving Entity to database.

```cs
interface ICreationAuditable<TUserId> : ICreationTrackable
    where TUserId : struct
{
    TUserId CreatorUserId { get; set; }
}
// or
interface ICreationAuditable : ICreationTrackable
{
    string CreatorUserId { get; set; }
}
```

#### `IModificationTrackable`
An entity can implement this interface if `UpdatedUtc` of this entity must be stored.
`UpdatedUtc` automatically set when updating the Entity.

```cs
interface IModificationTrackable
{
    DateTime? UpdatedUtc { get; set; }
}
```

#### `IModificationAuditable<TUserId>`
This interface is implemented by entities that is wanted
to store modification information (who and when modified lastly).
Properties are automatically set when updating the Entity.

```cs
interface IModificationAuditable<TUserId> : IModificationTrackable
    where TUserId : struct
{
    TUserId? UpdaterUserId { get; set; }
}
// or
interface IModificationAuditable : IModificationTrackable
{
    string UpdaterUserId { get; set; }
}
```

#### `IDeletionTrackable`
An entity can implement this interface if `DeletedUtc` of this entity must be stored.
`DeletedUtc` is automatically set when deleting Entity.

```cs
interface IDeletionTrackable : ISoftDeletable
{
    DateTime? DeletedUtc { get; set; }
}
```

#### `IDeletionAuditable<TUserId>`
This interface is implemented by entities which wanted to store deletion information (who and when deleted).

```cs
public interface IDeletionAuditable<TUserId> : IDeletionTrackable
    where TUserId : struct
{
    TUserId? DeleterUserId { get; set; }
}
// or
public interface IDeletionAuditable : IDeletionTrackable
{
    string DeleterUserId { get; set; }
}
```

#### `IFullTrackable`
This interface is implemented by entities which modification times must be tracked.
Related properties automatically set when saving/updating/deleting Entity objects.

```cs
interface IFullTrackable : ICreationTrackable, IModificationTrackable, IDeletionTrackable { }
```

#### `IFullAuditable<TUserId>`
This interface is implemented by entities which must be audited.
Related properties automatically set when saving/updating/deleting Entity objects.

```cs
interface IFullAuditable<TUserId> : IFullTrackable,
    ICreationAuditable<TUserId>, IModificationAuditable<TUserId>, IDeletionAuditable<TUserId>
    where TUserId : struct { }
// or
interface IFullAuditable : IFullTrackable, ICreationAuditable, IModificationAuditable, IDeletionAuditable { }
```

<br>

You can choose between saving the user `Id` or the user `Login`.  
So there are two overloadings for `DbContext.UpdateAudiatbleEntities()`:
```cs
static void UpdateAuditableEntities<TUserId>(this DbContext context, TUserId editorUserId);
static void UpdateAuditableEntities(this DbContext context, string editorUserId);
```
and also the separate extension to update only `Trackable` entities:
```cs
static void UpdateTrackableEntities(this DbContext context);
```

<br>

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

## <a name="ef-core-usage"></a> All together example for EntityFramework Core
```cs
class MyDbContext : DbContext
{
    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.UseTransactionLog();
    }

    // override the most general SaveChanges
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.UpdateTrackableEntities();
        this.UpdateConcurrentEntities();

        return this.SaveChangesWithTransactionLog(base.SaveChanges, acceptAllChangesOnSuccess);
    }

    // override the most general SaveChangesAsync
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
    {
        this.UpdateTrackableEntities();
        this.UpdateConcurrentEntities();

        return this.SaveChangesWithTransactionLogAsync(
            base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);
    }

    public int SaveChanges(int editorUserId)
    {
        this.UpdateAuditableEntities(editorUserId);
        
        return SaveChanges();
    }

    public Task<int> SaveChangesAsync(int editorUserId)
    {
        this.UpdateAuditableEntities(editorUserId);

        return SaveChangesAsync();
    }
}
```

<br>

## <a name="ef-6-usage"></a> All together example for EntityFramework 6
```cs
class MyDbContext : DbContext
{
    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.UseTransactionLog();
    }

    // override the most general SaveChanges
    public override int SaveChanges()
    {
        using (this.WithChangeTrackingOnce())
        {
            this.UpdateTrackableEntities();
            this.UpdateConcurrentEntities();

            return this.SaveChangesWithTransactionLog(base.SaveChanges);
        }
    }

    // override the most general SaveChangesAsync
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        using (this.WithChangeTrackingOnce())
        {
            this.UpdateTrackableEntities();
            this.UpdateConcurrentEntities();

            return this.SaveChangesWithTransactionLogAsync(base.SaveChangesAsync, cancellationToken);
        }
    }

    public int SaveChanges(int editorUserId)
    {
        this.UpdateAuditableEntities(editorUserId);
        
        return SaveChanges();
    }

    public Task<int> SaveChangesAsync(int editorUserId)
    {
        this.UpdateAuditableEntities(editorUserId);

        return SaveChangesAsync();
    }
}
```

<hr>

# <a name="changelog"></a> Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [2.0.1] - 2018-03-27
### Fixed
- `.AsExpandable()` works with bound lambda arguments

## [2.0.0] - 2018-03-23
### Added
- EFCore 2.0 support
- EntityFramework 6.2 support

### Changed
- `ICreationAuditable.CreatorUser` renamed to `CreatorUserId`
- `IModificationAuditable.UpdaterUser` renamed to `UpdaterUserId`
- `IDeletionAuditable.DeleterUser` renamed to `DeleterUserId`

See [#1](https://github.com/gnaeus/EntityFramework.CommonTools/issues/1).

For compatibility issues you still can use these interfaces:
```cs
public interface ICreationAuditableV1
{
    string CreatorUser { get; set; }
}

public interface IModificationAuditableV1
{
    string UpdaterUser { get; set; }
}

public interface IDeletionAuditableV1
{
    string DeleterUser { get; set; }
}

public interface IFullAuditableV1 : IFullTrackable,
    ICreationAuditableV1, IModificationAuditableV1, IDeletionAuditableV1
{
}
```

## [1.0.0] - 2017-08-16
### Added
Initial project version.

[2.0.1]: https://github.com/gnaeus/EntityFramework.CommonTools/compare/2.0.0...2.0.1
[2.0.0]: https://github.com/gnaeus/EntityFramework.CommonTools/compare/1.0.0...2.0.0
[1.0.0]: https://github.com/gnaeus/EntityFramework.CommonTools/tree/1.0.0
