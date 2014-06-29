using System;
using Hangfire.Common;
using Hangfire.Storage;
using Xunit;

namespace Hangfire.Core.Tests.Storage
{
    public class InvocationDataFacts
    {
        [Fact]
        public void Deserialize_CorrectlyDeserializes_AllTheData()
        {
            var type = typeof(InvocationDataFacts);
            var methodInfo = type.GetMethod("Sample");

            var serializedData = new InvocationData(
                type.AssemblyQualifiedName,
                methodInfo.Name,
                JobHelper.ToJson(new [] { typeof(string) }),
                JobHelper.ToJson(new [] { "Hello" }));

            var job = serializedData.Deserialize();

            Assert.Equal(type, job.Type);
            Assert.Equal(methodInfo, job.Method);
            Assert.Equal("Hello", job.Arguments[0]);
        }

        [Fact]
        public void Deserialize_WrapsAnException_WithTheJobLoadException()
        {
            var serializedData = new InvocationData(null, null, null, null);

            Assert.Throws<JobLoadException>(
                () => serializedData.Deserialize());
        }

        [Fact]
        public void Deserialize_ThrowsAnException_WhenTypeCanNotBeFound()
        {
            var serializedData = new InvocationData(
                "NonExistingType",
                "Perform",
                "",
                "");

            Assert.Throws<JobLoadException>(
                () => serializedData.Deserialize());
        }

        [Fact]
        public void Deserialize_ThrowsAnException_WhenMethodCanNotBeFound()
        {
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                "NonExistingMethod",
                JobHelper.ToJson(new [] { typeof(string) }),
                "");

            Assert.Throws<JobLoadException>(
                () => serializedData.Deserialize());
        }

        [Fact]
        public void Serialize_CorrectlySerializesTheData()
        {
            var job = Job.FromExpression(() => Sample("Hello"));
            var invocationData = InvocationData.Serialize(job);

            Assert.Equal(typeof(InvocationDataFacts).AssemblyQualifiedName, invocationData.Type);
            Assert.Equal("Sample", invocationData.Method);
            Assert.Equal(JobHelper.ToJson(new[] { typeof(string) }), invocationData.ParameterTypes);
            Assert.Equal(JobHelper.ToJson(new[] { "Hello" }), invocationData.Arguments);
        }

        public static void Sample(string arg)
        {
        }
    }
}
