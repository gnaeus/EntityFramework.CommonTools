using System;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools
#elif EF_6
namespace EntityFramework.CommonTools
#endif
{
    /// <summary>
    /// This interface is implemented by entities that is wanted
    /// to store creation information (who and when created).
    /// Creation time and creator user are automatically set when saving Entity to database.
    /// </summary>
    public interface ICreationAuditable<TUserId> : ICreationTrackable
        where TUserId : struct
    {
        TUserId CreatorUserId { get; set; }
    }

    /// <summary>
    /// This interface is implemented by entities that is wanted
    /// to store creation information (who and when created).
    /// Creation time and creator user are automatically set when saving Entity to database.
    /// </summary>
    public interface ICreationAuditable : ICreationTrackable
    {
        string CreatorUserId { get; set; }
    }

    [Obsolete("Use ICreationAuditable instead")]
    public interface ICreationAuditableV1 : ICreationTrackable
    {
        string CreatorUser { get; set; }
    }

    /// <summary>
    /// This interface is implemented by entities that is wanted
    /// to store modification information (who and when modified lastly).
    /// Properties are automatically set when updating the Entity.
    /// </summary>
    public interface IModificationAuditable<TUserId> : IModificationTrackable
        where TUserId : struct
    {
        TUserId? UpdaterUserId { get; set; }
    }

    /// <summary>
    /// This interface is implemented by entities that is wanted
    /// to store modification information (who and when modified lastly).
    /// Properties are automatically set when updating the Entity.
    /// </summary>
    public interface IModificationAuditable : IModificationTrackable
    {
        string UpdaterUserId { get; set; }
    }

    [Obsolete("Use IModificationAuditable instead")]
    public interface IModificationAuditableV1 : IModificationTrackable
    {
        string UpdaterUser { get; set; }
    }

    /// <summary>
    /// This interface is implemented by entities which wanted
    /// to store deletion information (who and when deleted).
    /// </summary>
    public interface IDeletionAuditable<TUserId> : IDeletionTrackable
        where TUserId : struct
    {
        TUserId? DeleterUserId { get; set; }
    }

    /// <summary>
    /// This interface is implemented by entities which wanted
    /// to store deletion information (who and when deleted).
    /// </summary>
    public interface IDeletionAuditable : IDeletionTrackable
    {
        string DeleterUserId { get; set; }
    }

    [Obsolete("Use IDeletionAuditable instead")]
    public interface IDeletionAuditableV1 : IDeletionTrackable
    {
        string DeleterUser { get; set; }
    }

    /// <summary>
    /// This interface is implemented by entities which must be audited.
    /// Related properties automatically set when saving/updating/deleting Entity objects.
    /// </summary>
    public interface IFullAuditable<TUserId> : IFullTrackable,
        ICreationAuditable<TUserId>, IModificationAuditable<TUserId>, IDeletionAuditable<TUserId>
        where TUserId : struct
    {
    }

    /// <summary>
    /// This interface is implemented by entities which must be audited.
    /// Related properties automatically set when saving/updating/deleting Entity objects.
    /// </summary>
    public interface IFullAuditable : IFullTrackable,
        ICreationAuditable, IModificationAuditable, IDeletionAuditable
    {
    }

    [Obsolete("Use IFullAuditable instead")]
    public interface IFullAuditableV1 : IFullTrackable,
        ICreationAuditableV1, IModificationAuditableV1, IDeletionAuditableV1
    {
    }
}
