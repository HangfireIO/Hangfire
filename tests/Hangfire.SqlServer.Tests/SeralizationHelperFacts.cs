using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class SeralizationHelperFacts
    {
        [Fact]
        public void Serialize_ReturnsNull_WhenValueIsNull()
        {
            var result = SerializationHelper.Serialize(null);
            Assert.Null(result);
        }

        [Fact]
        public void Serialize_ReturnsCorrectResult_WhenValueIsSimpleString()
        {
            var result = SerializationHelper.Serialize("Hello, world!");
            Assert.Equal("\"Hello, world!\"", result);
        }

        [Fact]
        public void Serialize_ReturnsCorrectResult_WhenValueIsObjectOfCustomClass()
        {
            var result = SerializationHelper.Serialize(new MyClass { StringValue = "simple" });
            Assert.Equal(@"{""StringValue"":""simple"",""IntValue"":0}", result);
        }

        [Fact]
        public void Deserialize_ReturnsNull_WhenValueIsNull()
        {
            var result = SerializationHelper.Deserialize<string>(null);
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_ReturnsDefaultValue_WhenValueIsNullAndGenericArgumentIsValueType()
        {
            var result = SerializationHelper.Deserialize<int>(null);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Deserialize_ReturnsCorrectValue_WhenValueIsString()
        {
            var result = SerializationHelper.Deserialize<string>("\"hello\"");
            Assert.Equal("hello", result);
        }

        [Fact]
        public void Deserialize_RetrunsCorrectObject_WhenTypeIsCustomClass()
        {
            var valueJson = @"{""StringValue"":""simple"",""IntValue"":0}";

            var value = SerializationHelper.Deserialize<MyClass>(valueJson);

            Assert.NotNull(value);
            Assert.Equal("simple", value.StringValue);
            Assert.Equal(0, value.IntValue);
        }

        [Fact, CleanJsonSerializersSettings]
        public void Deserialize_HandlesDefaultSettingsDoNotAffect()
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

            var result = SerializationHelper.Serialize(new CustomClass { DateTimeValue = new DateTime(1961, 4, 12) });
            Assert.Equal(@"{""DateTimeValue"":""1961-04-12T00:00:00"",""NullValue"":null,""DefaultValue"":0}", result);
        }

        public class MyClass
        {
            public string StringValue { get; set; }

            public int IntValue { get; set; }
        }

        public class CustomClass
        {
            public DateTime DateTimeValue { get; set; }

            public object NullValue { get; set; }

            public int DefaultValue { get; set; }

        }

        private class CustomSerializerBinder : SerializationBinder
        {
            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = "someAssembly";
                typeName = "someType";
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(MyClass);
            }
        }
    }
}