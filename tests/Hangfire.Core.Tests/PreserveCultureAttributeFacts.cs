using System;
using System.Globalization;
using Hangfire.Client;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class PreserveCultureAttributeFacts
    {
        private readonly CreatingContext _creatingContext;
        private readonly PerformingContext _performingContext;
        private readonly PerformedContext _performedContext;
        private readonly Mock<IStorageConnection> _connection;
        private const string JobId = "id";

        public PreserveCultureAttributeFacts()
        {
            _connection = new Mock<IStorageConnection>();

            var storage = new Mock<JobStorage>();
            var backgroundJob = new BackgroundJobMock { Id = JobId };
            var state = new Mock<IState>();

            var createContext = new CreateContext(
                storage.Object, _connection.Object, backgroundJob.Job, state.Object);
            _creatingContext = new CreatingContext(createContext);

            var performContext = new PerformContext(
                _connection.Object, backgroundJob.Object, new Mock<IJobCancellationToken>().Object);
            _performingContext = new PerformingContext(performContext);
            _performedContext = new PerformedContext(performContext, null, false, null);
        }

        [Fact]
        public void OnCreating_ThrowsAnException_WhenContextIsNull()
        {
            var filter = CreateFilter();

            Assert.Throws<ArgumentNullException>(
                () => filter.OnCreating(null));
        }

        [Fact]
        public void OnCreating_CapturesCultures_AndSetsThemAsJobParameters()
        {
            CultureHelper.SetCurrentCulture("ru-RU");
            CultureHelper.SetCurrentUICulture("ru-RU");

            var filter = CreateFilter();
            filter.OnCreating(_creatingContext);

            Assert.Equal("ru-RU", _creatingContext.GetJobParameter<string>("CurrentCulture"));
            Assert.Equal("ru-RU", _creatingContext.GetJobParameter<string>("CurrentUICulture"));
        }

        [Fact]
        public void OnCreating_CapturesInvariantCulture_AndSetsStringEmptyAsJobParameters()
        {
            CultureHelper.SetCurrentCulture(CultureInfo.InvariantCulture);
            CultureHelper.SetCurrentUICulture(CultureInfo.InvariantCulture);

            var filter = CreateFilter();
            filter.OnCreating(_creatingContext);

            Assert.Equal(String.Empty, _creatingContext.GetJobParameter<string>("CurrentCulture"));
            Assert.Equal(String.Empty, _creatingContext.GetJobParameter<string>("CurrentUICulture"));
        }

        [Fact]
        public void OnCreated_DoesNotThrowAnException()
        {
            var filter = CreateFilter();

            // Does not throw
            filter.OnCreated(null);
        }

        [Fact]
        public void OnPerforming_SetsThreadCultures_ToTheSpecifiedOnesInJobParameters()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentCulture")).Returns("\"ru-RU\"");
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentUICulture")).Returns("\"ru-RU\"");

            CultureHelper.SetCurrentCulture("en-US");
            CultureHelper.SetCurrentUICulture("en-US");

            var filter = CreateFilter();
            filter.OnPerforming(_performingContext);

            Assert.Equal("ru-RU", CultureInfo.CurrentCulture.Name);
            Assert.Equal("ru-RU", CultureInfo.CurrentUICulture.Name);
        }

        [Fact]
        public void OnPerforming_SetsInvariantThreadCultures_WhenJobParametersAreEmptyStrings()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentCulture")).Returns("\"\"");
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentUICulture")).Returns("\"\"");

            CultureHelper.SetCurrentCulture("en-US");
            CultureHelper.SetCurrentUICulture("en-US");

            var filter = CreateFilter();
            filter.OnPerforming(_performingContext);

            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentUICulture);
        }

        [Fact]
        public void OnPerforming_DoesNotDoAnything_WhenCultureJobParameterIsNotSet()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentCulture")).Returns((string)null);
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentUICulture")).Returns((string)null);

            CultureHelper.SetCurrentCulture("en-US");
            CultureHelper.SetCurrentUICulture("en-US");

            var filter = CreateFilter();
            filter.OnPerforming(_performingContext);

            Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        }

        [Fact]
        public void OnPerformed_ThrowsAnException_WhenContextIsNull()
        {
            var filter = CreateFilter();

            Assert.Throws<ArgumentNullException>(() => filter.OnPerformed(null));
        }

        [Fact]
        public void OnPerformed_RestoresPreviousCurrentCulture()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentCulture")).Returns("\"ru-RU\"");
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentUICulture")).Returns("\"ru-RU\"");

            CultureHelper.SetCurrentCulture("en-US");
            CultureHelper.SetCurrentUICulture("en-US");

            var filter = CreateFilter();
            filter.OnPerforming(_performingContext);
            filter.OnPerformed(_performedContext);

            Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        }

        [Fact]
        public void OnPerformed_RestoresPreviousCurrentCulture_OnlyIfItWasChanged()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentCulture")).Returns((string)null);
            _connection.Setup(x => x.GetJobParameter(JobId, "CurrentUICulture")).Returns((string)null);

            CultureHelper.SetCurrentCulture("en-US");
            CultureHelper.SetCurrentUICulture("en-US");

            var filter = CreateFilter();
            filter.OnPerforming(_performingContext);
            filter.OnPerformed(_performedContext);

            Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        }

        public static void Sample() { }

        private CaptureCultureAttribute CreateFilter()
        {
            return new CaptureCultureAttribute();
        }
    }
}
