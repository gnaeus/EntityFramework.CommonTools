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
                var element = new SettingsElement
                {
                    Key = "first",
                    Value = new { Number = 123, String = "test" },
                };
                
                context.Settings.Add(element);

                context.SaveChanges();
            }

            using (var context = new TestDbContext(_connection))
            {
                var element = context.Settings.Find("first");

                Assert.IsNotNull(element);
                Assert.IsNotNull(element.Value);
                Assert.AreEqual(123, (int)element.Value.Number);
                Assert.AreEqual("test", (string)element.Value.String);
            }
        }
    }
}
