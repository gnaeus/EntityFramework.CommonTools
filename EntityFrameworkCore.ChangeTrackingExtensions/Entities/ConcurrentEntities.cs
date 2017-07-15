#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions.Entities
#else
namespace EntityFramework.ChangeTrackingExtensions.Entities
#endif
{
    /// <summary>
    /// An entity can implement this interface if it should use Optimistic Concurrency Check
    /// with populating <see cref="RowVersion"/> from client-side.
    /// <para />
    /// <see cref="RowVersion"/> property should be decorated by [ConcurrencyCheck]
    /// and [DatabaseGenerated(DatabaseGeneratedOption.Computed)] attributes.
    /// <para />
    /// <see cref="RowVersion"/> column should be updated by trigger in DB:
    /// <code>
    ///     CREATE TRIGGER TRG_MyTable_UPD
    ///     AFTER UPDATE ON MyTable
    ///         WHEN old.RowVersion = new.RowVersion
    ///     BEGIN
    ///         UPDATE MyTable
    ///         SET RowVersion = RowVersion + 1;
    ///     END;
    /// </code>
    /// </summary>
    public interface IConcurrencyCheckable
    {
        long RowVersion { get; set; }
    }

    /// <summary>
    /// An entity can implement this interface if it should use Optimistic Concurrency Check
    /// with populating <see cref="RowVersion"/> from client-side.
    /// <para />
    /// <see cref="RowVersion"/> property should be decorated by [Timestamp] attribute.
    /// <para />
    /// <see cref="RowVersion"/> column should have ROWVERSION type in SQL Server. 
    /// </summary>
    public interface ITimestampCheckable
    {
        byte[] RowVersion { get; set; }
    }
}
