using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hangfire.Common;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    [SuppressMessage("ReSharper", "ConvertClosureToMethodGroup")]
    public class RecurringJobEntityFacts
    {
        private const string RecurringJobId = "recurring-job-id";
        private readonly DateTime _nowInstant = new DateTime(2017, 03, 30, 15, 30, 0, DateTimeKind.Utc);
        private readonly Dictionary<string,string> _recurringJob;
        private readonly Mock<ITimeZoneResolver> _timeZoneResolver;

        public RecurringJobEntityFacts()
        {
            var timeZone = TimeZoneInfo.Local;

            _recurringJob = new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", InvocationData.SerializeJob(Job.FromExpression(() => Console.WriteLine())).SerializePayload() },
                { "TimeZoneId", timeZone.Id }
            };

            _timeZoneResolver = new Mock<ITimeZoneResolver>();
            _timeZoneResolver.Setup(x => x.GetTimeZoneById(It.IsAny<string>())).Throws<InvalidTimeZoneException>();
            _timeZoneResolver.Setup(x => x.GetTimeZoneById(timeZone.Id)).Returns(timeZone);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenRecurringJobId_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new RecurringJobEntity(null, _recurringJob, _timeZoneResolver.Object, _nowInstant));

            Assert.Equal("recurringJobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenRecurringJob_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new RecurringJobEntity(RecurringJobId, null, _timeZoneResolver.Object, _nowInstant));

            Assert.Equal("recurringJob", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimeZoneResolver_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new RecurringJobEntity(RecurringJobId, _recurringJob, null, _nowInstant));

            Assert.Equal("timeZoneResolver", exception.ParamName);
        }

        [Fact]
        public void Ctor_SetsRelaxedMisfireOption_WhenCorrespondingKeyIsMissing()
        {
            var entity = CreateEntity();
            Assert.Equal(MisfireHandlingMode.Relaxed, entity.MisfireHandling);
        }

        [Fact]
        public void Ctor_CorrectlyParses_RelaxedMisfireOption_SerializedAsInt()
        {
            _recurringJob["Misfire"] = "0";

            var entity = CreateEntity();

            Assert.Equal(MisfireHandlingMode.Relaxed, entity.MisfireHandling);
        }

        [Fact]
        public void Ctor_CorrectlyParses_StrictMisfireOption_SerializedAsInt()
        {
            _recurringJob["Misfire"] = "1";

            var entity = CreateEntity();

            Assert.Equal(MisfireHandlingMode.Strict, entity.MisfireHandling);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMisfireOption_CanNotBeParsed()
        {
            _recurringJob["Misfire"] = "2adgadsg";
            Assert.Throws<ArgumentException>(() => CreateEntity());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMisfireOption_IsNotWithinAValidRange()
        {
            _recurringJob["Misfire"] = "2";
            Assert.Throws<NotSupportedException>(() => CreateEntity());
        }

        [Fact]
        public void IsChanged_DoesNotAddMisfireKey_WhenItIsNotPresentAndDefaultValueIsUnchanged()
        {
            var entity = CreateEntity();

            entity.IsChanged(out var fields, out _);

            Assert.False(fields.ContainsKey("Misfire"));
        }

        [Fact]
        public void IsChanged_DoesNotAddMisfireKey_WhenItIsSetToDefaultAndDefaultValueIsUnchanged()
        {
            _recurringJob["Misfire"] = "0";
            var entity = CreateEntity();

            entity.IsChanged(out var fields, out _);

            Assert.False(fields.ContainsKey("Misfire"));
        }

        [Fact]
        public void IsChanged_AddsMisfireKey_WhenItIsNotPresent_ButDefaultValueIsChanged()
        {
            var entity = CreateEntity();
            entity.MisfireHandling = MisfireHandlingMode.Strict;

            entity.IsChanged(out var fields, out _);

            Assert.True(fields.ContainsKey("Misfire"));
            Assert.Equal("1", fields["Misfire"]);
        }

        [Fact]
        public void IsChanged_ExplicitlySetsTheDefaultValue_WhenItWasSetToStrict()
        {
            _recurringJob["Misfire"] = "1";
            var entity = CreateEntity();
            entity.MisfireHandling = MisfireHandlingMode.Relaxed;

            entity.IsChanged(out var fields, out _);

            Assert.True(fields.ContainsKey("Misfire"));
            Assert.Equal("0", fields["Misfire"]);
        }

        [Fact]
        public void IsChanged_DoesNotAddMisfireKey_WhenItsNonDefaultValueIsUnchanged()
        {
            _recurringJob["Misfire"] = "1";
            var entity = CreateEntity();
            entity.MisfireHandling = MisfireHandlingMode.Strict;

            entity.IsChanged(out var fields, out _);

            Assert.False(fields.ContainsKey("Misfire"));
        }

        private RecurringJobEntity CreateEntity()
        {
            return new RecurringJobEntity(RecurringJobId, _recurringJob, _timeZoneResolver.Object, _nowInstant);
        }
    }
}