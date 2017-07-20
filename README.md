# <img src="icon.png" style="height: 32px; vertical-align: text-bottom" /> EntityFramework.ChangeTrackingExtensions
 An extension for EntityFramework and EntityFrameworkCore that provides Audit Logging, Concurrency Checks, storing Complex Types as JSON and storing history of all changes from `DbContext` to Transaction Log.

[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/gnaeus/EntityFramework.ChangeTrackingExtensions/master/LICENSE)
[![NuGet version](https://img.shields.io/nuget/v/EntityFramework.ChangeTrackingExtensions.svg)](https://www.nuget.org/packages/EntityFramework.ChangeTrackingExtensions)
[![NuGet version](https://img.shields.io/nuget/v/EntityFrameworkCore.ChangeTrackingExtensions.svg)](https://www.nuget.org/packages/EntityFrameworkCore.ChangeTrackingExtensions)

## Documentation
 * [JSON Complex Types](#ef-json-field)
 * [Auditable Entities](#ef-auditable-entities)
 * [Concurrency Checks](#ef-concurrency-checks)
 * [Transaction Logs](#ef-transaction-logs)
 * [Extensions (EF 6 only)](#ef-6-only)

<br>

### <a name="ef-json-field"></a> JSON Complex Types
There is an utility struct named `JsonField`, that helps to persist any Complex Type as JSON string in single table column.

```cs
struct JsonField<TValue>
    where TValue : class
{
    public string Json { get; set; }
    public TValue Value { get; set; }
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
        get { return _address.Value; }
        set { _address.Value = value; }
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
        get { return _phones.Value; }
        set { _phones.Value = value; }
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

If we update such JSON properties the following SQL is generated during `SaveChanges`:
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
struct JsonField<TValue>
{
    public static implicit operator JsonField<TValue>(TValue value);
}
```

The only caveat is that `TValue` object should not contain reference loops.  
Because `JsonField` uses [Jil](https://github.com/kevin-montrose/Jil) (the fastest .NET JSON serializer) behind the scenes.

<br>

### <a name="ef-auditable-entities"></a> Auditable Entities
Automatically update info about who and when create/modify/delete the entity during `context.SaveCahnges()`

Usage:
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

```cs
interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
```
Used to standardize soft deleting entities. Soft-delete entities are not actually deleted,
marked as `IsDeleted == true` in the database, but can not be retrieved to the application.

<br>

```cs
interface ICreationTrackable
{
    DateTime CreatedUtc { get; set; }
}
```
An entity can implement this interface if `CreatedUtc` of this entity must be stored.
`CreatedUtc` is automatically set when saving Entity to database.

<br>

```cs
interface ICreationAuditable<TUserId> : ICreationTrackable
    where TUserId : struct
{
    TUserId CreatorUserId { get; set; }
}
// or
interface ICreationAuditable : ICreationTrackable
{
    string CreatorUser { get; set; }
}
```
This interface is implemented by entities that is wanted to store creation information (who and when created).
Creation time and creator user are automatically set when saving Entity to database.

<br>

```cs
interface IModificationTrackable
{
    DateTime? UpdatedUtc { get; set; }
}
```
An entity can implement this interface if `UpdatedUtc` of this entity must be stored.
`UpdatedUtc` automatically set when updating the Entity.

<br>

```cs
interface IModificationAuditable<TUserId> : IModificationTrackable
    where TUserId : struct
{
    TUserId? UpdaterUserId { get; set; }
}
// or
interface IModificationAuditable : IModificationTrackable
{
    string UpdaterUser { get; set; }
}
```
This interface is implemented by entities that is wanted
to store modification information (who and when modified lastly).
Properties are automatically set when updating the Entity.

<br>

```cs
interface IDeletionTrackable : ISoftDeletable
{
    DateTime? DeletedUtc { get; set; }
}
```
An entity can implement this interface if `DeletedUtc` of this entity must be stored.
`DeletedUtc` is automatically set when deleting Entity.

<br>

```cs
public interface IDeletionAuditable<TUserId> : IDeletionTrackable
    where TUserId : struct
{
    TUserId? DeleterUserId { get; set; }
}
// or
public interface IDeletionAuditable : IDeletionTrackable
{
    string DeleterUser { get; set; }
}
```
This interface is implemented by entities which wanted to store deletion information (who and when deleted).

<br>

```cs
interface IFullTrackable : ICreationTrackable, IModificationTrackable, IDeletionTrackable { }
```
This interface is implemented by entities which modification times must be tracked.
Related properties automatically set when saving/updating/deleting Entity objects.

<br>

```cs
interface IFullAuditable<TUserId> : IFullTrackable,
    ICreationAuditable<TUserId>, IModificationAuditable<TUserId>, IDeletionAuditable<TUserId>
    where TUserId : struct { }
// or
interface IFullAuditable : IFullTrackable, ICreationAuditable, IModificationAuditable, IDeletionAuditable { }
```
This interface is implemented by entities which must be audited.
Related properties automatically set when saving/updating/deleting Entity objects.

<br>

You can choose between saving the user `Id` or the user `Login`.  
So there are two overloadings for `DbContext.UpdateAudiatbleEntities()`:
```cs
static void UpdateAuditableEntities<TUserId>(this DbContext context, TUserId editorUserId);
static void UpdateAuditableEntities(this DbContext context, string editorUser);
```
and also the separate extension to update only `Trackable` entities:
```cs
static void UpdateTrackableEntities(this DbContext context);
```

<br>

### <a name="ef-concurrency-checks"></a> Concurrency Checks
By default EF and EFCore uses `EntityEntry.OriginalValues["RowVersion"]` for concurrency checks
([see docs](https://docs.microsoft.com/en-us/ef/core/saving/concurrency)).  
With this behaviour the concurrency conflict may occur only between `SELECT` statement
that loads entities to the `DbContext` and `UPDATE` statement from `DbContext.SaveChanges()`.

But sometimes we want check concurrency conflicts between two or more edit operations that comes from client-side. For example:

* User1 loads the editor form
* User2 loads the same editor form
* User1 saves his changes
* User2 saves his changes __and gets concurrency conflict__.

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
```cs
class MyEntity : IConcurrencyCheckable<Guid>
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```
`RowVersion` property should be decorated by `[Timestamp]` attribute.  
`RowVersion` column should have `ROWVERSION` type in SQL Server.  
The default behaviour. Supported only by Microsoft SQL Server.

<br>

#### `IConcurrencyCheckable<Guid>`
```cs
class MyEntity : IConcurrencyCheckable<Guid>
{
    [ConcurrencyCheck]
    public Guid RowVersion { get; set; }
}
```
`RowVersion` property should be decorated by `[ConcurrencyCheck]` attribute.  
It's value is populated by `Guid.NewGuid()` during each `DbContext.SaveChanges()` call at client-side.  
No specific database support is needed.

<br>

#### `IConcurrencyCheckable<long>`
```cs
class MyEntity : IConcurrencyCheckable<long>
{
    [ConcurrencyCheck]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public long RowVersion { get; set; }
}
```
`RowVersion` property should be decorated by `[ConcurrencyCheck]` and `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` attributes.  
`RowVersion` column should be updated by trigger in DB. Example for SQLite:
```sql
CREATE TABLE MyTable ( RowVersion INTEGER DEFAULT 0 );

CREATE TRIGGER TRG_MyTable_UPD
AFTER UPDATE ON MyTable
    WHEN old.RowVersion = new.RowVersion
BEGIN
    UPDATE MyTable
    SET RowVersion = RowVersion + 1;
END;
```

<br>

### <a name="ef-transaction-logs"></a> Transaction Logs

<br>

### <a name="ef-6-only"></a> Extensions (EF 6 only)

<br>
