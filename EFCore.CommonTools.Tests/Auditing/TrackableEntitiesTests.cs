using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools.Tests
#elif EF_6
namespace EntityFramework.CommonTools.Tests
#endif
{
    [TestClass]
    public class TrackableEntitiesTests : TestInitializer
    {
        [TestMethod]
        public void TestTrackableEntities()
        {
            using (var context = CreateInMemoryDbContext())
            {
                // insert
                var user = new User();
                context.Users.Add(user);

                context.SaveChanges();

                context.Entry(user).Reload();
                Assert.AreEqual(DateTime.UtcNow.Date, user.CreatedUtc.Date);

                // update
                user.Login = "admin";

                context.SaveChanges();

                context.Entry(user).Reload();
                Assert.IsNotNull(user.UpdatedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, user.UpdatedUtc?.Date);

                // delete
                context.Users.Remove(user);

                context.SaveChanges();

                context.Entry(user).Reload();
                Assert.AreEqual(true, user.IsDeleted);
                Assert.IsNotNull(user.DeletedUtc);
                Assert.AreEqual(DateTime.UtcNow.Date, user.DeletedUtc?.Date);
            }
        }
    }
}
