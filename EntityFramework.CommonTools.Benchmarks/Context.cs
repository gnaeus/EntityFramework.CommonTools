using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;

namespace EntityFramework.CommonTools.Benchmarks
{
    public class Context : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }

        public Context(DbConnection connection)
            : base(connection, false)
        {
            Database.SetInitializer<Context>(null);
        }

        public static DbConnection CreateConnection()
        {
            var connection = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");

            connection.Open();

            using (var context = new Context(connection))
            {
                context.Database.ExecuteSqlCommand(@"
                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Login TEXT
                );

                CREATE TABLE Posts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AuthorId INTEGER,
                    Date DATETIME,
                    Title TEXT,
                    Content TEXT
                );");
            }

            return connection;
        }
    }
}
