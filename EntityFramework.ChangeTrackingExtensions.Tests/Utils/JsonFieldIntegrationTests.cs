using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
{
    [TestClass]
    public class JsonFieldIntegrationTests : TestInitializer
    {
        [TestMethod]
        public void TestJsonFieldWithDbContext()
        {
            using (var context = new TestDbContext(_connection))
            {
                var settings = new Settings
                {
                    Key = "first",
                    Value = new { Number = 123, String = "test" },
                };
                
                context.Settings.Add(settings);

                context.SaveChanges();
            }

            using (var context = new TestDbContext(_connection))
            {
                var settings = context.Settings.Find("first");

                Assert.IsNotNull(settings);
                Assert.IsNotNull(settings.Value);
                Assert.AreEqual(123, (int)settings.Value.Number);
                Assert.AreEqual("test", (string)settings.Value.String);
            }
        }
    }
}
