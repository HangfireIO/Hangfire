using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class BackgroundProcessExtensionsFacts
    {
        private readonly BackgroundProcessContextMock _context;

        public BackgroundProcessExtensionsFacts()
        {
            _context = new BackgroundProcessContextMock();
        }

        [Fact]
        public void CreateTask_ThrowsAnException_WhenProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundProcessExtensions.CreateTask(null, _context.Object));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact]
        public void CreateTask_ThrowsAnException_WhenProcessIsOfCustomType()
        {
            var process = CreateProcess<ILongRunningProcess>();
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => BackgroundProcessExtensions.CreateTask(process.Object, _context.Object));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact]
        public void CreateTask_ReturnsALongRunningTask()
        {
            var task = CreateProcess<IBackgroundProcess>().Object.CreateTask(_context.Object);

            Assert.True(task.CreationOptions.HasFlag(TaskCreationOptions.LongRunning));
        }

        [Fact]
        public void CreateTask_ReturnsATask_ThatCallsTheExecuteMethod_OfAGivenComponent()
        {
            var component = CreateProcess<IServerComponent>();
            var task = component.Object.CreateTask(_context.Object);

            task.Wait();
            
            component.Verify(x => x.Execute(_context.CancellationTokenSource.Token), Times.Once);
        }

        [Fact]
        public void CreateTask_ReturnsATask_ThatCallsTheExecuteMethod_OfAGivenBackgroundProcess()
        {
            var process = CreateProcess<IBackgroundProcess>();
            var task = process.Object.CreateTask(_context.Object);

            task.Wait();

            process.Verify(x => x.Execute(_context.Object), Times.Once);
        }

        [Fact]
        public void CreateTask_ReturnsATask_ThatDoesNotThrowAnyException()
        {
            var process = CreateProcess<IBackgroundProcess>();
            process.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>())).Throws<InvalidOperationException>();
            var task = process.Object.CreateTask(_context.Object);

            Assert.DoesNotThrow(() => task.Wait());
        }

        private Mock<T> CreateProcess<T>()
            where T : class, ILongRunningProcess
        {
            return new Mock<T>();
        }
    }
}
