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
        public void OnPerforming_ReadsCorrespondingJobParameters_AndSetCurrentCultures()
        {
            // Arrange
            SetCurrentCulture(CultureInfo.InvariantCulture);
            SetCurrentUICulture(CultureInfo.InvariantCulture);

            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentCulture"))
                .Returns($"\"{_culture.Name}\"");

            _perform.Connection
                .Setup(x => x.GetJobParameter(_perform.BackgroundJob.Id, "CurrentUICulture"))
                .Returns($"\"{_uiCulture.Name}\"");

            var attribute = new CaptureCultureAttribute();

            // Act
            attribute.OnPerforming(_perform.GetPerformingContext());

            // Assert
            Assert.Equal(_culture, CultureInfo.CurrentCulture);
            Assert.Equal(_uiCulture, CultureInfo.CurrentUICulture);
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