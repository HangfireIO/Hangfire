using System;
using HangFire.Common;
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
        }

        [Fact]
        public void Deserialize_WrapsAnException_WithTheJobLoadException()
        {
            var serializedData = new InvocationData(null, null, null);

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
            var type = typeof (Console);
            var method = type.GetMethod("WriteLine", new[] { typeof (string), typeof (object) });

            var methodData = new MethodData(type, method);
            var invocationData = methodData.Serialize();

            Assert.Equal(typeof(Console).AssemblyQualifiedName, invocationData.Type);
            Assert.Equal("WriteLine", invocationData.Method);
            Assert.Equal(JobHelper.ToJson(new [] { typeof(string), typeof(object) }), invocationData.ParameterTypes);
        }
        
        public class TestJob
        {
            public void Perform()
            {
            }
        }
    }
}
