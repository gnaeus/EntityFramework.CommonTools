#if EF_CORE
using System;
using System.Diagnostics;
using Jil;

namespace EntityFrameworkCore.ChangeTrackingExtensions
#else
using System;
using System.Diagnostics;
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

        public long Id { get; set; }
        public Guid TransactionId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string Operation { get; set; }
        public string Schema { get; set; }
        public string TableName { get; set; }
        public string EntityType { get; set; }
        public string EntityJson { get; set; }

        private object _entity;
        public object Entity => _entity
            ?? (_entity = JSON.Deserialize(EntityJson, Type.GetType(EntityType)));
        
        public TEntity GetEntity<TEntity>()
            where TEntity : class
        {
            return Entity as TEntity;
        }
    }
}
