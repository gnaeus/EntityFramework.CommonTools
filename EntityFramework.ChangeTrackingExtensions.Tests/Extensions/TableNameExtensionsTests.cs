using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
{
    [TestClass]
    public class TableNameExtensionsTests : TestInitializer
    {
        [TestMethod]
        public void ShouldReturnTableAndSchemaName()
        {
            using (var context = CreateSqliteDbContext())
            {
                var tableAndSchema = context.GetTableAndSchemaName(typeof(User));

                Assert.AreEqual("Users", tableAndSchema.TableName);
                Assert.AreEqual("dbo", tableAndSchema.Schema);
            }
        }

        [TestMethod]
        public void ShouldReturnTableAndSchemaNames()
        {
            using (var context = CreateSqliteDbContext())
            {
                var tableAndSchemas = context.GetTableAndSchemaNames(typeof(Post));

                Assert.IsNotNull(tableAndSchemas);
                Assert.AreEqual(1, tableAndSchemas.Length);
                Assert.AreEqual("Posts", tableAndSchemas[0].TableName);
                Assert.AreEqual("dbo", tableAndSchemas[0].Schema);
            }
        }
    }
}
