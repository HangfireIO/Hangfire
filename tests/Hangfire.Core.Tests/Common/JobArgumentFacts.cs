using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Hangfire.Common;
using Moq;
using Newtonsoft.Json;
using Xunit;

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

        private const Double DoubleValue = 3.14159265359D;
		public void Method(Double value) { Assert.Equal(DoubleValue, value); }

		[Fact]
		public void DoubleValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(DoubleValue);
		}

        private const Single SingleValue = 3.1415F;
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
		public void Method(DateTimeOffset value) {  Assert.Equal(DateTimeOffsetValue, value); }

		[Fact]
		public void DateTimeOffsetValues_AreBeingDeserializedCorrectly()
		{
			// Don't run this test on Mono – https://bugzilla.xamarin.com/show_bug.cgi?id=25158
                	if (Type.GetType("Mono.Runtime") == null)
	        	{
				CreateAndPerform(DateTimeOffsetValue);
			}
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
		public void NullNullableValues_AreBeingCorrectlyDeserialized()
		{
			CreateAndPerform(NullNullableValue);
		}

		private static readonly string[] ArrayValue = { "Hello", "world" };
		public void Method(string[] value) { Assert.Equal(ArrayValue, value); }

		[Fact]
		public void ArrayValues_AreBeingCorrectlyDeserialized_FromJson()
		{
			CreateAndPerform(ArrayValue, true);
		}

		private static readonly List<DateTime> ListValue = new List<DateTime> { DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1) };
		public void Method(List<DateTime> value) { Assert.Equal(ListValue, value); }

		[Fact]
		public void ListValues_AreBeingCorrectlyDeserialized_FromJson()
		{
			CreateAndPerform(ListValue, true);
		}

		private static readonly Dictionary<TimeSpan, string> DictionaryValue = new Dictionary<TimeSpan, string>
		{
			{ TimeSpan.FromSeconds(1), "123" },
			{ TimeSpan.FromDays(12), "376" }
		};  
		public void Method(Dictionary<TimeSpan, string> value) { Assert.Equal(DictionaryValue, value); }

		[Fact]
		public void DictionaryValues_AreBeingCorrectlyDeserialized_FromJson()
		{
			CreateAndPerform(DictionaryValue, true);
		}

		public struct MyStruct
		{
			public Guid Id { get; set; } 
			public string Name { get; set; }
		}

		private static readonly MyStruct CustomStructValue = new MyStruct { Id = Guid.NewGuid(), Name = "Hangfire" };
		public void Method(MyStruct value) { Assert.Equal(CustomStructValue, value); }

		[Fact]
		public void CustomStructValues_AreBeingCorrectlyDeserialized_FromJson()
		{
			CreateAndPerform(CustomStructValue, true);
		}

		public class MyClass
		{
			public DateTime CreatedAt { get; set; }
		}
		
		private static readonly MyClass CustomClassValue = new MyClass { CreatedAt = DateTime.UtcNow };
		public void Method(MyClass value) { Assert.Equal(CustomClassValue.CreatedAt, value.CreatedAt); }

		[Fact]
		public void CustomClassValues_AreBeingCorrectlyDeserialized_FromJson()
		{
			CreateAndPerform(CustomClassValue, true);
		}

		private void CreateAndPerform<T>(T argumentValue, bool checkJsonOnly = false)
		{
			var type = typeof(JobArgumentFacts);
			var methodInfo = type.GetMethod("Method", new[] { typeof(T) });

			var serializationMethods = new List<Tuple<string, Func<string>>>();

			if (!checkJsonOnly)
			{
				var converter = TypeDescriptor.GetConverter(typeof(T));
				serializationMethods.Add(new Tuple<string, Func<string>>(
					"TypeDescriptor",
					() => converter.ConvertToInvariantString(argumentValue)));
			}

			serializationMethods.Add(new Tuple<string, Func<string>>(
				"JSON",
				() => JsonConvert.SerializeObject(argumentValue)));

			foreach (var method in serializationMethods)
			{
				var job = new Job(type, methodInfo, new[] { method.Item2() });
				job.Perform(_activator.Object, _token.Object);	
			}
		}
	}
}
