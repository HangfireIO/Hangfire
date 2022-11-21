using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using Hangfire.Common;
using Hangfire.Storage;
using Newtonsoft.Json;
using Xunit;
using System.Globalization;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Serialization;

#pragma warning disable 618

namespace Hangfire.Core.Tests.Storage
{
    public class InvocationDataFacts
    {
        [DataCompatibilityRangeFact]
        public void Deserialize_CorrectlyDeserializes_AllTheData()
        {
            var type = typeof(InvocationDataFacts);
            var methodInfo = type.GetMethod("Sample");

            var serializedData = new InvocationData(
                type.AssemblyQualifiedName,
                // ReSharper disable once PossibleNullReferenceException
                methodInfo.Name,
                JobHelper.ToJson(new [] { typeof(string) }),
                JobHelper.ToJson(new [] { JobHelper.ToJson("Hello") }));

            var job = serializedData.Deserialize();

            Assert.Equal(type, job.Type);
            Assert.Equal(methodInfo, job.Method);
            Assert.Equal("Hello", job.Args[0]);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_HandlesNullOrEmpty_ParameterTypesAndArguments()
        {
            var serializedData = new InvocationData(
                "Hangfire.JobStorage, Hangfire.Core",
                "GetConnection",
                String.Empty,
                null);

            var job = serializedData.Deserialize();

            Assert.Equal(job.Type, typeof(JobStorage));
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_HandlesTypesWithoutAssemblyName_FromMscorlibAssembly()
        {
            var serializedData = new InvocationData(
                "System.DateTime",
                "IsLeapYear",
                "[\"System.Int32\"]",
                "[\"1\"]");

            var job = serializedData.Deserialize();

            Assert.Equal(job.Type, typeof(DateTime));
        }

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
        public void Deserialize_WrapsAnException_WithTheJobLoadException()
        {
            var serializedData = new InvocationData(null, null, null, null);

            Assert.Throws<JobLoadException>(
                () => serializedData.Deserialize());
        }

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
        public void Serialize_CorrectlySerializesTheData()
        {
            var job = Job.FromExpression(() => Sample("Hello"));

            var invocationData = InvocationData.Serialize(job);

            Assert.Equal(typeof(InvocationDataFacts).AssemblyQualifiedName, invocationData.Type);
            Assert.Equal("Sample", invocationData.Method);
            Assert.Equal(JobHelper.ToJson(new[] { typeof(string) }), invocationData.ParameterTypes);
            Assert.Equal(JobHelper.ToJson(new[] { "\"Hello\"" }), invocationData.Arguments);
        }

        [DataCompatibilityRangeFact]
        public void Serialize_CorrectlyHandles_ParameterTypes_InPossibleOldFormat()
        {
            var invocationData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "ComplicatedMethod",
                "{\"$type\":\"System.Type[], mscorlib\",\"$values\":[\"System.Collections.Generic.IList`1[[System.String, mscorlib]], mscorlib\",\"Hangfire.Core.Tests.Storage.InvocationDataFacts+SomeClass, Hangfire.Core.Tests\"]}",
                "[null, null]");

            var serialized = invocationData.SerializePayload();
            var job = InvocationData.DeserializePayload(serialized).Deserialize();

            Assert.Equal(typeof(InvocationDataFacts), job.Type);
            Assert.Equal(typeof(InvocationDataFacts).GetMethod("ComplicatedMethod"), job.Method);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void Serialize_SerializesDateTimeUsingCustomFormatter_BeforeVersion170()
        {
            var dateTimeString = "2019-03-05T13:20:04.5932150Z";
            var dateTime = DateTime.Parse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var dateTimeData = InvocationData.SerializeJob(Job.FromExpression(() => DateTimeMethod(dateTime)));
            var nullableData = InvocationData.SerializeJob(Job.FromExpression(() => NullableDateTimeMethod(dateTime)));

            Assert.Equal($"[\"{dateTimeString}\"]", dateTimeData.Arguments);
            Assert.Equal($"[\"{dateTimeString}\"]", nullableData.Arguments);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void Serialize_SerializesDateTimeUsingRegularJsonFormatter_AfterVersion170()
        {
            var dateTimeString = "\"2019-03-05T13:20:04.5932150Z\"";
            var dateTime = SerializationHelper.Deserialize<DateTime>(dateTimeString, SerializationOption.User);
            var dateTimeData = InvocationData.SerializeJob(Job.FromExpression(() => DateTimeMethod(dateTime)));
            var nullableData = InvocationData.SerializeJob(Job.FromExpression(() => NullableDateTimeMethod(dateTime)));

            Assert.Equal($"[\"\\\"2019-03-05T13:20:04.593215Z\\\"\"]", dateTimeData.Arguments);
            Assert.Equal($"[\"\\\"2019-03-05T13:20:04.593215Z\\\"\"]", nullableData.Arguments);
        }

        [DataCompatibilityRangeFact(), CleanSerializerSettings]
        public void Serialize_WithTypeNameHandlingAuto_PreservesTypeInformation()
        {
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                TypeNameHandling = TypeNameHandling.Auto
            });

            var job = Job.FromExpression(() => GenericMethod<object>(new SomeClass()));
            var data = InvocationData.SerializeJob(job);

            Assert.Equal("[\"{\\\"$type\\\":\\\"Hangfire.Core.Tests.Storage.InvocationDataFacts+SomeClass, Hangfire.Core.Tests\\\"}\"]", data.Arguments);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void Deserialize_CanHandleArgumentWithExplicitTypeName_WhenUsingTypeNameHandlingAuto()
        {
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling =  TypeNameHandling.Auto
            });

            var data = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "GenericMethod",
                "[\"System.Object, mscorlib\"]",
                "[\"{\\\"$type\\\":\\\"Hangfire.Core.Tests.Storage.InvocationDataFacts+SomeClass, Hangfire.Core.Tests\\\"}\"]");

            var job = data.DeserializeJob();
            Assert.Equal("GenericMethod", job.Method.Name);
            Assert.Equal(new object[] { typeof(object) }, job.Method.GetParameters().Select(x => x.ParameterType).ToArray());
            Assert.IsType<SomeClass>(job.Args[0]);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void SerializePayload_CorrectlySerializesInvocationDataToString_WithOldFormat_InVersion_Pre_170()
        {
            var invocationData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "Sample",
                "[\"System.String\"]",
                "[\"\\\"Hello\\\"\"]");

            var payload = invocationData.SerializePayload();

            Assert.Equal(
                "{\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"Method\":\"Sample\",\"ParameterTypes\":\"[\\\"System.String\\\"]\",\"Arguments\":\"[\\\"\\\\\\\"Hello\\\\\\\"\\\"]\"}",
                payload);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void SerializePayload_DoesNotIncludeArgumentsWhenStatedSo_WithOldFormat_InVersion_Pre_170()
        {
            var invocationData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "Sample",
                "[\"System.String\"]",
                "[\"\\\"Hello\\\"\"]");

            var payload = invocationData.SerializePayload(excludeArguments: true);

            Assert.Equal(
                "{\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"Method\":\"Sample\",\"ParameterTypes\":\"[\\\"System.String\\\"]\",\"Arguments\":null}",
                payload);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void SerializePayload_SerializesInvocationDataToString_WithoutNullifyingEmptyEntries_InVersion_Pre_170()
        {
            var invocationData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "Empty",
                "[]",
                "[]");

            var payload = invocationData.SerializePayload();

            Assert.Equal(
                "{\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"Method\":\"Empty\",\"ParameterTypes\":\"[]\",\"Arguments\":\"[]\"}",
                payload);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializePayload_CorrectlySerializesInvocationDataToString_WithNewFormat_InVersion_170()
        {
            var invocationData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "Sample",
                "[\"System.String\"]",
                "[\"\\\"Hello\\\"\"]");

            var payload = invocationData.SerializePayload();

            Assert.Equal(
                "{\"t\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"m\":\"Sample\",\"p\":[\"System.String\"],\"a\":[\"\\\"Hello\\\"\"]}",
                payload);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializePayload_DoesNotIncludeArgumentsWhenStatedSo_WithNewFormat_InVersion_170()
        {
            var invocationData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "Sample",
                "[\"System.String\"]",
                "[\"\\\"Hello\\\"\"]");

            var payload = invocationData.SerializePayload(excludeArguments: true);

            Assert.Equal(
                "{\"t\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"m\":\"Sample\",\"p\":[\"System.String\"]}",
                payload);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializePayload_SerializesInvocationDataToString_WithNullifyingEmptyEntries_InVersion_170()
        {
            var invocationData = new InvocationData(
                "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests",
                "Empty",
                "[]",
                "[]");

            var payload = invocationData.SerializePayload();

            Assert.Equal(
                "{\"t\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"m\":\"Empty\"}",
                payload);
        }

        [DataCompatibilityRangeTheory]

        // Previous serialization format.
        [InlineData("{\"$type\":\"Hangfire.Storage.InvocationData, Hangfire.Core\",\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"Method\":\"Sample\",\"ParameterTypes\":\"{\\\"$type\\\":\\\"System.Type[], mscorlib\\\",\\\"$values\\\":[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]}\",\"Arguments\":\"{\\\"$type\\\":\\\"System.String[], mscorlib\\\",\\\"$values\\\":[\\\"\\\\\\\"Hello\\\\\\\"\\\"]}\"}")]
        [InlineData("{\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"Method\":\"Sample\",\"ParameterTypes\":\"{\\\"$type\\\":\\\"System.Type[], mscorlib\\\",\\\"$values\\\":[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]}\",\"Arguments\":\"{\\\"$type\\\":\\\"System.String[], mscorlib\\\",\\\"$values\\\":[\\\"\\\\\\\"Hello\\\\\\\"\\\"]}\"}")]
        [InlineData("{\"$type\":\"Hangfire.Storage.InvocationData, Hangfire.Core\",\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"Method\":\"Sample\",\"ParameterTypes\":\"[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]\",\"Arguments\":\"[\\\"\\\\\\\"Hello\\\\\\\"\\\"]\"}")]
        [InlineData("{\"Type\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"Method\":\"Sample\",\"ParameterTypes\":\"[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]\",\"Arguments\":\"[\\\"\\\\\\\"Hello\\\\\\\"\\\"]\"}")]

        // New serialization format.
        [InlineData("{\"t\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"m\":\"Sample\",\"p\":[\"System.String\"],\"a\":[\"\\\"Hello\\\"\"]}")]
        public void Deserialize_DeserializesCorrectlyStringToInvocationData(string invocationData)
        {
            try
            {
                JobHelper.SetSerializerSettings(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                var serializedData = InvocationData.DeserializePayload(invocationData);

                var job = serializedData.Deserialize();

                Assert.False(job.Type.GetTypeInfo().ContainsGenericParameters);
                Assert.Equal("Sample", job.Method.Name);
                Assert.Equal(typeof(string), job.Method.GetParameters()[0].ParameterType);
                Assert.Equal(1, job.Args.Count);
                Assert.Equal("Hello", job.Args[0]);
            }
            finally
            {
                JobHelper.SetSerializerSettings(null);
            }
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_DeserializesCorrectlyShortFormatStringToInvocationData()
        {
            var invocationData = "{\"t\":\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\",\"m\":\"Empty\"}";

            var serializedData = InvocationData.DeserializePayload(invocationData);

            var job = serializedData.Deserialize();

            Assert.False(job.Type.GetTypeInfo().ContainsGenericParameters);
            Assert.Equal("Empty", job.Method.Name);
            Assert.Equal(0, job.Method.GetParameters().Length);
            Assert.Equal(0, job.Args.Count);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_HandlesGenericTypes()
        {
            var serializedData = InvocationData.Serialize(
                Job.FromExpression<GenericType<string>>(x => x.Method()));

            var job = serializedData.Deserialize();

            Assert.False(job.Type.GetTypeInfo().ContainsGenericParameters);
            Assert.Equal(typeof(string), job.Type.GetGenericArguments()[0]);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_HandlesGenericMethods_WithOpenTypeParameters()
        {
            var serializedData = InvocationData.Serialize(
                Job.FromExpression<GenericType<string>>(x => x.Method("asd", 123)));

            var job = serializedData.Deserialize();

            Assert.False(job.Method.ContainsGenericParameters);
        }

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeTheory]
        [MemberData(nameof(MemberData))]
        public void Serialize_CorrectlySerializesJobToInvocationData(Job job, string typeName, string method, string parameterTypes, string serializedArgs)
        {
            try
            {
                InvocationData.SetTypeSerializer(TypeHelper.SimpleAssemblyTypeSerializer);

                var invocationData = InvocationData.Serialize(job);

                Assert.Equal(typeName, invocationData.Type);
                Assert.Equal(method, invocationData.Method);
                Assert.Equal(parameterTypes, invocationData.ParameterTypes);
                Assert.Equal(serializedArgs, invocationData.Arguments);
            }
            finally
            {
                InvocationData.SetTypeSerializer(null);
            }
        }

        [DataCompatibilityRangeTheory]
        [MemberData(nameof(MemberData))]
        public void Deserialize_CorrectlyDeserializesJobFromInvocationData(Job job, string typeName, string method, string parameterTypes, string serializedArgs)
        {
            var deserializedJob = new InvocationData(typeName, method, parameterTypes, serializedArgs).Deserialize();

#if NETCOREAPP1_0 || NETCOREAPP2_1
            Assert.Equal(job.Type.FullName, deserializedJob.Type.FullName);
            Assert.Equal(job.Method.Name, deserializedJob.Method.Name);
#else
            Assert.Equal(job.Type, deserializedJob.Type);
            Assert.Equal(job.Method, deserializedJob.Method);
#endif

            var parameters = job.Method.GetParameters();
            var deserializedParameters = deserializedJob.Method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                Assert.Equal(parameters[i].ParameterType, deserializedParameters[i].ParameterType);
            }

            for (var i = 0; i < job.Args.Count; i++)
            {
                Assert.Equal(job.Args[i], deserializedJob.Args[i]);
            }
        }

        public static IEnumerable<object[]> MemberData
        {
            get
            {
                return new []
                {
                    new object[] { Job.FromExpression(() => Thread.Sleep(TimeSpan.FromSeconds(5))), "System.Threading.Thread, mscorlib", "Sleep", "[\"System.TimeSpan, mscorlib\"]", "[\"\\\"00:00:05\\\"\"]" },
                    new object[] { Job.FromExpression(() => Console.WriteLine("4567")), "System.Console, mscorlib", "WriteLine", "[\"System.String\"]", "[\"\\\"4567\\\"\"]" },
                    new object[] { Job.FromExpression(() => Sample("str1")), "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests", "Sample", "[\"System.String\"]", "[\"\\\"str1\\\"\"]" },
                    new object[] { Job.FromExpression(() => ListMethod(new string[0])), "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests", "ListMethod", "[\"System.Collections.Generic.IList`1[[System.String]], mscorlib\"]", "[\"[]\"]" },

                    new object[] { Job.FromExpression(() => GenericMethod(1)), "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests", "GenericMethod", "[\"System.Int32\"]", "[\"1\"]" },
#if !NETCOREAPP1_0
                    new object[] { Job.FromExpression(() => GenericMethod((StringDictionary)null)), "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests", "GenericMethod", "[\"System.Collections.Specialized.StringDictionary, System\"]", "[null]" },
#endif
                    new object[] { Job.FromExpression(() => GenericMethod((InvocationDataFacts)null)), "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests", "GenericMethod", "[\"Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests\"]", "[null]" },
                    new object[] { Job.FromExpression(() => GenericMethod((GlobalType)null)), "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests", "GenericMethod", "[\"GlobalType, Hangfire.Core.Tests\"]", "[null]" },
                    new object[] { Job.FromExpression(() => OtherGenericMethod(1, new List<int>())), "Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests", "OtherGenericMethod", "[\"System.Int32\",\"System.Collections.Generic.List`1[[System.Int32]], mscorlib\"]", "[\"1\",\"[]\"]" },

                    new object[] { Job.FromExpression<NestedType>(x => x.Method()), "Hangfire.Core.Tests.Storage.InvocationDataFacts+NestedType, Hangfire.Core.Tests", "Method", "[]", "[]" },
                    new object[] { Job.FromExpression<NestedType>(x => x.NestedGenericMethod(1)), "Hangfire.Core.Tests.Storage.InvocationDataFacts+NestedType, Hangfire.Core.Tests", "NestedGenericMethod", "[\"System.Int32\"]", "[\"1\"]" },

                    new object[] { Job.FromExpression<GenericType<int>>(x => x.Method()), "Hangfire.Core.Tests.Storage.InvocationDataFacts+GenericType`1[[System.Int32]], Hangfire.Core.Tests", "Method", "[]", "[]" },
                    new object[] { Job.FromExpression<GenericType<GlobalType>>(x => x.Method()), "Hangfire.Core.Tests.Storage.InvocationDataFacts+GenericType`1[[GlobalType, Hangfire.Core.Tests]], Hangfire.Core.Tests", "Method", "[]", "[]" },
                    new object[] { Job.FromExpression<GenericType<InvocationDataFacts>>(x => x.Method()), "Hangfire.Core.Tests.Storage.InvocationDataFacts+GenericType`1[[Hangfire.Core.Tests.Storage.InvocationDataFacts, Hangfire.Core.Tests]], Hangfire.Core.Tests", "Method", "[]", "[]" },
                    new object[] { Job.FromExpression<GenericType<int>>(x => x.Method(1, 1)), "Hangfire.Core.Tests.Storage.InvocationDataFacts+GenericType`1[[System.Int32]], Hangfire.Core.Tests", "Method", "[\"System.Int32\",\"System.Int32\"]", "[\"1\",\"1\"]" },
                    new object[] { Job.FromExpression<GenericType<int>.NestedGenericType<string>>(x => x.Method(1, "1")), "Hangfire.Core.Tests.Storage.InvocationDataFacts+GenericType`1+NestedGenericType`1[[System.Int32],[System.String]], Hangfire.Core.Tests", "Method", "[\"System.Int32\",\"System.String\"]", "[\"1\",\"\\\"1\\\"\"]" },

                    new object[] { Job.FromExpression<GlobalType>(x => x.Method()), "GlobalType, Hangfire.Core.Tests", "Method", "[]", "[]" },
                    new object[] { Job.FromExpression<GlobalType>(x => x.GenericMethod(1)), "GlobalType, Hangfire.Core.Tests", "GenericMethod", "[\"System.Int32\"]", "[\"1\"]" },
                    new object[] { Job.FromExpression<GlobalType.NestedType>(x => x.NestedMethod()), "GlobalType+NestedType, Hangfire.Core.Tests", "NestedMethod", "[]", "[]" },
                    new object[] { Job.FromExpression<GlobalType.NestedGenericType<long>>(x => x.NestedGenericMethod(1, 1)), "GlobalType+NestedGenericType`1[[System.Int64]], Hangfire.Core.Tests", "NestedGenericMethod", "[\"System.Int64\",\"System.Int32\"]", "[\"1\",\"1\"]" },

                    new object[] { Job.FromExpression<GlobalGenericType<int>>(x => x.Method()), "GlobalGenericType`1[[System.Int32]], Hangfire.Core.Tests", "Method", "[]", "[]" },
                    new object[] { Job.FromExpression<GlobalGenericType<object>>(x => x.GenericMethod(1)), "GlobalGenericType`1[[System.Object]], Hangfire.Core.Tests", "GenericMethod", "[\"System.Int32\"]", "[\"1\"]" },
                    new object[] { Job.FromExpression<GlobalGenericType<int>.NestedType>(x => x.Method()), "GlobalGenericType`1+NestedType[[System.Int32]], Hangfire.Core.Tests", "Method", "[]", "[]" },
                    new object[] { Job.FromExpression<GlobalGenericType<long>.NestedGenericType<int>>(x => x.Method(1, 1)), "GlobalGenericType`1+NestedGenericType`1[[System.Int64],[System.Int32]], Hangfire.Core.Tests", "Method", "[\"System.Int64\",\"System.Int32\"]", "[\"1\",\"1\"]" },
                };
            }
        }

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
        public void Deserialize_CorrectlySeserializes_NullableUtcDateTimeArguments_With_Null()
        {
            DateTime? value = null;

            var serializedData = InvocationData.Serialize(Job.FromExpression(() => NullableDateTimeMethod(value)));

            var job = serializedData.Deserialize();

            Assert.Equal(value, job.Args[0]);
        }

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
        public void Deserialize_CorrectlyDeserializes_NullableDateTimeArguments_With_Null_Value()
        {
            DateTime? value = null;
            var serializedData = new InvocationData(
                typeof(InvocationDataFacts).AssemblyQualifiedName,
                nameof(NullableDateTimeMethod),
                JobHelper.ToJson(new[] { typeof(DateTime?) }),
                JobHelper.ToJson(new[] { value }));

            var job = serializedData.Deserialize();

            Assert.Equal(value, job.Args[0]);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_CorrectlyDeserializes_DateTimesInRegularJsonFormat()
        {
            var dateTimeString = "\"2019-03-05T13:20:04.5932150Z\"";
            var dateTime = SerializationHelper.Deserialize<DateTime>(dateTimeString, SerializationOption.User);

            var dateTimeJob = new InvocationData(
                GetType().AssemblyQualifiedName,
                "DateTimeMethod",
                JobHelper.ToJson(new [] { typeof(DateTime) }),
                "[\"\\\"2019-03-05T13:20:04.593215Z\\\"\"]").DeserializeJob();

            var nullableJob = new InvocationData(
                GetType().AssemblyQualifiedName,
                "NullableDateTimeMethod",
                JobHelper.ToJson(new[] { typeof(DateTime?) }),
                "[\"\\\"2019-03-05T13:20:04.593215Z\\\"\"]").DeserializeJob();

            Assert.Equal(dateTime, dateTimeJob.Args[0]);
            Assert.Equal(dateTime, nullableJob.Args[0]);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
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

            Assert.Equal(typeof(List<string>), job.Args[0]?.GetType());
            Assert.Equal("one", (job.Args[0] as List<string>)?[0]);
            Assert.Equal("two", (job.Args[0] as List<string>)?[1]);

            Assert.Equal(typeof(SomeClass), job.Args[1]?.GetType());
            Assert.Equal("value", (job.Args[1] as SomeClass)?.StringValue);
            Assert.Equal(0, (job.Args[1] as SomeClass)?.DefaultValue);
            Assert.Equal(null, (job.Args[1] as SomeClass)?.NullObject);
        }

#if !NET452 && !NET461
        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void DeserializeJob_CanPreviousFormat_WhenTypeNameHandlingOptionIsSetToAll()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            JsonConvert.DefaultSettings = () => settings;
#pragma warning disable 618
            JobHelper.SetSerializerSettings(settings);
#pragma warning restore 618

            var job = InvocationData
                .DeserializePayload("{\"$type\":\"Hangfire.Storage.InvocationData, Hangfire.Core\",\"Type\":\"System.Console, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\",\"Method\":\"WriteLine\",\"ParameterTypes\":\"{\\\"$type\\\":\\\"System.Type[], mscorlib\\\",\\\"$values\\\":[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]}\",\"Arguments\":\"{\\\"$type\\\":\\\"System.String[], mscorlib\\\",\\\"$values\\\":[\\\"\\\\\\\"Hello \\\\\\\"\\\"]}\"}")
                .DeserializeJob();

            Assert.Equal("System.Console", job.Type.FullName);
            Assert.Equal("WriteLine", job.Method.Name);
            Assert.Equal("Hello ", job.Args[0]);
        }
#endif

        [Fact]
        public void DeserializePayload_ThrowsAnException_WhenPayloadIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => InvocationData.DeserializePayload(null));

            Assert.Equal("payload", exception.ParamName);
        }

        // https://github.com/HangfireIO/Hangfire/issues/1470
        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void DeserializePayload_CanHandleFieldBasedSerialization_OfInvocationDataClass()
        {
#pragma warning disable 618
            JobHelper.SetSerializerSettings(new JsonSerializerSettings { ContractResolver = new FieldsOnlyContractResolver() });
#pragma warning restore 618
            var payload = "{\"<Type>k__BackingField\":\"System.Console, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\",\"<Method>k__BackingField\":\"WriteLine\",\"<ParameterTypes>k__BackingField\":\"[]\",\"<Arguments>k__BackingField\":\"[]\"}";

            var data = InvocationData.DeserializePayload(payload);

            Assert.StartsWith("System.Console", data.Type);
            Assert.Equal("WriteLine", data.Method);
            Assert.Equal("[]", data.ParameterTypes);
            Assert.Equal("[]", data.Arguments);
        }

        private class FieldsOnlyContractResolver: DefaultContractResolver 
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
                => objectType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Cast<MemberInfo>()
                    .ToList();

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) 
                => base.CreateProperties(type, MemberSerialization.Fields);
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

        public static void ComplicatedMethod(IList<string> arg, SomeClass objArg)
        {
        }

        public class SomeClass
        {
            public string StringValue { get; set; }
            public object NullObject { get; set; }
            public int DefaultValue { get; set; }
        }

        public class NestedType
        {
            public void Method() { }
            public void NestedGenericMethod<T>(T arg1) { }
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