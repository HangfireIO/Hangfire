using System;
using System.ComponentModel;
using System.Globalization;
using Hangfire.Common;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
	public class TypeDescriptorArgumentDeserializationTests
	{
		private readonly Mock<JobActivator> _activator;
		private readonly Mock<IJobCancellationToken> _token;

		public TypeDescriptorArgumentDeserializationTests()
		{
			_activator = new Mock<JobActivator>();
			_activator.Setup(x => x.ActivateJob(It.IsAny<Type>()))
				      .Returns(() => new TypeDescriptorArgumentDeserializationTests());

			_token = new Mock<IJobCancellationToken>();
		}

		private const Boolean BooleanValue = true;
		public void Method(Boolean value) { Assert.Equal(BooleanValue, value); }

		[Fact]
		public void BooleanArguments_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(BooleanValue);
		}

		private const Byte ByteValue = 142;
		public void Method(Byte value) { Assert.Equal(ByteValue, value); }

		[Fact]
		public void ByteValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(ByteValue);
		}

		private const SByte SByteValue = -111;
		public void Method(SByte value) { Assert.Equal(SByteValue, value); }

		[Fact]
		public void SByteValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(SByteValue);
		}

		private const Char CharValue = Char.MaxValue;
		public void Method(Char value) { Assert.Equal(CharValue, value); }

		[Fact]
		public void CharValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(CharValue);
		}

		private const Decimal DecimalValue = Decimal.MaxValue;
		public void Method(Decimal value) { Assert.Equal(DecimalValue, value); }

		[Fact]
		public void DecimalValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(DecimalValue);
		}

		private const Double DoubleValue = Double.MaxValue;
		public void Method(Double value) { Assert.Equal(DoubleValue, value); }

		[Fact]
		public void DoubleValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(DoubleValue);
		}

		private const Single SingleValue = Single.MaxValue;
		public void Method(Single value) { Assert.Equal(SingleValue, value); }

		[Fact]
		public void SingleValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(SingleValue);
		}

		private const Int32 Int32Value = Int32.MaxValue;
		public void Method(Int32 value) { Assert.Equal(Int32Value, value); }

		[Fact]
		public void Int32Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(Int32Value);
		}

		private const UInt32 UInt32Value = UInt32.MaxValue;
		public void Method(UInt32 value) { Assert.Equal(UInt32Value, value); }

		[Fact]
		public void UInt32Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(UInt32Value);
		}

		private const Int64 Int64Value = Int64.MaxValue;
		public void Method(Int64 value) { Assert.Equal(Int64Value, value); }

		[Fact]
		public void Int64Values_AreBeingCorrectyDeserialized()
		{
			CreateAndPerform(Int64Value);
		}

		private const UInt64 UInt64Value = UInt64.MaxValue;
		public void Method(UInt64 value) { Assert.Equal(UInt64Value, value); }

		[Fact]
		public void UInt64Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(UInt64Value);
		}

		private const Int16 Int16Value = Int16.MaxValue;
		public void Method(Int16 value) { Assert.Equal(Int16Value, value); }

		[Fact]
		public void Int16Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(Int16Value);
		}

		private const UInt16 UInt16Value = UInt16.MaxValue;
		public void Method(UInt16 value) { Assert.Equal(UInt16Value, value); }

		[Fact]
		public void UInt16Values_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(UInt16Value);
		}

		private const String StringValue = "jkashdgfa$%^&";
		public void Method(String value) { Assert.Equal(StringValue, value); }

		[Fact]
		public void StringValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(StringValue);
		}

		private static readonly TimeSpan TimeSpanValue = TimeSpan.FromDays(1);
		public void Method(TimeSpan value) { Assert.Equal(TimeSpanValue, value); }

		[Fact]
		public void TimeSpanValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(TimeSpanValue);
		}

		private static readonly Object ObjectValue = "Hellojkadg";
		public void Method(Object value) { Assert.Equal(ObjectValue, value); }

		[Fact]
		public void ObjectValues_AreBeingDeserializedAsStrings()
		{
			CreateAndPerform(ObjectValue);
		}

		private static readonly DateTimeOffset DateTimeOffsetValue = new DateTimeOffset(new DateTime(2012, 12, 12), TimeSpan.FromHours(1));
		public void Method(DateTimeOffset value) { Assert.Equal(DateTimeOffsetValue, value); }

		[Fact]
		public void DateTimeOffsetValues_AreBeingDeserializedCorrectly()
		{
			CreateAndPerform(DateTimeOffsetValue);
		}

		private static readonly CultureInfo CultureInfoValue = CultureInfo.GetCultureInfo("ru-RU");
		public void Method(CultureInfo value) { Assert.Equal(CultureInfoValue, value); }

		[Fact]
		public void CultureInfoValues_AreBeingDeserializedCorrectly()
		{
			CreateAndPerform(CultureInfoValue);
		}

		private const DayOfWeek EnumValue = DayOfWeek.Saturday;
		public void Method(DayOfWeek value) { Assert.Equal(EnumValue, value); }

		[Fact]
		public void EnumValues_AreBeingDeserializedCorrectly()
		{
			CreateAndPerform(EnumValue);
		}

		private static readonly Guid GuidValue = Guid.NewGuid();
		public void Method(Guid value) { Assert.Equal(GuidValue, value); }

		[Fact]
		public void GuidValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(GuidValue);
		}

		private static readonly Uri UriValue = new Uri("http://example.com", UriKind.Absolute);
		public void Method(Uri value) { Assert.Equal(UriValue, value); }

		[Fact]
		public void UriValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(UriValue);
		}

		private static readonly Int64? NotNullNullableValue = Int64.MaxValue;
		public void Method(Int64? value) { Assert.Equal(NotNullNullableValue, value); }

		[Fact]
		public void NotNullNullableValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(NotNullNullableValue);
		}

		private static readonly Int32? NullNullableValue = null;
		public void Method(Int32? value) { Assert.Equal(NullNullableValue, value); }

		[Fact]
		public void NullNullableValue_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(NullNullableValue);
		}

		private void CreateAndPerform<T>(T argumentValue)
		{
			var job = CreateJob(argumentValue);
			job.Perform(_activator.Object, _token.Object);
		}

		private static Job CreateJob<T>(T argumentValue)
		{
			var type = typeof (TypeDescriptorArgumentDeserializationTests);
			var methodInfo = type.GetMethod("Method", new []{ typeof(T) });

			var converter = TypeDescriptor.GetConverter(typeof(T));

			return new Job(type, methodInfo, new []{ converter.ConvertToInvariantString(argumentValue) });
		}
	}
}
