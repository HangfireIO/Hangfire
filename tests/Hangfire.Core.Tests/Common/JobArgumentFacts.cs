using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;
using Moq;
using Newtonsoft.Json;
using Xunit;

#pragma warning disable 618

namespace Hangfire.Core.Tests.Common
{
	public class JobArgumentFacts
	{
		private readonly Mock<JobActivator> _activator;
		private readonly Mock<IJobCancellationToken> _token;

		public JobArgumentFacts()
		{
			_activator = new Mock<JobActivator>();
			_activator.Setup(x => x.ActivateJob(It.IsAny<Type>()))
				.Returns(() => new JobArgumentFacts());

			_token = new Mock<IJobCancellationToken>();
		}

		private const Boolean BooleanValue = true;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Boolean value) { Assert.Equal(BooleanValue, value); }

		[Fact]
		public void BooleanArguments_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(BooleanValue);
		}

		[DataCompatibilityRangeFact]
		public void BooleanArguments_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(BooleanValue);
		}

		private const Byte ByteValue = 142;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Byte value) { Assert.Equal(ByteValue, value); }

		[Fact]
		public void ByteValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(ByteValue);
		}

		[DataCompatibilityRangeFact]
		public void ByteValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(ByteValue);
		}

		private const SByte SByteValue = -111;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(SByte value) { Assert.Equal(SByteValue, value); }

		[Fact]
		public void SByteValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(SByteValue);
		}

		[DataCompatibilityRangeFact]
		public void SByteValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(SByteValue);
		}

		private const Char CharValue = Char.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Char value) { Assert.Equal(CharValue, value); }

		[Fact]
		public void CharValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(CharValue);
		}

		[DataCompatibilityRangeFact]
		public void CharValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(CharValue);
		}

		private const Decimal DecimalValue = Decimal.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Decimal value) { Assert.Equal(DecimalValue, value); }

		[Fact]
		public void DecimalValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(DecimalValue);
		}

		[DataCompatibilityRangeFact]
		public void DecimalValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(DecimalValue);
		}

		private const Double DoubleValue = 3.14159265359D;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Double value) { Assert.Equal(DoubleValue, value); }

		[Fact]
		public void DoubleValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(DoubleValue);
		}

		[DataCompatibilityRangeFact]
		public void DoubleValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(DoubleValue);
		}

		private const Single SingleValue = 3.1415F;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Single value) { Assert.Equal(SingleValue, value); }

		[Fact]
		public void SingleValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(SingleValue);
		}

		[DataCompatibilityRangeFact]
		public void SingleValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(SingleValue);
		}

		private const Int32 Int32Value = Int32.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Int32 value) { Assert.Equal(Int32Value, value); }

		[Fact]
		public void Int32Values_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(Int32Value);
		}

		[DataCompatibilityRangeFact]
		public void Int32Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(Int32Value);
		}

		private const UInt32 UInt32Value = UInt32.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(UInt32 value) { Assert.Equal(UInt32Value, value); }

		[Fact]
		public void UInt32Values_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(UInt32Value);
		}

		[DataCompatibilityRangeFact]
		public void UInt32Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(UInt32Value);
		}

		private const Int64 Int64Value = Int64.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Int64 value) { Assert.Equal(Int64Value, value); }

		[Fact]
		public void Int64Values_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(Int64Value);
		}

		[DataCompatibilityRangeFact]
		public void Int64Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(Int64Value);
		}

#if !NETCOREAPP1_0
		private const UInt64 UInt64Value = UInt64.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(UInt64 value) { Assert.Equal(UInt64Value, value); }

		[Fact]
		public void UInt64Values_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(UInt64Value);
		}

		[DataCompatibilityRangeFact]
		public void UInt64Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(UInt64Value);
		}
#endif

		private const Int16 Int16Value = Int16.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Int16 value) { Assert.Equal(Int16Value, value); }

		[Fact]
		public void Int16Values_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(Int16Value);
		}

		[DataCompatibilityRangeFact]
		public void Int16Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(Int16Value);
		}

		private const UInt16 UInt16Value = UInt16.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(UInt16 value) { Assert.Equal(UInt16Value, value); }

		[Fact]
		public void UInt16Values_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(UInt16Value);
		}

		[DataCompatibilityRangeFact]
		public void UInt16Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(UInt16Value);
		}

		private static readonly BigInteger BigIntegerValue = BigInteger.Parse("2415832045177255062381688311100088888888888888888888888888");

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(BigInteger value) { Assert.Equal(BigIntegerValue, value); }

		[Fact]
		public void BigIntegerValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(BigIntegerValue);
		}

		[DataCompatibilityRangeFact]
		public void BigIntegerValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(BigIntegerValue);
		}

		private const String StringValue = "jkashdgfa$%^&";

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(String value) { Assert.Equal(StringValue, value); }

		[Fact]
		public void StringValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(StringValue);
		}

		[DataCompatibilityRangeFact]
		public void StringValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(StringValue);
		}

		private static readonly TimeSpan TimeSpanValue = TimeSpan.FromDays(1);

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(TimeSpan value) { Assert.Equal(TimeSpanValue, value); }

		[Fact]
		public void TimeSpanValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(TimeSpanValue);
		}

		[DataCompatibilityRangeFact]
		public void TimeSpanValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(TimeSpanValue);
		}

		private static readonly Object ObjectValue = "Hellojkadg";

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Object value) { Assert.Equal(ObjectValue, value); }

		[Fact]
		public void ObjectValues_AreBeingDeserializedAsStrings_Legacy()
		{
			CreateAndPerform(ObjectValue);
		}

		[DataCompatibilityRangeFact]
		public void ObjectValues_AreBeingDeserializedAsStrings()
		{
			CreateAndPerform_WithCompatibilityLevel(ObjectValue);
		}

		private static readonly DateTime DateTimeValue = DateTime.UtcNow;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(DateTime value) {  Assert.Equal(DateTimeValue, value); }

		[Fact]
		public void DateTimeValues_AreBeingDeserializedCorrectly_Legacy()
		{
			// TypeConverter doesn't convert milliseconds by default. No problem, since
			// it is a legacy converter, and newer ones fully support this case.
			var overriddenValue = new DateTime(2014, 08, 24, 23, 12, 30);

			// Don't run this test on Mono – https://bugzilla.xamarin.com/show_bug.cgi?id=25158
			if (Type.GetType("Mono.Runtime") == null)
			{
				CreateAndPerform(overriddenValue);
			}
		}

		[DataCompatibilityRangeFact]
		public void DateTimeValues_AreBeingDeserializedCorrectly()
		{
			// Don't run this test on Mono – https://bugzilla.xamarin.com/show_bug.cgi?id=25158
			if (Type.GetType("Mono.Runtime") == null)
			{
				CreateAndPerform_WithCompatibilityLevel(DateTimeValue);
			}
		}

		private static readonly DateTimeOffset DateTimeOffsetValue = new DateTimeOffset(new DateTime(2012, 12, 12), TimeSpan.FromHours(1));

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(DateTimeOffset value) {  Assert.Equal(DateTimeOffsetValue, value); }

		[Fact]
		public void DateTimeOffsetValues_AreBeingDeserializedCorrectly_Legacy()
		{
			// Don't run this test on Mono – https://bugzilla.xamarin.com/show_bug.cgi?id=25158
			if (Type.GetType("Mono.Runtime") == null)
			{
				CreateAndPerform(DateTimeOffsetValue);
			}
		}

		[DataCompatibilityRangeFact]
		public void DateTimeOffsetValues_AreBeingDeserializedCorrectly()
		{
			// Don't run this test on Mono – https://bugzilla.xamarin.com/show_bug.cgi?id=25158
			if (Type.GetType("Mono.Runtime") == null)
			{
				CreateAndPerform_WithCompatibilityLevel(DateTimeOffsetValue);
			}
		}

#if !NETCOREAPP1_0
		private static readonly CultureInfo CultureInfoValue = new CultureInfo("ru-RU");

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(CultureInfo value) { Assert.Equal(CultureInfoValue, value); }

		[Fact]
		public void CultureInfoValues_AreBeingDeserializedCorrectly_Legacy()
		{
			CreateAndPerform(CultureInfoValue);
		}

		[DataCompatibilityRangeFact]
		public void CultureInfoValues_AreBeingDeserializedCorrectly()
		{
			CreateAndPerform_WithCompatibilityLevel(CultureInfoValue);
		}
#endif

		private const DayOfWeek EnumValue = DayOfWeek.Saturday;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(DayOfWeek value) { Assert.Equal(EnumValue, value); }

		[Fact]
		public void EnumValues_AreBeingDeserializedCorrectly_Legacy()
		{
			CreateAndPerform(EnumValue);
		}

		[DataCompatibilityRangeFact]
		public void EnumValues_AreBeingDeserializedCorrectly()
		{
			CreateAndPerform_WithCompatibilityLevel(EnumValue);
		}

		private static readonly Guid GuidValue = Guid.NewGuid();

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Guid value) { Assert.Equal(GuidValue, value); }

		[Fact]
		public void GuidValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(GuidValue);
		}

		[DataCompatibilityRangeFact]
		public void GuidValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(GuidValue);
		}

		private static readonly Uri UriValue = new Uri("https://example.com", UriKind.Absolute);

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Uri value) { Assert.Equal(UriValue, value); }

		[Fact]
		public void UriValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(UriValue);
		}

		[DataCompatibilityRangeFact]
		public void UriValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(UriValue);
		}

		private static readonly Int64? NotNullNullableValue = Int64.MaxValue;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Int64? value) { Assert.Equal(NotNullNullableValue, value); }

		[Fact]
		public void NotNullNullableValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(NotNullNullableValue);
		}

		[DataCompatibilityRangeFact]
		public void NotNullNullableValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(NotNullNullableValue);
		}

		private static readonly Int32? NullNullableValue = null;

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Int32? value) { Assert.Equal(NullNullableValue, value); }

		[Fact]
		public void NullNullableValues_AreBeingCorrectlyDeserialized_Legacy()
		{
			CreateAndPerform(NullNullableValue);
		}

		[DataCompatibilityRangeFact]
		public void NullNullableValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(NullNullableValue);
		}

		private static readonly string[] ArrayValue = { "Hello", "world" };

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(string[] value) { Assert.Equal(ArrayValue, value); }

		[Fact]
		public void ArrayValues_AreBeingCorrectlyDeserialized_FromJson_Legacy_WithDefaultSettings()
		{
			CreateAndPerform(ArrayValue, true);
		}

		[DataCompatibilityRangeFact]
		public void ArrayValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(ArrayValue);
		}

		private static readonly List<DateTime> ListValue = new List<DateTime> { DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1) };

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(List<DateTime> value) { Assert.Equal(ListValue, value); }

		[Fact]
		public void ListValues_AreBeingCorrectlyDeserialized_FromJson_Legacy()
		{
			CreateAndPerform(ListValue, true);
		}

		[DataCompatibilityRangeFact]
		public void ListValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(ListValue);
		}

		private static readonly IList<DateTime> IListValue = new List<DateTime> { DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1) };

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(IList<DateTime> value) { Assert.Equal(IListValue, value); }

		[Fact]
		public void IListValues_AreBeingCorrectlyDeserialized_FromJson_Legacy()
		{
			CreateAndPerform(IListValue, true);
		}

		[DataCompatibilityRangeFact]
		public void IListValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(IListValue);
		}

		private static readonly Dictionary<TimeSpan, string> DictionaryValue = new Dictionary<TimeSpan, string>
		{
			{ TimeSpan.FromSeconds(1), "123" },
			{ TimeSpan.FromDays(12), "376" }
		};

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(Dictionary<TimeSpan, string> value) { Assert.Equal(DictionaryValue, value); }

		[Fact]
		public void DictionaryValues_AreBeingCorrectlyDeserialized_FromJson_Legacy()
		{
			CreateAndPerform(DictionaryValue, true);
		}

		[DataCompatibilityRangeFact]
		public void DictionaryValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(DictionaryValue);
		}

		private static readonly IDictionary<TimeSpan, string> IDictionaryValue = new Dictionary<TimeSpan, string>
		{
			{ TimeSpan.FromSeconds(1), "123" },
			{ TimeSpan.FromDays(12), "376" }
		};

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(IDictionary<TimeSpan, string> value) { Assert.Equal(IDictionaryValue, value); }

		[Fact]
		public void IDictionaryValues_AreBeingCorrectlyDeserialized_FromJson_Legacy()
		{
			CreateAndPerform(IDictionaryValue, true);
		}

		[DataCompatibilityRangeFact]
		public void IDictionaryValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(IDictionaryValue);
		}

		public struct MyStruct
		{
			public Guid Id { get; set; } 
			public string Name { get; set; }
		}

		private static readonly MyStruct CustomStructValue = new MyStruct { Id = Guid.NewGuid(), Name = "Hangfire" };

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(MyStruct value) { Assert.Equal(CustomStructValue, value); }

		[Fact]
		public void CustomStructValues_AreBeingCorrectlyDeserialized_FromJson_Legacy()
		{
			CreateAndPerform(CustomStructValue, true);
		}

		[DataCompatibilityRangeFact]
		public void CustomStructValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(CustomStructValue);
		}

#pragma warning disable 659
		public class MyClass : IEquatable<MyClass>
		{
			public DateTime CreatedAt { get; set; }

			public bool Equals(MyClass other)
			{
				if (other == null) return false;
				return CreatedAt.Equals(other.CreatedAt);
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as MyClass);
			}
		}
#pragma warning restore 659

		private static readonly MyClass CustomClassValue = new MyClass { CreatedAt = DateTime.UtcNow };

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(MyClass value) { Assert.Equal(CustomClassValue.CreatedAt, value.CreatedAt); }

		[Fact]
		public void CustomClassValues_AreBeingCorrectlyDeserialized_FromJson_Legacy()
		{
			CreateAndPerform(CustomClassValue, true);
		}

		[DataCompatibilityRangeFact]
		public void CustomClassValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform_WithCompatibilityLevel(CustomClassValue);
		}

		private static readonly IEquatable<MyClass> CustomInterfaceValue = new MyClass { CreatedAt = DateTime.UtcNow };

		[UsedImplicitly]
		[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public void Method(IEquatable<MyClass> value) { Assert.Equal(CustomInterfaceValue, value); }

		[Theory, CleanSerializerSettings]
		[InlineData(TypeNameHandling.Objects)]
		[InlineData(TypeNameHandling.All)]
		public void CustomInterfaceValues_AreBeingCorrectlyDeserialized_FromJson_Legacy_WithCustomSettings(TypeNameHandling typeNameHandling)
		{
			SerializationHelper.SetUserSerializerSettings(new JsonSerializerSettings
			{
				TypeNameHandling = typeNameHandling
			});

			CreateAndPerform(CustomInterfaceValue, true);
		}

#if !NET452 && !NET461
		[Theory, CleanSerializerSettings]
		[InlineData(TypeNameHandling.Objects)]
		[InlineData(TypeNameHandling.All)]
		public void CustomInterfaceValues_AreBeingCorrectlyDeserialized_FromJson_Legacy_WithCustomDefaultSettings(TypeNameHandling typeNameHandling)
		{
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				TypeNameHandling = typeNameHandling
			};

			CreateAndPerform(CustomInterfaceValue, true);
		}
#endif

		[DataCompatibilityRangeTheory, CleanSerializerSettings]
		[InlineData(TypeNameHandling.Objects)]
		[InlineData(TypeNameHandling.All)]
		[InlineData(TypeNameHandling.Auto)]
		public void CustomInterfaceValues_AreBeingCorrectlyDeserialized_WithCustomSettings(TypeNameHandling typeNameHandling)
		{
			GlobalConfiguration.Configuration.UseSerializerSettings(new JsonSerializerSettings
			{
				TypeNameHandling = typeNameHandling
			});

			CreateAndPerform_WithCompatibilityLevel(CustomInterfaceValue);
		}

#if !NET452 && !NET461
		[DataCompatibilityRangeTheory, CleanSerializerSettings]
		[InlineData(TypeNameHandling.Objects)]
		[InlineData(TypeNameHandling.All)]
		[InlineData(TypeNameHandling.Auto)]
		public void CustomInterfaceValues_AreBeingCorrectlyDeserialized_WithCustomDefaultSettings(TypeNameHandling typeNameHandling)
		{
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				TypeNameHandling = typeNameHandling
			};

			CreateAndPerform_WithCompatibilityLevel(CustomInterfaceValue);
		}
#endif

		[DataCompatibilityRangeFact, CleanSerializerSettings]
		public void CustomInterfaceValues_AreBeingCorrectlyDeserialized_WithRecommendedSettings()
		{
			GlobalConfiguration.Configuration.UseRecommendedSerializerSettings();
			CreateAndPerform_WithCompatibilityLevel(CustomInterfaceValue);
		}

		private static void CreateAndPerform<T>(T argumentValue, bool checkJsonOnly = false)
		{
			var type = typeof(JobArgumentFacts);
			var methodInfo = type.GetMethod("Method", new[] { typeof(T) });

			var serializationMethods = new List<Tuple<string, string>>();

#if !NETCOREAPP1_0
			if (!checkJsonOnly)
			{
				var converter = TypeDescriptor.GetConverter(typeof(T));
				serializationMethods.Add(new Tuple<string, string>(
					"TypeDescriptor",
					converter.ConvertToInvariantString(argumentValue)));
			}
#endif

			serializationMethods.Add(new Tuple<string, string>(
				"JSON",
				JobHelper.ToJson(argumentValue)));

			foreach (var method in serializationMethods)
			{
				var data = new InvocationData(
					methodInfo?.DeclaringType?.AssemblyQualifiedName,
					methodInfo?.Name,
					JobHelper.ToJson(methodInfo?.GetParameters().Select(x => x.ParameterType).ToArray()),
					JobHelper.ToJson(new[] { method.Item2 }));

				var job = data.DeserializeJob();

				Assert.Equal(argumentValue, job.Args[0]);
			}
		}

		private static void CreateAndPerform_WithCompatibilityLevel<T>(T argumentValue)
		{
			var type = typeof(JobArgumentFacts);
			var methodInfo = type.GetMethod("Method", new[] { typeof(T) }) ?? throw new InvalidOperationException("Method not found");

			var jobToSerialize = new Job(methodInfo.DeclaringType, methodInfo, argumentValue);
			var data = InvocationData.SerializeJob(jobToSerialize);

			var job = data.DeserializeJob();

			Assert.Equal(argumentValue, job.Args[0]);
		}
	}
}
