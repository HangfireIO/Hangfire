#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using Hangfire;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class DiagnosticsActivityFilterFacts
    {
        // dotnet test -f net6.0 -l "console;verbosity=detailed" --filter "FullyQualifiedName~Hangfire.Core.Tests.DiagnosticsActivityFilterFacts"

        private readonly CreateContextMock _createContext;
        private readonly PerformContextMock _performContext;

        public DiagnosticsActivityFilterFacts()
        {
            _createContext = new CreateContextMock();
            _performContext = new PerformContextMock();
        }

        [Fact]
        public void OnCreating_WhenActivityIsConfigured_CreationContextIsStored()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => {},
                ActivityStopped = activity => {}
            };
            ActivitySource.AddActivityListener(listener);

            var testActivity = new Activity("test");
            testActivity.Start();
            var expectedTraceId = testActivity.TraceId.ToHexString();

            var filter = CreateFilter();

            // Act
            filter.OnCreating(_createContext.GetCreatingContext());

            // Assert
            Assert.Equal(((string)_createContext.Object.Parameters["traceparent"]).Substring(3,32), expectedTraceId);
        }

        [Fact]
        public void OnPerforming_WhenActivityParametersExist_CreationContextIsUsed()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => {},
                ActivityStopped = activity => {}
            };
            ActivitySource.AddActivityListener(listener);

            var expectedTraceId = "abcdef0123456789abcdef0123456789";
            var traceParent = $"00-{expectedTraceId}-123456789abcdef0-01";
            _performContext.Connection
                .Setup(x => x.GetJobParameter(_performContext.BackgroundJob.Id, "traceparent"))
                .Returns($"\"{traceParent}\"");

            var filter = CreateFilter();

            // Act
            filter.OnPerforming(_performContext.GetPerformingContext());

            // Assert
            Assert.Equal(Activity.Current.TraceId.ToHexString(), expectedTraceId);
        }

        private DiagnosticsActivityFilter CreateFilter()
        {
            return new DiagnosticsActivityFilter();
        }
    }
}
#endif
