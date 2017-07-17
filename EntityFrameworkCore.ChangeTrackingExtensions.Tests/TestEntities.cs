using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
{
    public class User : IFullTrackable, ITransactionLoggable
    {
        public int Id { get; set; }
        public string Login { get; set; }

        [Column("UserContacts")]
        public string Contacts { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public DateTime? DeletedUtc { get; set; }
    }

    public class Post : IFullAuditable<int>, IConcurrencyCheckable<Guid>, ITransactionLoggable
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        JsonField<ICollection<string>> _tags = new HashSet<string>();

        public bool ShouldSerializeTagsJson() => false;

        public string TagsJson
        {
            get { return _tags.Json; }
            set { _tags.Json = value; }
        }

        [NotMapped]
        public ICollection<string> Tags
        {
            get { return _tags.Value; }
            set { _tags.Value = value; }
        }

        public bool IsDeleted { get; set; }
        public int CreatorUserId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int? UpdaterUserId { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public int? DeleterUserId { get; set; }
        public DateTime? DeletedUtc { get; set; }

        [ConcurrencyCheck]
        public Guid RowVersion { get; set; }

        [ForeignKey(nameof(CreatorUserId))]
        public virtual User Author { get; set; }
    }

    [Table("Settings")]
    public class Settings : IFullAuditable
    {
        [Key]
        public string Key { get; set; }

        JsonField<dynamic> _value;

        [Column("Value"), IgnoreDataMember]
        public string ValueJson
        {
            get { return _value.Json; }
            set { _value.Json = value; }
        }

        [NotMapped]
        public dynamic Value
        {
            get { return _value.Value; }
            set { _value.Value = value; }
        }

        public bool IsDeleted { get; set; }
        public string CreatorUser { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string UpdaterUser { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public string DeleterUser { get; set; }
        public DateTime? DeletedUtc { get; set; }
    }
}
