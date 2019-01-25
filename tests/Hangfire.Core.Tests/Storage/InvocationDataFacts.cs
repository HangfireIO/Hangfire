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
        public void Deserialize_HandlesNullOrEmpty_ParameterTypesAndArguments()
        {
            var serializedData = new InvocationData(
                "Hangfire.JobStorage",
                "GetConnection",
                String.Empty,
                null);

            var job = serializedData.Deserialize();

            Assert.Equal(job.Type, typeof(JobStorage));
        }

        [Fact]
        public void Deserialize_HandlesTypesWithoutAssemblyName_FromTheSameAssembly()
        {
            var serializedData = new InvocationData(
                "Hangfire.JobStorage",
                "GetConnection",
                null,
                null);

            var job = serializedData.Deserialize();

            Assert.Equal(job.Type, typeof(JobStorage));
        }

        [Fact]
        public void Deserialize_HandlesPartialAssemblyNames()
        {
            var serializedData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "Empty",
                null,
                null);

            var job = serializedData.Deserialize();

            Assert.Equal(job.Type, typeof(InvocationDataFacts));
        }

        [Fact]
        public void Deserialize_HandlesFullAssemblyNames()
        {
            var serializedData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "Empty",
                null,
                null);

            var job = serializedData.Deserialize();

            Assert.Equal(job.Type, typeof(InvocationDataFacts));
        }

        [Fact]
        public void Deserialize_HandlesFullyQualifiedAssemblyNames_OfNonSignedAssembly_OfDifferentVersion()
        {
            try
            {
                GlobalConfiguration.Configuration.UseIgnoredAssemblyVersionTypeResolver();

                var serializedData = new InvocationData(
                    "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=9.9.9.9, Culture=neutral, PublicKeyToken=null",
                    "Empty",
                    null,
                    null);

                var job = serializedData.Deserialize();

                Assert.Equal(job.Type, typeof(InvocationDataFacts));
            }
            finally
            {
                GlobalConfiguration.Configuration.UseDefaultTypeResolver();
            }
        }

        [Fact]
        public void Deserialize_HandlesFullyQualifiedAssemblyNames_OfSignedAssembly_OfDifferentVersion()
        {
            try
            {
                GlobalConfiguration.Configuration.UseIgnoredAssemblyVersionTypeResolver();

                var serializedData = new InvocationData(
                    "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=9.9.9.9, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                    "Empty",
                    null,
                    null);

                var job = serializedData.Deserialize();

                Assert.Equal(job.Type, typeof(InvocationDataFacts));
            }
            finally
            {
                GlobalConfiguration.Configuration.UseDefaultTypeResolver();
            }
        }

        [Fact]
        public void Deserialize_HandlesGenericTypes_WithFullyQualifiedAssemblyNames_OfSignedAssembly_OfDifferentVersion()
        {
            try
            {
                GlobalConfiguration.Configuration.UseIgnoredAssemblyVersionTypeResolver();

                var serializedData = new InvocationData(
                    "Hangfire.Core.Tests.Storage.InvocationDataFacts+GenericType`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], Hangfire.Core.Tests, Version=9.9.9.9, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                    "Method",
                    "[\"System.Int32, System.Private.CoreLib, Version=9.9.9.9, Culture=neutral, PublicKeyToken=lalalalala\",\"System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"]",
                    "[\"123\",\"456\"]");

                var job = serializedData.Deserialize();

                Assert.Equal(job.Type, typeof(GenericType<int>));
                Assert.Equal("Method", job.Method.Name);
                Assert.Equal(123, job.Args[0]);
            }
            finally
            {
                GlobalConfiguration.Configuration.UseDefaultTypeResolver();
            }
        }

        [Fact]
        public void Deserialize_HandlesSystemPrivateCoreLib_TypeForwarding()
        {
            var serializedData = new InvocationData(
                "System.String, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                "IsNullOrEmpty",
                "[\"System.String, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"]",
                JobHelper.ToJson(new[] { JobHelper.ToJson("hello") }));

            var job = serializedData.Deserialize();

            Assert.Equal(typeof(string), job.Type);
            Assert.Equal("IsNullOrEmpty", job.Method.Name);
            Assert.Equal("hello", job.Args[0]);
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
            Assert.Equal(JobHelper.ToJson(new[] { "\"Hello\"" }), invocationData.Arguments);
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

        public static void Empty()
        {
        }

        public static void Sample(string arg)
        {
        }

        public static void ListMethod(IList<string> arg)
        {
        }

        public static void DateTimeMethod(DateTime arg)
        {
        }

        public static void NullableDateTimeMethod(DateTime? arg)
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
