using System.Data.Common;
using System.Data.SQLite;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
{
    public abstract class TestInitializer
    {
        protected DbConnection _connection;

        [TestInitialize]
        public void TestInitialize()
        {
            _connection = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");

            _connection.Open();

            _connection.Execute(@"
                CREATE TABLE _TransactionLog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransactionId BLOB,
                    CreatedUtc DATETIME,
                    Operation TEXT,
                    Schema TEXT,
                    TableName TEXT,
                    EntityType TEXT,
                    EntityJson TEXT
                );

                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Login TEXT,
                    UserContacts TEXT,

                    IsDeleted BOOLEAN,
                    CreatedUtc DATETIME,
                    UpdatedUtc DATETIME,
                    DeletedUtc DATETIME
                );

                CREATE TABLE Posts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT,
                    Content TEXT,
                    TagsJson TEXT,

                    IsDeleted BOOLEAN,
                    CreatedUtc DATETIME,
                    CreatorUserId INTEGER,
                    UpdatedUtc DATETIME,
                    UpdaterUserId INTEGER,
                    DeletedUtc DATETIME,
                    DeleterUserId INTEGER,

                    RowVersion TEXT
                );

                CREATE TABLE Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT,

                    IsDeleted BOOLEAN,
                    CreatedUtc DATETIME,
                    CreatorUser TEXT,
                    UpdatedUtc DATETIME,
                    UpdaterUser TEXT,
                    DeletedUtc DATETIME,
                    DeleterUser TEXT,

                    RowVersion INTEGER DEFAULT 0
                );

                CREATE TRIGGER TRG_Settings_UPD
                    AFTER UPDATE ON Settings
                    WHEN old.RowVersion = new.RowVersion
                BEGIN
                    UPDATE Settings
                    SET RowVersion = RowVersion + 1;
                END;");
        }

        protected TestDbContext CreateTestDbContext()
        {
            return new TestDbContext(_connection);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _connection.Close();
        }
    }
}
