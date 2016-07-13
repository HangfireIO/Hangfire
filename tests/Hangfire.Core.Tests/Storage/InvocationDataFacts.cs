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
                JobHelper.ToJson(new[] { typeof(string) }),
                JobHelper.ToJson(new[] { JobHelper.ToJson("Hello") }),
                @"c:\");

            var job = serializedData.Deserialize();

            Assert.Equal(type, job.Type);
            Assert.Equal(methodInfo, job.Method);
            Assert.Equal("Hello", job.Args[0]);
            Assert.Equal(@"c:\", job.Path);
        }

        [Fact]
        public void Deserialize_WrapsAnException_WithTheJobLoadException()
        {
            var serializedData = new InvocationData(null, null, null, null, null);

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
                JobHelper.ToJson(new[] { typeof(string) }),
                "",
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
            Assert.Equal(JobHelper.ToJson(new[] { "\"Hello\"" }), invocationData.Arguments);
        }

        [Fact]
        public void Deserialize_HandlesGenericTypes()
        {
            var serializedData = InvocationData.Serialize(
                Job.FromExpression<GenericType<string>>(x => x.Method()));

            var job = serializedData.Deserialize();

            Assert.False(job.Type.ContainsGenericParameters);
            Assert.Equal(typeof(string), job.Type.GetGenericArguments()[0]);
        }

        [Fact]
        public void Deserialize_HandlesGenericMethods_WithOpenTypeParameters()
        {
            var serializedData = InvocationData.Serialize(
                Job.FromExpression<GenericType<string>>(x => x.Method("asd", 123)));

            var job = serializedData.Deserialize();

            Assert.False(job.Method.ContainsGenericParameters);
        }

        [Fact]
        public void Deserialize_HandlesMethodsDefinedInInterfaces()
        {
            var serializedData = new InvocationData(
                typeof(IParent).AssemblyQualifiedName,
                "Method",
                "",
                JobHelper.ToJson(new string[0]),
                JobHelper.ToJson(new string[0]));

            var job = serializedData.Deserialize();

            Assert.Equal(typeof(IParent), job.Type);
        }

        [Fact]
        public void Deserialize_HandlesMethodsDefinedInParentInterfaces()
        {
            var serializedData = new InvocationData(
                typeof(IChild).AssemblyQualifiedName,
                "Method",
                "",
                JobHelper.ToJson(new string[0]),
                JobHelper.ToJson(new string[0]));

            var job = serializedData.Deserialize();

            Assert.Equal(typeof(IChild), job.Type);
        }

        public static void Sample(string arg)
        {
        }

        public class GenericType<T1>
        {
            public void Method() { }
            public void Method<T2>(T1 arg1, T2 arg2) { }
        }

        public interface IParent
        {
            void Method();
        }

        public interface IChild : IParent
        {
        }
    }
}
