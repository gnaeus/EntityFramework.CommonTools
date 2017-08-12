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
    string CreatorUser { get; set; }
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
    string UpdaterUser { get; set; }
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
    string DeleterUser { get; set; }
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
static void UpdateAuditableEntities(this DbContext context, string editorUser);
```
and also the separate extension to update only `Trackable` entities:
```cs
static void UpdateTrackableEntities(this DbContext context);
```

<br>
