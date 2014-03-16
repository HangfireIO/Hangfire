using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HangFire.Common;
using HangFire.Common.Filters;
using HangFire.Storage;
using Xunit;

namespace HangFire.Core.Tests.Client
{
    public class JobMethodTests
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenTheTypeIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new JobMethod(null, typeof (TestJob).GetMethod("Perform")));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheMethodIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new JobMethod(typeof (TestJob), null));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheTypeDoesNotContainTheGivenMethod()
        {
            Assert.Throws<ArgumentException>(
                () => new JobMethod(typeof (JobMethod), typeof (TestJob).GetMethod("Perform")));
        }

        [Fact]
        public void Ctor_CorrectlySets_PropertyValues()
        {
            var type = typeof (TestJob);
            var methodInfo = type.GetMethod("Perform");
            var method = new JobMethod(type, methodInfo);

            Assert.Equal(type, method.Type);
            Assert.Equal(methodInfo, method.MethodInfo);
            Assert.False(method.OldFormat);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_AllTheData()
        {
            var type = typeof(TestJob);
            var methodInfo = type.GetMethod("Perform");
            var serializedData = new InvocationData
            {
                Type = type.AssemblyQualifiedName,
                Method = methodInfo.Name,
                ParameterTypes = JobHelper.ToJson(new Type[0])
            };

            var method = JobMethod.Deserialize(serializedData);

            Assert.Equal(type, method.Type);
            Assert.Equal(methodInfo, method.MethodInfo);
            Assert.False(method.OldFormat);
        }

        [Fact]
        public void Deserialize_ThrowsAnException_WhenSerializedDataIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => JobMethod.Deserialize(null));
        }

        [Fact]
        public void Deserialize_WrapsAnException_WithTheJobLoadException()
        {
            var serializedData = new InvocationData();

            Assert.Throws<JobLoadException>(
                () => JobMethod.Deserialize(serializedData));
        }

        [Fact]
        public void Deserialize_ThrowsAnException_WhenTypeCanNotBeFound()
        {
            var serializedData = new InvocationData
            {
                Type = "NonExistingType",
                Method = "Perform",
                ParameterTypes = "",
            };

            Assert.Throws<JobLoadException>(
                () => JobMethod.Deserialize(serializedData));
        }

        [Fact]
        public void Deserialize_ThrowsAnException_WhenMethodCanNotBeFound()
        {
            var serializedData = new InvocationData
            {
                Type = typeof (TestJob).AssemblyQualifiedName,
                Method = "NonExistingMethod",
                ParameterTypes = JobHelper.ToJson(new Type[0])
            };

            Assert.Throws<JobLoadException>(
                () => JobMethod.Deserialize(serializedData));
        }

        [Fact]
        public void FromStaticExpression_ShouldThrowException_WhenNullExpressionProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => JobMethod.FromExpression((Expression<Action>) null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromStaticExpression_ShouldReturnCorrectResult()
        {
            var method = JobMethod.FromExpression(() => Console.WriteLine());

            Assert.Equal(typeof(Console), method.Type);
            Assert.Equal("WriteLine", method.MethodInfo.Name);
        }

        [Fact]
        public void FromInstanceExpression_ShouldThrowException_WhenNullExpressionIsProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => JobMethod.FromExpression<List<int>>(null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromInstanceExpression_ShouldReturnCorrectResult()
        {
// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            var method = JobMethod.FromExpression<String>(x => x.Equals("hello"));

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

        private static JobMethod GetCorrectMethod()
        {
            var type = typeof(TestJob);
            var methodInfo = type.GetMethod("Perform");
            return new JobMethod(type, methodInfo);
        }

        #region Old Client API tests

        [Fact]
        public void Deserialization_FromTheOldFormat_CorrectlySerializesBothTypeAndMethod()
        {
            var serializedData = new InvocationData
            {
                Type = typeof (TestJob).AssemblyQualifiedName
            };

            var method = JobMethod.Deserialize(serializedData);
            Assert.Equal(typeof(TestJob), method.Type);
            Assert.Equal(typeof(TestJob).GetMethod("Perform"), method.MethodInfo);
            Assert.True(method.OldFormat);
        }

        [Fact]
        public void SerializedData_IsNotBeingChanged_DuringTheDeserialization()
        {
            var serializedData = new InvocationData
            {
                Type = typeof (TestJob).AssemblyQualifiedName
            };

            JobMethod.Deserialize(serializedData);
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
