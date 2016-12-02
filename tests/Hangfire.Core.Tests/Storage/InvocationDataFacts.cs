using System;
using System.Collections.Generic;
using System.Reflection;
using Hangfire.Common;
using Hangfire.Storage;
using Newtonsoft.Json;
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
                SerializationHelper.Serialize(new [] { typeof(string) }),
                SerializationHelper.Serialize(new [] { SerializationHelper.Serialize("Hello", SerializationOption.User) }));

            var job = serializedData.Deserialize();

            Assert.Equal(type, job.Type);
            Assert.Equal(methodInfo, job.Method);
            Assert.Equal("Hello", job.Args[0]);
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
                SerializationHelper.Serialize(new [] { typeof(string) }),
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
            Assert.Equal(SerializationHelper.Serialize(new[] { typeof(string) }), invocationData.ParameterTypes);
            Assert.Equal(SerializationHelper.Serialize(new[] { "\"Hello\"" }), invocationData.Arguments);
        }

        [Fact]
        public void Deserialize_HandlesGenericTypes()
        {
            var serializedData = InvocationData.Serialize(
                Job.FromExpression<GenericType<string>>(x => x.Method()));

            var job = serializedData.Deserialize();

            Assert.False(job.Type.GetTypeInfo().ContainsGenericParameters);
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
                SerializationHelper.Serialize(new Type[0]),
                SerializationHelper.Serialize(new string[0]));

            var job = serializedData.Deserialize();

            Assert.Equal(typeof(IParent), job.Type);
        }

        [Fact]
        public void Deserialize_HandlesMethodsDefinedInParentInterfaces()
        {
            var serializedData = new InvocationData(
                typeof(IChild).AssemblyQualifiedName,
                "Method",
                SerializationHelper.Serialize(new Type[0]),
                SerializationHelper.Serialize(new string[0]));

            var job = serializedData.Deserialize();

            Assert.Equal(typeof(IChild), job.Type);
        }

        [Fact]
        public void Deserialize_RethrowsJsonException_InsteadOfNullValue_WhenReferenceConverterChosen()
        {
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                "ListMethod",
                SerializationHelper.Serialize(new [] { typeof(IList<string>) }),
                SerializationHelper.Serialize(new [] { "asdfasdf" }));

            var exception = Assert.Throws<JobLoadException>(() => serializedData.Deserialize());
            Assert.IsType<JsonReaderException>(exception.InnerException);
        }

        [Fact, CleanJsonSerializersSettings]
        public void Deserialize_HandlesChangingProcessOfInternalDataSerialization()
        {
            SerializationHelper.SetUserSerializerSettings(SerializerSettingsHelper.DangerousSettings);

            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                "ComplicatedMethod",
                SerializationHelper.Serialize(new[]
                {
                    typeof(IList<string>),
                    typeof(SomeClass)
                }, SerializationOption.User),
                SerializationHelper.Serialize(new[]
                {
                    SerializationHelper.Serialize(new List<string> { "one", "two" }, SerializationOption.User),
                    SerializationHelper.Serialize(new SomeClass { StringValue = "value" }, SerializationOption.User)
                }, SerializationOption.User));

            var job = serializedData.Deserialize();

            Assert.Equal(typeof(InvocationDataFacts), job.Type);
            Assert.Equal(2, job.Args.Count);

            Assert.Equal(typeof(List<string>), job.Args[0].GetType());
            Assert.Equal("one", (job.Args[0] as List<string>)?[0]);
            Assert.Equal("two", (job.Args[0] as List<string>)?[1]);

            Assert.Equal(typeof(SomeClass), job.Args[1].GetType());
            Assert.Equal("value", (job.Args[1] as SomeClass)?.StringValue);
            Assert.Equal(0, (job.Args[1] as SomeClass)?.DefaultValue);
            Assert.Equal(null, (job.Args[1] as SomeClass)?.NullObject);
        }

        public static void Sample(string arg)
        {
        }

        public static void ListMethod(IList<string> arg)
        {
        }

        public static void ComplicatedMethod(IList<string> arg, SomeClass objArg)
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

        public class SomeClass
        {
            public string StringValue { get; set; }

            public object NullObject { get; set; }

            public int DefaultValue { get; set; }
        }
        
    }
}
