using System;
using System.Collections.Generic;
using System.Reflection;
using Hangfire.Common;
using Hangfire.Storage;
using Newtonsoft.Json;
using Xunit;
using System.Globalization;

namespace Hangfire.Core.Tests.Storage
{
    public class InvocationDataFacts
    {
        private const string NamespaceName = "Hangfire.Core.Tests.Storage";
        private const string AssemblyName = "Hangfire.Core.Tests";

        [Fact]
        public void Deserialize_CorrectlyDeserializes_AllTheData()
        {
            var type = typeof(InvocationDataFacts);
            var methodInfo = type.GetMethod("Sample");

            var serializedData = new InvocationData(
                type.AssemblyQualifiedName,
                methodInfo.Name,
                JobHelper.ToJson(new [] { typeof(string) }),
                JobHelper.ToJson(new [] { JobHelper.ToJson("Hello") }));

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
                JobHelper.ToJson(new [] { typeof(string) }),
                "");

            Assert.Throws<JobLoadException>(
                () => serializedData.Deserialize());
        }

        [Theory]
        [InlineData("Method", "[]", "[]", null, null)]
        [InlineData("Sample", "[\"System.String\"]", "[\"\\\"Hello\\\"\"]", "[\\\"System.String\\\"]", "[\\\"\\\\\\\"Hello\\\\\\\"\\\"]")]
        public void Serialize_CorrectlySerializesInvocationDataToString(string method, string parameterTypes,
            string args, string expectedParameterTypes, string expectedArgs)
        {
            var type = $"{NamespaceName}.InvocationDataFacts, {AssemblyName}";

            var invocationData = new InvocationData(type, method, parameterTypes, args);

            var expectedString = $"[\"{type}\",\"{method}\"";
            if (expectedParameterTypes != null) expectedString += $",\"{expectedParameterTypes}\"";
            if (expectedArgs != null) expectedString += $",\"{expectedArgs}\"";
            expectedString += "]";

            Assert.Equal(expectedString, invocationData.Serialize());
        }

        [Theory]

        // Previous serialization format.
        [InlineData("{\"$type\":\"Hangfire.Storage.InvocationData, Hangfire.Core\",\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"Method\":\"Sample\",\"ParameterTypes\":\"{\\\"$type\\\":\\\"System.Type[], mscorlib\\\",\\\"$values\\\":[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]}\",\"Arguments\":\"{\\\"$type\\\":\\\"System.String[], mscorlib\\\",\\\"$values\\\":[\\\"\\\\\\\"Hello\\\\\\\"\\\"]}\"}")]
        [InlineData("{\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"Method\":\"Sample\",\"ParameterTypes\":\"{\\\"$type\\\":\\\"System.Type[], mscorlib\\\",\\\"$values\\\":[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]}\",\"Arguments\":\"{\\\"$type\\\":\\\"System.String[], mscorlib\\\",\\\"$values\\\":[\\\"\\\\\\\"Hello\\\\\\\"\\\"]}\"}")]
        [InlineData("{\"$type\":\"Hangfire.Storage.InvocationData, Hangfire.Core\",\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"Method\":\"Sample\",\"ParameterTypes\":\"[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]\",\"Arguments\":\"[\\\"\\\\\\\"Hello\\\\\\\"\\\"]\"}")]
        [InlineData("{\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"Method\":\"Sample\",\"ParameterTypes\":\"[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]\",\"Arguments\":\"[\\\"\\\\\\\"Hello\\\\\\\"\\\"]\"}")]

        // New serialization format.
        [InlineData("[\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"Sample\",\"[\\\"System.String\\\"]\",\"[\\\"\\\\\\\"Hello\\\\\\\"\\\"]\"]")]
        public void Deserialize_DeserializesCorrectlyStringToInvocationData(string invocationData)
        {
            var serializedData = InvocationData.Deserialize(invocationData);

            var job = serializedData.Deserialize();

            Assert.False(job.Type.GetTypeInfo().ContainsGenericParameters);
            Assert.Equal("Sample", job.Method.Name);
            Assert.Equal(typeof(string), job.Method.GetParameters()[0].ParameterType);
            Assert.Equal(1, job.Args.Count);
            Assert.Equal("Hello", job.Args[0]);
        }

        [Fact]
        public void Deserialize_DeserializesCorrectlyShortFormatStringToInvocationData()
        {
            var invocationData = "[\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"Method\"]";

            var serializedData = InvocationData.Deserialize(invocationData);

            var job = serializedData.Deserialize();

            Assert.False(job.Type.GetTypeInfo().ContainsGenericParameters);
            Assert.Equal("Method", job.Method.Name);
            Assert.Equal(0, job.Method.GetParameters().Length);
            Assert.Equal(0, job.Args.Count);
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
                JobHelper.ToJson(new Type[0]),
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
                JobHelper.ToJson(new Type[0]),
                JobHelper.ToJson(new string[0]));

            var job = serializedData.Deserialize();

            Assert.Equal(typeof(IChild), job.Type);
        }

        [Fact]
        public void Deserialize_RethrowsJsonException_InsteadOfNullValue_WhenReferenceConverterChosen()
        {
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                "ListMethod",
                JobHelper.ToJson(new [] { typeof(IList<string>) }),
                JobHelper.ToJson(new [] { "asdfasdf" }));

            var exception = Assert.Throws<JobLoadException>(() => serializedData.Deserialize());
            Assert.IsType<JsonReaderException>(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(MemberData))]
        public void Serialize_CorrectlySerializesJobToInvocationData(Job job, string className, string parameterTypes, string serializedArgs, bool isGlobalNameSpace)
        {
            var prefix = isGlobalNameSpace ? "" : $"{NamespaceName}.";

            var methodName = job.Method.Name;
            var invocationData = InvocationData.Serialize(job);

            Assert.Equal($"{prefix}{className}, {AssemblyName}", invocationData.Type);
            Assert.Equal(methodName, invocationData.Method);
            Assert.Equal(parameterTypes, invocationData.ParameterTypes);
            Assert.Equal(serializedArgs, invocationData.Arguments);
        }

        public static IEnumerable<object[]> MemberData
        {
            get
            {
                return new []
                {
                    new object[] { Job.FromExpression(() => Sample("str1")), "InvocationDataFacts", "[\"System.String\"]", "[\"\\\"str1\\\"\"]", false },
                    new object[] { Job.FromExpression(() => ListMethod(new string[0])), nameof(InvocationDataFacts), "[\"System.Collections.Generic.IList`1[[System.String]]\"]", "[\"[]\"]", false },

                    new object[] { Job.FromExpression(() => GenericMethod(1)), "InvocationDataFacts", "[\"System.Int32\"]", "[\"1\"]", false },
                    new object[] { Job.FromExpression(() => GenericMethod(new InvocationDataFacts())), "InvocationDataFacts", $"[\"{NamespaceName}.InvocationDataFacts, {AssemblyName}\"]", "[\"{}\"]", false },
                    new object[] { Job.FromExpression(() => GenericMethod(new GlobalType())), "InvocationDataFacts", $"[\"GlobalType, {AssemblyName}\"]", "[\"{}\"]", false },
                    new object[] { Job.FromExpression(() => OtherGenericMethod(1, new List<int>())), "InvocationDataFacts", "[\"System.Int32\",\"System.Collections.Generic.List`1[[System.Int32]]\"]", "[\"1\",\"[]\"]", false },

                    new object[] { Job.FromExpression<NestedType>(x => x.Method()), "InvocationDataFacts+NestedType", "[]", "[]", false },
                    new object[] { Job.FromExpression<NestedType>(x => x.NestedGenericMethod(1)), "InvocationDataFacts+NestedType", "[\"System.Int32\"]", "[\"1\"]", false },

                    new object[] { Job.FromExpression<GenericType<int>>(x => x.Method()), "InvocationDataFacts+GenericType`1[[System.Int32]]", "[]", "[]", false },
                    new object[] { Job.FromExpression<GenericType<GlobalType>>(x => x.Method()), "InvocationDataFacts+GenericType`1[[GlobalType, Hangfire.Core.Tests]]", "[]", "[]", false },
                    new object[] { Job.FromExpression<GenericType<InvocationDataFacts>>(x => x.Method()), $"InvocationDataFacts+GenericType`1[[{NamespaceName}.InvocationDataFacts, {AssemblyName}]]", "[]", "[]", false },
                    new object[] { Job.FromExpression<GenericType<int>>(x => x.Method(1, 1)), "InvocationDataFacts+GenericType`1[[System.Int32]]", "[\"System.Int32\",\"System.Int32\"]", "[\"1\",\"1\"]", false },
                    new object[] { Job.FromExpression<GenericType<int>.NestedGenericType<string>>(x => x.Method(1, "1")), "InvocationDataFacts+GenericType`1+NestedGenericType`1[[System.Int32],[System.String]]", "[\"System.Int32\",\"System.String\"]", "[\"1\",\"\\\"1\\\"\"]", false },

                    new object[] { Job.FromExpression<GlobalType>(x => x.Method()), "GlobalType", "[]", "[]", true},
                    new object[] { Job.FromExpression<GlobalType>(x => x.GenericMethod(1)), "GlobalType", "[\"System.Int32\"]", "[\"1\"]", true},
                    new object[] { Job.FromExpression<GlobalType.NestedType>(x => x.NestedMethod()), "GlobalType+NestedType", "[]", "[]", true},
                    new object[] { Job.FromExpression<GlobalType.NestedGenericType<long>>(x => x.NestedGenericMethod(1, 1)), "GlobalType+NestedGenericType`1[[System.Int64]]", "[\"System.Int64\",\"System.Int32\"]", "[\"1\",\"1\"]", true},

                    new object[] { Job.FromExpression<GlobalGenericType<int>>(x => x.Method()), "GlobalGenericType`1[[System.Int32]]", "[]", "[]", true},
                    new object[] { Job.FromExpression<GlobalGenericType<object>>(x => x.GenericMethod(1)), "GlobalGenericType`1[[System.Object]]", "[\"System.Int32\"]", "[\"1\"]", true},
                    new object[] { Job.FromExpression<GlobalGenericType<int>.NestedType>(x => x.Method()), "GlobalGenericType`1+NestedType[[System.Int32]]", "[]", "[]", true},
                    new object[] { Job.FromExpression<GlobalGenericType<long>.NestedGenericType<int>>(x => x.Method(1, 1)), "GlobalGenericType`1+NestedGenericType`1[[System.Int64],[System.Int32]]", "[\"System.Int64\",\"System.Int32\"]", "[\"1\",\"1\"]", true},
                };
            }
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_LocalDateTimeArguments_ConvertedToRoundtripFormat()
        {
            var value = DateTime.Now;
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(DateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime) }),
                JobHelper.ToJson(new[] { value.ToString("o", CultureInfo.InvariantCulture) }));

            var job = serializedData.Deserialize();

            Assert.Equal(value, (DateTime)job.Args[0]);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_UnknownDateTimeArguments_ConvertedToRoundtripFormat()
        {
            var value = new DateTime(2017, 1, 1, 1, 1, 1, 1, DateTimeKind.Unspecified);
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(DateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime) }),
                JobHelper.ToJson(new[] { value.ToString("o", CultureInfo.InvariantCulture) }));

            var job = serializedData.Deserialize();

            Assert.Equal(value, (DateTime)job.Args[0]);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_UtcDateTimeArguments_ConvertedToRoundtripFormat()
        {

            var value = DateTime.UtcNow;
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(DateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime) }),
                JobHelper.ToJson(new[] { value.ToString("o", CultureInfo.InvariantCulture) }));

            var job = serializedData.Deserialize();

            Assert.Equal(value, (DateTime)job.Args[0]);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_LocalDateTimeArguments_ConvertedToOldFormat_WithLoweredPrecision()
        {
            var value = DateTime.Now;
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(DateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime) }),
                JobHelper.ToJson(new[] { value.ToString("MM/dd/yyyy HH:mm:ss.ffff", CultureInfo.InvariantCulture) }));

            var job = serializedData.Deserialize();

            var actualValue = (DateTime)job.Args[0];

            Assert.Equal(value.Year, actualValue.Year);
            Assert.Equal(value.Month, actualValue.Month);
            Assert.Equal(value.Day, actualValue.Day);
            Assert.Equal(value.Hour, actualValue.Hour);
            Assert.Equal(value.Minute, actualValue.Minute);
            Assert.Equal(value.Second, actualValue.Second);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_UnknownDateTimeArguments_ConvertedToOldFormat_WithLoweredPrecision()
        {
            var value = new DateTime(2017, 1, 1, 1, 1, 1, 1, DateTimeKind.Unspecified);
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(DateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime) }),
                JobHelper.ToJson(new[] { value.ToString("MM/dd/yyyy HH:mm:ss.ffff", CultureInfo.InvariantCulture) }));

            var job = serializedData.Deserialize();

            var actualValue = (DateTime)job.Args[0];

            Assert.Equal(value.Year, actualValue.Year);
            Assert.Equal(value.Month, actualValue.Month);
            Assert.Equal(value.Day, actualValue.Day);
            Assert.Equal(value.Hour, actualValue.Hour);
            Assert.Equal(value.Minute, actualValue.Minute);
            Assert.Equal(value.Second, actualValue.Second);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_UtcDateTimeArguments_ConvertedToOldFormat_WithLoweredPrecision()
        {
            var value = DateTime.UtcNow;

            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(DateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime) }),
                JobHelper.ToJson(new[] { value.ToString("MM/dd/yyyy HH:mm:ss.ffff", CultureInfo.InvariantCulture) }));

            var job = serializedData.Deserialize();

            var actualValue = (DateTime)job.Args[0];

            Assert.Equal(value.Year, actualValue.Year);
            Assert.Equal(value.Month, actualValue.Month);
            Assert.Equal(value.Day, actualValue.Day);
            Assert.Equal(value.Hour, actualValue.Hour);
            Assert.Equal(value.Minute, actualValue.Minute);
            Assert.Equal(value.Second, actualValue.Second);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_NullableUtcDateTimeArguments()
        {
            DateTime? value = DateTime.UtcNow;
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(NullableDateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime?) }),
                JobHelper.ToJson(new[] { value.Value.ToString("o", CultureInfo.InvariantCulture) }));

            var job = serializedData.Deserialize();

            Assert.Equal(value, job.Args[0]);
        }

        [Fact]
        public void Deserialize_CorrectlySeserializes_NullableUtcDateTimeArguments_With_Null()
        {
            DateTime? value = null;

            var serializedData = InvocationData.Serialize(Job.FromExpression(() => NullableDateTimeMethod(value)));

            var job = serializedData.Deserialize();

            Assert.Equal(value, job.Args[0]);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_NullableLocalDateTimeArguments()
        {
            DateTime? value = DateTime.Now;
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(NullableDateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime?) }),
                JobHelper.ToJson(new[] { value.Value.ToString("o", CultureInfo.InvariantCulture) }));

            var job = serializedData.Deserialize();

            Assert.Equal(value, job.Args[0]);
        }

        [Fact]
        public void Deserialize_CorrectlyDeserializes_NullableDateTimeArguments_With_Null_Value()
        {
            DateTime? value = null;
            var result = value is DateTime;
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(NullableDateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime?) }),
                JobHelper.ToJson(new[] { value }));

            var job = serializedData.Deserialize();

            Assert.Equal(value, job.Args[0]);
        }

        public static void Method()
        {
        }

        public static void Sample(string arg)
        {
        }

        public static void ListMethod(IList<string> arg)
        {
        }

        public static void GenericMethod<T>(T arg)
        {
        }

        public static void OtherGenericMethod<T1,T2>(T1 arg1, T2 arg2)
        {
        }

        public static void DateTimeMethod(DateTime arg)
        {
        }

        public static void NullableDateTimeMethod(DateTime? arg)
        {
        }

        public class NestedType
        {
            public void Method() { }
            public void NestedGenericMethod<T>(T arg1) { }
        }

        public class GenericType<T1>
        {
            public void Method()
            {
            }

            public void Method<T2>(T1 arg1, T2 arg2)
            {
            }

            public class NestedGenericType<T2>
            {
                public void Method(T1 arg1, T2 arg2)
                {
                }
            }
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

public class GlobalType
{
    public void Method() {}
    public void GenericMethod<T>(T arg) {}

    public class NestedType
    {
        public void NestedMethod() { }
    }

    public class NestedGenericType<T>
    {
        public void NestedGenericMethod<T1>(T arg1, T1 arg2) { }
    }
}

public class GlobalGenericType<T>
{
    public void Method() { }
    public void GenericMethod<T1>(T1 arg) { }

    public class NestedType
    {
        public void Method() { }
    }

    public class NestedGenericType<T1>
    {
        public void Method(T arg1, T1 arg2) { }
    }
}