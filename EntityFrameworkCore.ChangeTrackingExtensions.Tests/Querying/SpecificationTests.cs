#if EF_CORE
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#else
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    [TestClass]
    public class SpecificationTests : TestInitializer
    {
        public class ActiveUserSpec : Specification<User>
        {
            public ActiveUserSpec()
            {
                Predicate = u => !u.IsDeleted;
            }
        }

        [TestMethod]
        public void TestSpec()
        {
            var isDeleted = new Specification<User>(u => u.IsDeleted);
            var loginOlolo = new Specification<User>(u => u.Login == "ololo");
            var spec = loginOlolo && new ActiveUserSpec();

            bool ok = spec.IsSatisfiedBy(new User { Login = "ololo", IsDeleted = false });
        }
    }
}
