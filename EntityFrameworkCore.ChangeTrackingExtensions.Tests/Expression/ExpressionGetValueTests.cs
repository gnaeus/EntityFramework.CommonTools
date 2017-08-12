using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions.Tests
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions.Tests
#endif
{
    [TestClass]
    public class ExpressionGetValueTests
    {
        [TestMethod]
        public void ShouldHandleConstants()
        {
            Expression<Func<int, int>> expression = num => 123;

            object value = expression.Body.GetValue();

            Assert.AreEqual(123, value);
        }

        [TestMethod]
        public void ShouldHandleClosures()
        {
            int expected = 123;

            Expression<Func<int, int>> expression = num => expected;

            object value = expression.Body.GetValue();

            Assert.AreEqual(expected, value);
        }

        [TestMethod]
        public void ShouldHandleExpressions()
        {
            int expected = 123;

            Expression<Func<int, int>> expression = num => expected * expected;

            object value = expression.Body.GetValue();

            Assert.AreEqual(expected * expected, value);
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ShouldFailWithOpenParams()
        {
            int expected = 123;

            Expression<Func<int, int>> expression = num => num * expected;

            object value = expression.Body.GetValue();
        }
    }
}
