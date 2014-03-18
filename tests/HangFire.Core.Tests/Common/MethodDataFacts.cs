using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HangFire.Common;
using HangFire.Common.Filters;
using HangFire.Storage;
using Xunit;

namespace HangFire.Core.Tests.Common
{
    public class MethodDataFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenTheTypeIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new MethodData(null, typeof (TestJob).GetMethod("Perform")));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheMethodIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new MethodData(typeof (TestJob), null));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheTypeDoesNotContainTheGivenMethod()
        {
            Assert.Throws<ArgumentException>(
                () => new MethodData(typeof (MethodData), typeof (TestJob).GetMethod("Perform")));
        }

        [Fact]
        public void Ctor_CorrectlySets_PropertyValues()
        {
            var type = typeof (TestJob);
            var methodInfo = type.GetMethod("Perform");
            var method = new MethodData(type, methodInfo);

            Assert.Equal(type, method.Type);
            Assert.Equal(methodInfo, method.MethodInfo);
            Assert.False(method.OldFormat);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_AllTheData()
        {
            var type = typeof(TestJob);
            var methodInfo = type.GetMethod("Perform");
            var serializedData = new InvocationData(
                type.AssemblyQualifiedName,
                methodInfo.Name,
                JobHelper.ToJson(new Type[0]));

            var method = MethodData.Deserialize(serializedData);

            Assert.Equal(type, method.Type);
            Assert.Equal(methodInfo, method.MethodInfo);
            Assert.False(method.OldFormat);
        }

        [Fact]
        public void Deserialize_WrapsAnException_WithTheJobLoadException()
        {
            var serializedData = new InvocationData();

            Assert.Throws<JobLoadException>(
                () => MethodData.Deserialize(serializedData));
        }

        [Fact]
        public void Deserialize_ThrowsAnException_WhenTypeCanNotBeFound()
        {
            var serializedData = new InvocationData(
                "NonExistingType",
                "Perform",
                "");

            Assert.Throws<JobLoadException>(
                () => MethodData.Deserialize(serializedData));
        }

        [Fact]
        public void Deserialize_ThrowsAnException_WhenMethodCanNotBeFound()
        {
            var serializedData = new InvocationData(
                typeof (TestJob).AssemblyQualifiedName,
                "NonExistingMethod",
                JobHelper.ToJson(new Type[0]));

            Assert.Throws<JobLoadException>(
                () => MethodData.Deserialize(serializedData));
        }

        [Fact]
        public void Serialize_CorrectlySerializesTheData()
        {
            var methodData = MethodData.FromExpression(() => Console.WriteLine("Hello {0}!", "world"));
            var invocationData = methodData.Serialize();

            Assert.Equal(typeof(Console).AssemblyQualifiedName, invocationData.Type);
            Assert.Equal("WriteLine", invocationData.Method);
            Assert.Equal(JobHelper.ToJson(new [] { typeof(string), typeof(object) }), invocationData.ParameterTypes);
        }

        [Fact]
        public void FromStaticExpression_ShouldThrowException_WhenNullExpressionProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => MethodData.FromExpression(null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromStaticExpression_ShouldReturnCorrectResult()
        {
            var method = MethodData.FromExpression(() => Console.WriteLine());

            Assert.Equal(typeof(Console), method.Type);
            Assert.Equal("WriteLine", method.MethodInfo.Name);
        }

        [Fact]
        public void FromInstanceExpression_ShouldThrowException_WhenNullExpressionIsProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => MethodData.FromExpression<List<int>>(null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromInstanceExpression_ShouldReturnCorrectResult()
        {
// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            var method = MethodData.FromExpression<String>(x => x.Equals("hello"));

            Assert.Equal(typeof(String), method.Type);
            Assert.Equal("Equals", method.MethodInfo.Name);
        }

        [Fact]
        public void GetTypeFilterAttributes_ReturnsCorrectAttributes()
        {
            var method = GetCorrectMethod();
            var nonCachedAttributes = method.GetTypeFilterAttributes(false).ToArray();
            var cachedAttributes = method.GetTypeFilterAttributes(true).ToArray();

            Assert.Equal(1, nonCachedAttributes.Length);
            Assert.Equal(1, cachedAttributes.Length);

            Assert.True(nonCachedAttributes[0] is TestTypeAttribute);
            Assert.True(cachedAttributes[0] is TestTypeAttribute);
        }

        [Fact]
        public void GetMethodFilterAttributes_ReturnsCorrectAttributes()
        {
            var method = GetCorrectMethod();
            var nonCachedAttributes = method.GetMethodFilterAttributes(false).ToArray();
            var cachedAttributes = method.GetMethodFilterAttributes(true).ToArray();
            
            Assert.Equal(1, nonCachedAttributes.Length);
            Assert.Equal(1, cachedAttributes.Length);

            Assert.True(nonCachedAttributes[0] is TestMethodAttribute);
            Assert.True(cachedAttributes[0] is TestMethodAttribute);
        }

        private static MethodData GetCorrectMethod()
        {
            var type = typeof(TestJob);
            var methodInfo = type.GetMethod("Perform");
            return new MethodData(type, methodInfo);
        }

        #region Old Client API tests

        [Fact]
        public void Deserialization_FromTheOldFormat_CorrectlySerializesBothTypeAndMethod()
        {
            var serializedData = new InvocationData(
                typeof (TestJob).AssemblyQualifiedName,
                null,
                null);

            var method = MethodData.Deserialize(serializedData);
            Assert.Equal(typeof(TestJob), method.Type);
            Assert.Equal(typeof(TestJob).GetMethod("Perform"), method.MethodInfo);
            Assert.True(method.OldFormat);
        }

        [Fact]
        public void SerializedData_IsNotBeingChanged_DuringTheDeserialization()
        {
            var serializedData = new InvocationData(
                typeof (TestJob).AssemblyQualifiedName,
                null,
                null);

            MethodData.Deserialize(serializedData);
            Assert.Null(serializedData.Method);
        }

        public class TestTypeAttribute : JobFilterAttribute
        {
        }

        public class TestMethodAttribute : JobFilterAttribute
        {
        }

        [TestType]
        public class TestJob : BackgroundJob
        {
            [TestMethod]
            public override void Perform()
            {
            }
        }

        #endregion
    }
}
