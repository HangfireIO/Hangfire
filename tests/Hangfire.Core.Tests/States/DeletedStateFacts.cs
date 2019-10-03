using System;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class DeletedStateFacts
    {
        [Fact]
        public void StateName_ReturnsDeleted()
        {
            var result = DeletedState.StateName;
            Assert.Equal("Deleted", result);
        }

        [Fact]
        public void NameProperty_ReturnsStateName()
        {
            var state = CreateState();

            var result = state.Name;

            Assert.Equal(DeletedState.StateName, result);
        }

        [Fact]
        public void IsFinalProperty_ReturnsTrue()
        {
            var state = CreateState();

            var result = state.IsFinal;

            Assert.True(result);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsTrue()
        {
            var state = CreateState();

            var result = state.IgnoreJobLoadException;

            Assert.True(result);
        }

        [Fact]
        public void DeletedAtProperty_ReturnsCurrentUtcDate()
        {
            var state = CreateState();

            Assert.True(DateTime.UtcNow.AddMinutes(-1) < state.DeletedAt);
            Assert.True(state.DeletedAt < DateTime.UtcNow.AddMinutes(1));
        }

        [Fact]
        public void SerializeData_ReturnsSerializedStateData()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal(1, data.Count);
            Assert.True(JobHelper.DeserializeDateTime(data["DeletedAt"]) != default(DateTime));
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsCorrectString_Before170()
        {
            var state = new DeletedState();

            var serialized = SerializationHelper.Serialize<IState>(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.DeletedState, Hangfire.Core\",\"Reason\":null}",
                serialized);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsCorrectString_After170()
        {
            var state = new DeletedState();

            var serialized = SerializationHelper.Serialize<IState>(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.DeletedState, Hangfire.Core\"}",
                serialized);
        }

        private static DeletedState CreateState()
        {
            return new DeletedState();
        }
    }
}
