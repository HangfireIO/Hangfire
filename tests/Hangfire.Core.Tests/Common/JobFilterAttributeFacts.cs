using System;
using Hangfire.Common;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class JobFilterAttributeFacts
    {
        [Fact]
        public void SetOrder_ThrowsAnException_WhenValueIsLessThanDefaultOrder()
        {
            var filterAttribute = new Mock<JobFilterAttribute> { CallBase = true };
            Assert.Throws<ArgumentOutOfRangeException>(
                () => filterAttribute.Object.Order = -2);
        }

        [Fact]
        public void TypeId_Property_IsNotIncludedIntoSerializedForm()
        {
            var attribute = new SampleJobAttribute();
            var serialized = SerializationHelper.Serialize(attribute);
            Assert.DoesNotContain("TypeId", serialized);
        }

        [Fact]
        public void AllowMultiple_Property_IsNotIncludedIntoSerializedForm_SinceItIsGetOnlyProperty()
        {
            var attribute = new SampleJobAttribute();
            var serialized = SerializationHelper.Serialize(attribute);
            Assert.DoesNotContain("AllowMultiple", serialized);
        }        

        [Fact]
        public void Order_Property_IsNotIncludedIntoSerializedForm_WhenDefaultValueIsUsed()
        {
            var attribute = new SampleJobAttribute();
            var serialized = SerializationHelper.Serialize(attribute);
            Assert.DoesNotContain("Order", serialized);
        }

        [Fact]
        public void Order_Property_IsIncludedIntoSerializedForm_WhenNonDefaultValueIsUsed()
        {
            var attribute = new SampleJobAttribute { Order = 555 };
            var serialized = SerializationHelper.Serialize(attribute);
            Assert.Contains("\"Order\":555", serialized);
        }

        [Fact]
        public void Order_Property_ProperlyHandlesDefaultValue_WhenBeingDeserialized()
        {
            var attribute = SerializationHelper.Deserialize<SampleJobAttribute>("{}");
            Assert.Equal(-1, attribute.Order);
        }

        private sealed class SampleJobAttribute : JobFilterAttribute
        {
        }
    }
}
