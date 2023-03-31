using System;
using System.Runtime.Serialization;
using Hangfire.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class SerializationHelperFacts
    {
        [DataCompatibilityRangeFact]
        public void Serialize_ReturnsNull_WhenValueIsNull()
        {
            Assert.Null(SerializationHelper.Serialize((string)null));
        }

        [DataCompatibilityRangeFact]
        public void Serialize_ReturnsCorrectResult_WhenValueIsString()
        {
            var result = SerializationHelper.Serialize("Simple string");
            Assert.Equal("\"Simple string\"", result);
        }

        [DataCompatibilityRangeFact]
        public void Serialize_ReturnsCorrectValue_WhenValueIsCustomObject()
        {
            var result = SerializationHelper.Serialize(new ClassA("B"));
            Assert.Equal(@"{""PropertyA"":""B""}", result);
        }

        [DataCompatibilityRangeFact]
        public void Serialize_ReturnsCorrectJson_WhenOptionsIsTypedInternal()
        {
            var result = SerializationHelper.Serialize<object>(new ClassA("B"), SerializationOption.TypedInternal);
            Assert.Equal(@"{""$type"":""Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""B""}", result);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void Serialize_SerializesWithUserSettings_WhenOptionsIsUser()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var result = SerializationHelper.Serialize(new ClassA("A"), SerializationOption.User);
            Assert.Equal(@"{""propertyA"":""A""}", result);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170), CleanSerializerSettings]
        public void Serialize_SerializesWithUserSettings_WhenOptionsIsInternal_BeforeVersion170()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            });

            var result = SerializationHelper.Serialize(new ClassA("A"));
            Assert.Equal(@"{""propertyA"":""A""}", result);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170), CleanSerializerSettings]
        public void Serialize_SerializesWithDefaultSettingsWithTypeInformation_WhenOptionIsTypedInternal_BeforeVersion170()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var result = SerializationHelper.Serialize(new ClassB { StringValue = "hi"}, SerializationOption.TypedInternal);
            Assert.Equal(
                "{\"$type\":\"Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassB, Hangfire.Core.Tests\"," + 
                "\"StringValue\":\"hi\",\"NullValue\":null,\"DefaultValue\":0,\"DateTimeValue\":null}", 
                result);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170), CleanSerializerSettings]
        public void Serialize_DoesNotSerializeWithUserSettings_WhenOptionsIsInternal_AfterVersion170()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var result = SerializationHelper.Serialize(new ClassA("A"));
            Assert.Equal(@"{""PropertyA"":""A""}", result);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void Serialize_DoesNotSerializeWithUserSettings_WhenOptionsIsTypedInternal()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var result = SerializationHelper.Serialize<object>(new ClassA("A"), SerializationOption.TypedInternal);
            Assert.Equal(
                @"{""$type"":""Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""A""}",
                result);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170), CleanSerializerSettings]
        public void Serialize_ProducesObjectThatCanBeDeserialized_UsingJsonConvert_WithInternalSettings_BeforeVersion170()
        {
            // Arrange
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.All
            });

            var initialObject = new ClassA("A");

            // Act
            var result = SerializationHelper.Serialize(initialObject);

            var resultingObject = JsonConvert.DeserializeObject<ClassA>(result, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None
            });

            // Assert
            Assert.Equal(initialObject.PropertyA, resultingObject.PropertyA);
        }

#if !NET452 && !NET461
        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void Serialize_JsonDefaultSettingsAffectResult_WhenOptionIs_User()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "ddMMyyyy"
            };

            var result = SerializationHelper.Serialize(new ClassB { StringValue = "B", DateTimeValue = new DateTime(1961, 4, 12) }, SerializationOption.User);
            Assert.Equal(
                "{\"$type\":\"HANGFIRE.CORE.TESTS.COMMON.SERIALIZATIONHELPERFACTS+CLASSB, someAssembly\",\"StringValue\":\"B\",\"DateTimeValue\":\"12041961\"}",
                result);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170), CleanSerializerSettings]
        public void Serialize_JsonDefaultSettingsDoAffectResult_WhenOptionIs_Internal_BeforeVersion_170()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "ddMMyyyy"
            };

            var result = SerializationHelper.Serialize(new ClassB { StringValue = "B", DateTimeValue = new DateTime(1961, 4, 12) });
            Assert.Equal(
                "{\"$type\":\"HANGFIRE.CORE.TESTS.COMMON.SERIALIZATIONHELPERFACTS+CLASSB, someAssembly\",\"StringValue\":\"B\",\"DateTimeValue\":\"12041961\"}",
                result);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170), CleanSerializerSettings]
        public void Serialize_JsonDefaultSettingsDoNotAffectResult_WhenOptionIs_Internal_StartingFromVersion_170()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "ddMMyyyy"
            };

            var result = SerializationHelper.Serialize(new ClassB { StringValue = "B", DateTimeValue = new DateTime(1961, 4, 12) });
            Assert.Equal(@"{""StringValue"":""B"",""DateTimeValue"":""1961-04-12T00:00:00""}", result);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170), CleanSerializerSettings]
        public void Serialize_JsonDefaultSettingsDoAffectResult_WhenOptionIs_TypedInternal_BeforeVersion_170()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "ddMMyyyy"
            };

            var result = SerializationHelper.Serialize<object>(
                new ClassB { StringValue = "B", DateTimeValue = new DateTime(1961, 4, 12) },
                SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"HANGFIRE.CORE.TESTS.COMMON.SERIALIZATIONHELPERFACTS+CLASSB, someAssembly\",\"StringValue\":\"B\",\"DateTimeValue\":\"12041961\"}",
                result);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170), CleanSerializerSettings]
        public void Serialize_JsonDefaultSettingsDoNotAffectResult_WhenOptionIs_TypedInternal_StartingFromVersion_170()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "ddMMyyyy"
            };

            var result = SerializationHelper.Serialize<object>(
                new ClassB { StringValue = "B", DateTimeValue = new DateTime(1961, 4, 12) },
                SerializationOption.TypedInternal);

            Assert.Equal(@"{""$type"":""Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassB, Hangfire.Core.Tests"",""StringValue"":""B"",""DateTimeValue"":""1961-04-12T00:00:00""}", result);
        }
#endif

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void Serialize_WithSpecifiedType_DoesNotIncludeTypeProperty_WhenItEqualsToDeclared_AndTypeNameHandlingAutoIsUsed()
        {
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            var result = SerializationHelper.Serialize(
                new ClassA("hello"),
                typeof(ClassA),
                SerializationOption.User);

            Assert.Equal("{\"PropertyA\":\"hello\"}", result);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void Serialize_WithSpecifiedType_IncludesTypeProperty_WhenItDoesNotEqualToDeclared_AndTypeNameHandlingAutoIsUsed()
        {
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            var result = SerializationHelper.Serialize(
                new ClassA("hello"),
                typeof(object),
                SerializationOption.User);

            Assert.Equal("{\"$type\":\"Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassA, Hangfire.Core.Tests\",\"PropertyA\":\"hello\"}", result);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_ReturnsNull_WhenValueIsNull()
        {
            var result = SerializationHelper.Deserialize(null, typeof(string));
            Assert.Null(result);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_ThrowsException_WhenTypeIsNull()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            var exception = Assert.Throws<ArgumentNullException>(() => SerializationHelper.Deserialize("someString", null));
            Assert.Equal("type", exception.ParamName);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_ReturnsCorrectValue_WhenValueIsString()
        {
            var result = SerializationHelper.Deserialize("\"hello\"", typeof(string));
            Assert.Equal("hello", result);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_ReturnsCorrectObject_WhenTypeIsCustomClass()
        {
            var valueJson = @"{""PropertyA"":""A""}";

            var value = SerializationHelper.Deserialize(valueJson, typeof(ClassA)) as ClassA;

            Assert.NotNull(value);
            Assert.Equal("A", value.PropertyA);
        }

        [DataCompatibilityRangeFact]
        public void Deserialize_ReturnsCorrectObject_WhenOptionsIsTypedInternal()
        {
            var valueJson = @"{""$type"":""Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""A""}";

            var value = SerializationHelper.Deserialize(valueJson, typeof(ClassA), SerializationOption.TypedInternal);
            var customObj = value as ClassA;

            Assert.NotNull(customObj);
            Assert.Equal("A", customObj.PropertyA);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        //This test is here to check backward compatibility. Earlier user settings is used for serialization internal data.
        public void Deserialize_HandlesDeserializationUsingUserOption_WhenUsingInternalOptionThrewException()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            });

            var json = "{\"$type\":\"HANGFIRE.CORE.TESTS.COMMON.SERIALIZATIONHELPERFACTS+CLASSB, mscorlib\",\"StringValue\":\"B\",\"DateTimeValue\":\"\\/Date(1356044400000)\\/\"}";

            var value = SerializationHelper.Deserialize(json, typeof(ClassB));

            var obj = value as ClassB;

            Assert.NotNull(obj);
            Assert.Equal("B", obj.StringValue);
            Assert.Equal(new DateTime(2012, 12, 20, 23, 00, 00, DateTimeKind.Utc), obj.DateTimeValue);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        //This test is here to check backward compatibility. Earlier user settings is used for serialization internal data.
        public void Deserialize_RethrowsAnException_WhenUsingInternalOptionThrewException_AndUserSettingsAttemptFailedToo()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                DateFormatString = "ddMMyyyy"
            });

            var json = "{\"$type\":\"HANGFIRE.CORE.TESTS.COMMON.SERIALIZATIONHELPERFACTS+CLASSB, someAssembly\",\"StringValue\":\"B\",\"DateTimeValue\":\"12041961\"}";

            Assert.Throws<JsonSerializationException>(() => SerializationHelper.Deserialize(json, typeof(ClassB)));
        }

        [DataCompatibilityRangeFact]
        public void DeserializeGeneric_ReturnsNull_WhenValueIsNull()
        {
            var result = SerializationHelper.Deserialize<object>(null);
            Assert.Null(result);
        }

        [DataCompatibilityRangeFact]
        public void DeserializeGeneric_ReturnsDefaultValue_WhenGenericArgumentIsValueType()
        {
            var result = SerializationHelper.Deserialize<int>(null);
            Assert.Equal(0, result);
        }

        [DataCompatibilityRangeFact]
        public void DeserializeGeneric_ReturnsCorrectValue_WhenValueIsString()
        {
            var result = SerializationHelper.Deserialize<string>("\"hello\"");
            Assert.Equal("hello", result);
        }

        [DataCompatibilityRangeFact]
        public void DeserializeGeneric_ReturnsCorrectObject_WhenTypeIsCustomClass()
        {
            var valueJson = @"{""PropertyA"":""A""}";

            var value = SerializationHelper.Deserialize<ClassA>(valueJson);

            Assert.NotNull(value);
            Assert.Equal("A", value.PropertyA);
        }

        [DataCompatibilityRangeFact]
        public void DeserializeGeneric_ReturnsCorrectObject_WhenOptionsIsTypedInternal()
        {
            var valueJson = @"{""PropertyA"":""A""}";

            var value = SerializationHelper.Deserialize<ClassA>(valueJson, SerializationOption.TypedInternal);
            Assert.NotNull(value);
            Assert.Equal("A", value.PropertyA);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        //This test is here to check backward compatibility. Earlier user settings is used for serialization internal data.
        public void DeserializeGeneric_HandlesUsingUserOption_WhenUsingTypedInternalOptionThrewException()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
            });

            var valueJson = SerializationHelper.Serialize(new ClassA("A"), SerializationOption.User);

            var value = SerializationHelper.Deserialize<ClassA>(valueJson, SerializationOption.TypedInternal);

            Assert.NotNull(value);
            Assert.Equal("A", value.PropertyA);
        }

        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void DeserializeGeneric_RethrowsJsonException_WhenValueHasIncorrectFormat()
        {
            var valueJson = "asdfaljsadkfh";

            Assert.Throws<JsonReaderException>(() => SerializationHelper.Deserialize<ClassA>(valueJson));
        }

        [DataCompatibilityRangeFact]
        public void GetProtectedSettings_SetsDefaultSettings()
        {
            var serializerSettings = SerializationHelper.GetInternalSettings();

            Assert.Equal(TypeNameHandling.Auto, serializerSettings.TypeNameHandling);
#if !NET452 && !NET461 && !NETCOREAPP1_0
            Assert.Equal(TypeNameAssemblyFormatHandling.Simple, serializerSettings.TypeNameAssemblyFormatHandling);
#endif
            Assert.Equal(DefaultValueHandling.IgnoreAndPopulate, serializerSettings.DefaultValueHandling);
            Assert.Equal(NullValueHandling.Ignore, serializerSettings.NullValueHandling);
            Assert.True(serializerSettings.CheckAdditionalContent);
        }

        private interface IClass
        {
        }

        private class ClassA : IClass
        {
            public ClassA(string propertyA)
            {
                PropertyA = propertyA;
            }

            public string PropertyA { get; }
        }

        private class ClassB
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string StringValue { get; set; }

            // ReSharper disable once UnusedMember.Local
            public object NullValue { get; set; }

            // ReSharper disable once UnusedMember.Local
            public int DefaultValue { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public DateTime? DateTimeValue { get; set; }
        }

#pragma warning disable 618
        private class CustomSerializerBinder : SerializationBinder
#pragma warning restore 618
        {
            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = "someAssembly";
                typeName = serializedType.FullName.ToUpper();
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.Contains("ClassA")) return typeof(ClassA);
                return typeof(ClassB);
            }
        }
    }
}