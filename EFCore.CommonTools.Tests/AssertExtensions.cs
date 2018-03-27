using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools.Tests
#elif EF_6
namespace EntityFramework.CommonTools.Tests
#endif
{
    public static class AssertExtensions
    {
        private class Visitor : ExpressionVisitor
        {
            public List<Expression> Expressions = new List<Expression>();

            public override Expression Visit(Expression node)
            {
                Expressions.Add(node);
                return base.Visit(node);
            }
        }

        public static void MethodCallsAreMatch(this Assert assert, Expression expexted, Expression actual)
        {
            var expectedVisitor = new Visitor();
            expectedVisitor.Visit(expexted);
            var expectedList = expectedVisitor.Expressions;

            var actualVisitor = new Visitor();
            actualVisitor.Visit(actual);
            var actualList = actualVisitor.Expressions;

            Assert.AreEqual(expectedList.Count, actualList.Count);

            for (int i = 0; i < expectedList.Count; i++)
            {
                if (expectedList[i] != null && expectedList[i].NodeType == ExpressionType.Call)
                {
                    Assert.AreEqual(ExpressionType.Call, actualList[i].NodeType);

                    var expectedCall = (MethodCallExpression)expectedList[i];
                    var actualCall = (MethodCallExpression)actualList[i];

                    Assert.AreEqual(expectedCall.Method, actualCall.Method);
                }
            }
        }

        public static void SequenceEqual<T>(this Assert assert, IEnumerable<T> expexted, IEnumerable<T> actual)
        {
            expexted = expexted.ToList();
            actual = actual.ToList();

            Assert.AreEqual(expexted.Count(), actual.Count());
            Assert.IsTrue(expexted.SequenceEqual(actual));
        }
    }
}
