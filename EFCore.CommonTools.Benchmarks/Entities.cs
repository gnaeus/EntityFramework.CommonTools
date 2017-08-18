using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools.Benchmarks
#else
namespace EntityFramework.CommonTools.Benchmarks
#endif
{
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }

        [InverseProperty(nameof(Post.Author))]
        public virtual ICollection<Post> Posts { get; set; } = new HashSet<Post>();
    }

    public class Post
    {
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public virtual User Author { get; set; }
    }

    public static class Extenisons
    {
        [Expandable]
        public static IQueryable<Post> FilterToday(this IEnumerable<Post> posts)
        {
            DateTime today = DateTime.Now.Date;

            return posts.AsQueryable().Where(p => p.Date > today);
        }
    }
}
