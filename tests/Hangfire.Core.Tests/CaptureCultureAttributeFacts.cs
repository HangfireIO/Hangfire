using System.Globalization;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class CaptureCultureAttributeFacts
    {
        private readonly CreateContextMock _context;
        private readonly PerformContextMock _perform;
        private readonly CultureInfo _culture;
        private readonly CultureInfo _uiCulture;

        public CaptureCultureAttributeFacts()
        {
            _context = new CreateContextMock();
            _perform = new PerformContextMock();
            _culture = new CultureInfo("th-TH");
            _uiCulture = new CultureInfo("vi-VN");

            SetCurrentCulture(CultureInfo.InvariantCulture);
            SetCurrentUICulture(CultureInfo.InvariantCulture);

            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentCulture"))
                .Returns($"\"{_culture.Name}\"");

            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentUICulture"))
                .Returns($"\"{_uiCulture.Name}\"");
        }
        
        [Fact]
        public void OnCreating_SetsCultureRelated_Parameters()
        {
            // Arrange
            var attribute = new CaptureCultureAttribute();
            SetCurrentCulture(_culture);
            SetCurrentUICulture(_uiCulture);

            // Act
            attribute.OnCreating(_context.GetCreatingContext());

            // Assert
            Assert.Equal(_context.Object.Parameters["CurrentCulture"], _culture.Name);
            Assert.Equal(_context.Object.Parameters["CurrentUICulture"], _uiCulture.Name);
        }

        [Fact]
        public void OnCreated_DoesNotThrow_NotImplementedException()
        {
            // Arrange
            var attribute = new CaptureCultureAttribute();

            // Act
            attribute.OnCreated(_context.GetCreatedContext("id"));

            // Assert – does not throw
        }

        [Fact]
        public void OnPerforming_ReadsCorrespondingJobParameters_AndSetCurrentCultures()
        {
            // Arrange
            var attribute = new CaptureCultureAttribute();

            // Act
            attribute.OnPerforming(_perform.GetPerformingContext());

            // Assert
            Assert.Equal(_culture, CultureInfo.CurrentCulture);
            Assert.Equal(_uiCulture, CultureInfo.CurrentUICulture);
        }

        [Fact]
        public void OnPerforming_UsesTheSameCultureForUI_WhenCultureIsSetButUICultureIsMissing()
        {
            // Arrange
            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentUICulture"))
                .Returns((string)null);

            var attribute = new CaptureCultureAttribute();

            // Act
            attribute.OnPerforming(_perform.GetPerformingContext());

            // Assert
            Assert.Equal(_culture, CultureInfo.CurrentCulture);
            Assert.Equal(_culture, CultureInfo.CurrentUICulture);
        }

        [Fact]
        public void OnPerforming_DoesNotSetAnything_WhenBothJobParametersMissing()
        {
            // Arrange
            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentCulture"))
                .Returns((string)null);

            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentUICulture"))
                .Returns((string)null);

            var attribute = new CaptureCultureAttribute();

            // Act
            attribute.OnPerforming(_perform.GetPerformingContext());

            // Assert
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentUICulture);
        }

        [Fact]
        public void OnPerforming_DoesNotSetAnything_InCaseOfCultureNotFoundException()
        {
            // Arrange
            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentCulture"))
                .Returns("\"xx-XX\"");

            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentUICulture"))
                .Returns("\"yy-YY\"");

            var attribute = new CaptureCultureAttribute();

            // Act
            attribute.OnPerforming(_perform.GetPerformingContext());

            // Assert
            if (CultureInfo.CurrentCulture.Name != "xx-XX")
                Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
            if (CultureInfo.CurrentUICulture.Name != "yy-YY")
                Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentUICulture);
        }

        [Fact]
        public void OnPerformed_ResetsCurrentCultures_ToTheirOriginalValues()
        {
            // Arrange
            var attribute = new CaptureCultureAttribute();
            attribute.OnPerforming(_perform.GetPerformingContext());

            // Act
            attribute.OnPerformed(_perform.GetPerformedContext());

            // Assert
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentUICulture);
        }

        [Fact]
        public void OnPerformed_DoesNotThrow_WhenCanNotRestoreOriginalCultures()
        {
            // Arrange
            var attribute = new CaptureCultureAttribute();

            // Act
            attribute.OnPerformed(_perform.GetPerformedContext());

            // Assert – does not throw
        }

        private static void SetCurrentCulture(CultureInfo value)
        {
#if !NETCOREAPP1_0
            System.Threading.Thread.CurrentThread.CurrentCulture = value;
#else
            CultureInfo.CurrentCulture = value;
#endif
        }

        // ReSharper disable once InconsistentNaming
        private static void SetCurrentUICulture(CultureInfo value)
        {
#if !NETCOREAPP1_0
            System.Threading.Thread.CurrentThread.CurrentUICulture = value;
#else
            CultureInfo.CurrentUICulture = value;
#endif
        }
    }
}