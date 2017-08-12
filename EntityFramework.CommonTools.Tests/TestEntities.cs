using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace EntityFramework.CommonTools.Tests
{
    public abstract class Entity
    {
        public int Id { get; set; }
    }

    public class Role : Entity, IConcurrencyCheckable<Guid>, ITransactionLoggable
    {
        public string Name { get; set; }

        [ConcurrencyCheck]
        public Guid RowVersion { get; set; }
    }

    public class User : Entity, IFullTrackable, ITransactionLoggable
    {
        public string Login { get; set; }

        [Column("UserContacts")]
        public string Contacts { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public DateTime? DeletedUtc { get; set; }

        [InverseProperty(nameof(Post.Author))]
        public virtual ICollection<Post> Posts { get; set; } = new HashSet<Post>();
    }
    
    public class Post : Entity, IFullAuditable<int>, IConcurrencyCheckable<Guid>, ITransactionLoggable
    {
        public string Title { get; set; }
        public string Content { get; set; }

        JsonField<ICollection<string>> _tags = new HashSet<string>();

        public bool ShouldSerializeTagsJson() => false;

        public string TagsJson
        {
            get { return _tags.Json; }
            set { _tags.Json = value; }
        }

        public ICollection<string> Tags
        {
            get { return _tags.Object; }
            set { _tags.Object = value; }
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
    public class Settings : IFullAuditable, IConcurrencyCheckable<long>
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

        public dynamic Value
        {
            get { return _value.Object; }
            set { _value.Object = value; }
        }
        
        public bool IsDeleted { get; set; }
        public string CreatorUser { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string UpdaterUser { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public string DeleterUser { get; set; }
        public DateTime? DeletedUtc { get; set; }

        [ConcurrencyCheck]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public long RowVersion { get; set; }
    }
}
