using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Hangfire.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class SerializationHelperFacts
    {
        [Fact]
        public void Serialize_ReturnsNull_WnehValueIsNull()
        {
            Assert.Null(SerializationHelper.Serialize(null));
        }

        [Fact]
        public void Serialize_ReturnsCorrectResult_WhenValueIsString()
        {
            var result = SerializationHelper.Serialize("Simple string");
            Assert.Equal("\"Simple string\"", result);
        }

        [Fact]
        public void Serialize_ReturnsCorrectValue_WhenValueIsCustomObject()
        {
            var result = SerializationHelper.Serialize(new ClassA("B"));
            Assert.Equal(@"{""PropertyA"":""B""}", result);
        }

        [Fact]
        public void Serialize_ReturnsCorrectJson_WhenOptionsIsDefaultWithTypes()
        {
            var result = SerializationHelper.Serialize(new ClassA("B"), SerializationOption.DefaultWithTypes);
            Assert.Equal(@"{""$type"":""Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""B""}", result);
        }

        [Fact, CleanJsonSerializersSettings]
        public void Serialize_SerializesWithUserSettings_WhenOptionsIsUser()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var result = SerializationHelper.Serialize(new ClassA("A"), SerializationOption.User);
            Assert.Equal(@"{""propertyA"":""A""}", result);
        }

        [Fact, CleanJsonSerializersSettings]
        public void Serialize_HandleJsonDefaultSettingsDoesNotAffect_WhenOptionIsDefaultSettings()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "ddMMyyyy"
            };

            var result = SerializationHelper.Serialize(new ClassB { StringValue = "B", DateTimeValue = new DateTime(1961, 4, 12) });
            Assert.Equal(@"{""StringValue"":""B"",""DateTimeValue"":""1961-04-12T00:00:00""}", result);
        }

        [Fact, CleanJsonSerializersSettings]
        public void Serialize_HandleJsonDefaultSettingsDoesNotAffect_WhenOptionIsDefaultSettingsWithTypes()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "ddMMyyyy"
            };

            var result = SerializationHelper.Serialize(
                new ClassB { StringValue = "B", DateTimeValue = new DateTime(1961, 4, 12) },
                SerializationOption.DefaultWithTypes);

            Assert.Equal(@"{""$type"":""Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassB, Hangfire.Core.Tests"",""StringValue"":""B"",""DateTimeValue"":""1961-04-12T00:00:00""}", result);
        }

        [Fact]
        public void Deserialize_ReturnsNull_WhenValueIsNull()
        {
            var result = SerializationHelper.Deserialize(null, typeof(string));
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_ThrowsException_WhenTypeIsNull()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            var exception = Assert.Throws<ArgumentNullException>(() => SerializationHelper.Deserialize("someString", null));
            Assert.Equal("type", exception.ParamName);
        }

        [Fact]
        public void Deserialize_ReturnsCorrectValue_WhenValueIsString()
        {
            var result = SerializationHelper.Deserialize("\"hello\"", typeof(string));
            Assert.Equal("hello", result);
        }

        [Fact]
        public void Deserialize_RetrunsCorrectObject_WhenTypeIsCustomClass()
        {
            var valueJson = @"{""PropertyA"":""A""}";

            var value = SerializationHelper.Deserialize(valueJson, typeof(ClassA)) as ClassA;

            Assert.NotNull(value);
            Assert.Equal("A", value.PropertyA);
        }

        [Fact]
        public void Deserialize_RetrunsCorrectObject_WhenOptionsIsDefatultWithTypes()
        {
            var valueJson = @"{""$type"":""Hangfire.Core.Tests.Common.SerializationHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""A""}";

            var value = SerializationHelper.Deserialize(valueJson, typeof(ClassA), SerializationOption.DefaultWithTypes);
            var customObj = value as ClassA;

            Assert.NotNull(customObj);
            Assert.Equal("A", customObj.PropertyA);
        }

        [Fact, CleanJsonSerializersSettings]
        //This test is here to check backward compatability. Earlier user settings is used for serialization internal data.
        public void Deserialize_HandlesUsingUserOption_WhenUsingDefaultWithTypesOptionThrewException()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Binder = new CustomSerializerBinder()
            });

            var valueJson = SerializationHelper.Serialize(new ClassA("A"), SerializationOption.User);

            var value = SerializationHelper.Deserialize(valueJson, typeof(ClassA), SerializationOption.DefaultWithTypes);

            var classAObj = value as ClassA;

            Assert.NotNull(classAObj);
            Assert.Equal("A", classAObj.PropertyA);
        }

        [Fact]
        public void DeserializeGeneric_ReturnsNull_WhenValueIsNull()
        {
            var result = SerializationHelper.Deserialize<object>(null);
            Assert.Null(result);
        }

        [Fact]
        public void DeserializeGeneric_ReturnsDefaultValue_WhenGenericArgumentIsValueType()
        {
            var result = SerializationHelper.Deserialize<int>(null);
            Assert.Equal(0, result);
        }

        [Fact]
        public void DeserializeGeneric_ReturnsCorrectValue_WhenValueIsString()
        {
            var result = SerializationHelper.Deserialize<string>("\"hello\"");
            Assert.Equal("hello", result);
        }

        [Fact]
        public void DeserializeGeneric_RetrunsCorrectObject_WhenTypeIsCustomClass()
        {
            var valueJson = @"{""PropertyA"":""A""}";

            var value = SerializationHelper.Deserialize<ClassA>(valueJson);

            Assert.NotNull(value);
            Assert.Equal("A", value.PropertyA);
        }

        [Fact]
        public void DeserializeGeneric_RetrunsCorrectObject_WhenOptionsIsDefatultWithTypes()
        {
            var valueJson = @"{""PropertyA"":""A""}";

            var value = SerializationHelper.Deserialize<ClassA>(valueJson, SerializationOption.DefaultWithTypes);
            Assert.NotNull(value);
            Assert.Equal("A", value.PropertyA);
        }

        [Fact, CleanJsonSerializersSettings]
        //This test is here to check backward compatability. Earlier user settings is used for serialization internal data.
        public void DeserializeGeneric_HandlesUsingUserOption_WhenUsingDefaultWithTypesOptionThrewException()
        {
            SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Binder = new CustomSerializerBinder()
            });

            var valueJson = SerializationHelper.Serialize(new ClassA("A"), SerializationOption.User);

            var value = SerializationHelper.Deserialize<ClassA>(valueJson, SerializationOption.DefaultWithTypes);

            Assert.NotNull(value);
            Assert.Equal("A", value.PropertyA);
        }

        [Fact, CleanJsonSerializersSettings]
        public void DeserializeGeneric_RethrowsJsonException_WhenValueHasIncorrectFormat()
        {
            var valueJson = "asdfaljsadkfh";

            Assert.Throws<JsonReaderException>(() => SerializationHelper.Deserialize<ClassA>(valueJson));
        }

        [Fact]
        public void ApplyDefaultSerializerSettings_SetsDefaultSettings()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Arrays,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                DateFormatString = "",
                Formatting = Formatting.Indented,
                CheckAdditionalContent = true,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                Culture = CultureInfo.GetCultureInfo("de"),
                Binder = new CustomSerializerBinder(),
            };

            SerializationHelper.ApplyDefaultSerializerSettings(serializerSettings);

            Assert.Equal(TypeNameHandling.None, serializerSettings.TypeNameHandling);
            Assert.Equal(FormatterAssemblyStyle.Simple, serializerSettings.TypeNameAssemblyFormat);
            Assert.Equal(NullValueHandling.Ignore, serializerSettings.NullValueHandling);
            Assert.Equal(DefaultValueHandling.Ignore, serializerSettings.DefaultValueHandling);
            Assert.Equal(DateFormatHandling.IsoDateFormat, serializerSettings.DateFormatHandling);
            Assert.Equal(DateTimeZoneHandling.RoundtripKind, serializerSettings.DateTimeZoneHandling);
            Assert.Equal(DateParseHandling.DateTime, serializerSettings.DateParseHandling);
            Assert.Equal(FloatFormatHandling.DefaultValue, serializerSettings.FloatFormatHandling);
            Assert.Equal(FloatParseHandling.Double, serializerSettings.FloatParseHandling);
            Assert.Equal(StringEscapeHandling.Default, serializerSettings.StringEscapeHandling);
            Assert.Equal(@"yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", serializerSettings.DateFormatString);
            Assert.Equal(Formatting.None, serializerSettings.Formatting);
            Assert.Equal(false, serializerSettings.CheckAdditionalContent);
            Assert.Equal(ConstructorHandling.Default, serializerSettings.ConstructorHandling);
            Assert.Equal(ReferenceLoopHandling.Error, serializerSettings.ReferenceLoopHandling);
            Assert.Equal(MissingMemberHandling.Ignore, serializerSettings.MissingMemberHandling);
            Assert.Equal(ObjectCreationHandling.Auto, serializerSettings.ObjectCreationHandling);
            Assert.Equal(PreserveReferencesHandling.None, serializerSettings.PreserveReferencesHandling);
            Assert.Equal(CultureInfo.InvariantCulture, serializerSettings.Culture);
            Assert.IsType<DefaultSerializationBinder>(serializerSettings.Binder);
            Assert.IsType<StreamingContext>(serializerSettings.Context);
        }

        [Fact]
        public void ApplyDefaultSerializerSettings_SetsDefaultSettings_WhenTypeNameHandlingIsSet()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None
            };

            SerializationHelper.ApplyDefaultSerializerSettings(serializerSettings, TypeNameHandling.Objects);

            Assert.Equal(TypeNameHandling.Objects, serializerSettings.TypeNameHandling);
        }

        [Fact]
        public void ApplyDefaultSerializerSettings_ThrowsException_WhenSerializerSettingsIsNull()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            var exception = Assert.Throws<ArgumentNullException>(() => SerializationHelper.ApplyDefaultSerializerSettings(null));

            Assert.Equal("serializerSettings", exception.ParamName);
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

        private class CustomSerializerBinder : SerializationBinder
        {
            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = "someAssembly";
                typeName = serializedType.FullName.ToUpper();
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(ClassA);
            }
        }
    }
}