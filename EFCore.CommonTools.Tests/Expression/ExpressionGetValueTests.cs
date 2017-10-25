using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

#if EF_CORE
namespace EntityFrameworkCore.CommonTools.Tests
#elif EF_6
namespace EntityFramework.CommonTools.Tests
#endif
{
    [TestClass]
    public class ExpressionGetValueTests
    {
        [TestMethod]
        public void ShouldAcceptConstant()
        {
            Expression<Func<int>> expression = () => 123;

            object value = expression.Body.GetValue();

            Assert.AreEqual(123, value);
        }

        [TestMethod]
        public void ShouldAcceptClosure()
        {
            int expected = 123;

            Expression<Func<int>> expression = () => expected;

            object value = expression.Body.GetValue();

            Assert.AreEqual(expected, value);
        }

        public int ClassField = 123;

        [TestMethod]
        public void ShouldAcceptClassField()
        {
            Expression<Func<int>> expression = () => ClassField;

            object value = expression.Body.GetValue();

            Assert.AreEqual(ClassField, value);
        }

        public int ClassProperty { get; set; } = 456;

        [TestMethod]
        public void ShouldAcceptClassProperty()
        {
            Expression<Func<int>> expression = () => ClassProperty;

            object value = expression.Body.GetValue();

            Assert.AreEqual(ClassProperty, value);
        }

        class TestObject
        {
            public int Field;
            public int Property { get; set; }
            public int? Nullable { get; set; }
            public int this[int i, int j] => i * j;
        }

        [TestMethod]
        public void ShouldAcceptObjectField()
        {
            var obj = new TestObject { Field = 123 };

            Expression<Func<int>> expression = () => obj.Field;

            object value = expression.Body.GetValue();

            Assert.AreEqual(obj.Field, value);
        }

        [TestMethod]
        public void ShouldAcceptObjectProperty()
        {
            var obj = new TestObject { Property = 456 };

            Expression<Func<int>> expression = () => obj.Property;

            object value = expression.Body.GetValue();

            Assert.AreEqual(obj.Property, value);
        }

        [TestMethod]
        public void ShouldAcceptNullableConversion()
        {
            var obj = new TestObject { Nullable = 456 };

            Expression<Func<int?>> expression = () => obj.Nullable;

            object value = expression.Body.GetValue();

            Assert.AreEqual(obj.Nullable, value);
        }

        [TestMethod]
        public void ShouldAcceptObjectConversion()
        {
            var expected = new TestObject();

            Expression<Func<object>> expression = () => expected;

            object value = expression.Body.GetValue();

            Assert.AreEqual(expected, value);
        }

        [TestMethod]
        public void ShouldAcceptObjectIndexer()
        {
            var obj = new TestObject();
            int i = 123;
            int j = 456;

            Expression<Func<int>> expression = () => obj[i, j];

            object value = expression.Body.GetValue();

            Assert.AreEqual(i * j, value);
        }

        [TestMethod]
        public void ShouldAcceptListIndexer()
        {
            IReadOnlyList<int> list = new List<int> { 1, 2, 3 };
            int i = 1;

            Expression<Func<int>> expression = () => list[i];

            object value = expression.Body.GetValue();

            Assert.AreEqual(list[i], value);
        }

        [TestMethod]
        public void ShouldAcceptArrayIndexer()
        {
            var arr = new[] { 1, 2, 3 };
            int i = 1;

            Expression<Func<int>> expression = () => arr[i];

            object value = expression.Body.GetValue();

            Assert.AreEqual(arr[i], value);
        }

        [TestMethod]
        public void ShouldAcceptArrayLength()
        {
            var arr = new[] { 1, 2, 3 };

            Expression<Func<int>> expression = () => arr.Length;

            object value = expression.Body.GetValue();

            Assert.AreEqual(arr.Length, value);
        }

        [TestMethod]
        public void ShouldAcceptComplexExpressions()
        {
            var obj = new TestObject { Field = 123, Nullable = 123 };
            var list = new List<int> { 1, 2, 3 };

            int expected = obj[list[2], (int)obj.Nullable];

            Expression<Func<object>> expression = () => obj[list[2], (int)obj.Nullable];

            object value = expression.Body.GetValue();

            Assert.AreEqual(expected, value);
        }

        [TestMethod]
        public void ShouldFallBackToExpressionCompile()
        {
            int expected = 123;

            Expression<Func<int>> expression = () => expected * expected;

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
