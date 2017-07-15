#if EF_CORE
using System;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#else
using System;

namespace EntityFramework.ChangeTrackingExtensions
#endif
{
    /// <summary>
    /// Used to standardize soft deleting entities. Soft-delete entities are not actually deleted,
    /// marked as IsDeleted = true in the database, but can not be retrieved to the application.
    /// </summary>
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
    }

    /// <summary>
    /// An entity can implement this interface if <see cref="CreatedUtc"/> of this entity must be stored.
    /// <see cref="CreatedUtc"/> is automatically set when saving Entity to database.
    /// </summary>
    public interface ICreationTrackable
    {
        DateTime CreatedUtc { get; set; }
    }
    
    /// <summary>
    /// An entity can implement this interface if <see cref="UpdatedUtc"/> of this entity must be stored.
    /// <see cref="UpdatedUtc"/> is automatically set when updating Entity.
    /// </summary>
    public interface IModificationTrackable
    {
        DateTime? UpdatedUtc { get; set; }
    }
    
    /// <summary>
    /// An entity can implement this interface if <see cref="DeletedUtc"/> of this entity must be stored.
    /// <see cref="DeletedUtc"/> is automatically set when deleting Entity.
    /// </summary>
    public interface IDeletionTrackable : ISoftDeletable
    {
        DateTime? DeletedUtc { get; set; }
    }
    
    /// <summary>
    /// This interface is implemented by entities which modification times must be tracked.
    /// Related properties automatically set when saving/updating/deleting Entity objects.
    /// </summary>
    public interface IFullTrackable : ICreationTrackable, IModificationTrackable, IDeletionTrackable
    {
    }
}
