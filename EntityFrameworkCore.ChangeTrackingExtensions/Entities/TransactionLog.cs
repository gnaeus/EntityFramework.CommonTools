#if EF_CORE
using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Jil;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#else
using System;
using System.Diagnostics;
using System.Data.Entity;
using Jil;

namespace EntityFramework.ChangeTrackingExtensions
#endif
{
    /// <summary>
    /// This interface is implemented by entities which wanted
    /// to store all modifications in <see cref="TransactionLog"/>.
    /// </summary>
    public interface ITransactionLoggable { }

    [DebuggerDisplay("{TableName} {CreatedUtc}", Name = "{Id} {Operation}")]
    public class TransactionLog
    {
        public const string INSERT = "INS";
        public const string UPDATE = "UPD";
        public const string DELETE = "DEL";

        /// <summary>
        /// Auto incremented primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// An ID of all changes that captured during single <see cref="DbContext.SaveChanges"/> call.
        /// </summary>
        public Guid TransactionId { get; set; }

        /// <summary>
        /// UTC timestamp of <see cref="DbContext.SaveChanges"/> call.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// "INS", "UPD" or "DEL". Not null.
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Schema for captured entity. Can be null for SQLite.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Table for captured entity. Not null.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Assembly qualified type name of captured entity. Not null.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// The captured entity serialized to JSON by Jil serializer. Not null.
        /// </summary>
        public string EntityJson { get; set; }

        private object _entity;
        /// <summary>
        /// Lazily deserialized entity object.
        /// Type for deserialization is taken from <see cref="EntityType"/> property.
        /// All navigation properties and collections will be empty.
        /// </summary>
        public object Entity => _entity
            ?? (_entity = JSON.Deserialize(EntityJson, Type.GetType(EntityType)));

        /// <summary>
        /// Get strongly typed entity from transaction log.
        /// Can be null if TEntity and type from <see cref="EntityType"/> property are incompatible.
        /// All navigation properties and collections will be empty.
        /// </summary>
        public TEntity GetEntity<TEntity>()
            where TEntity : class
        {
            return Entity as TEntity;
        }
    }
}
