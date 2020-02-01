using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Server;
using Moq;

namespace Hangfire.Core.Tests
{
    public class BackgroundProcessContextMock
    {
        private readonly Lazy<BackgroundProcessContext> _context;

        public BackgroundProcessContextMock()
        {
            ServerId = "server";
            Storage = new Mock<JobStorage>();
            Properties = new Dictionary<string, object>();
            ExecutionId = Guid.NewGuid();
            StoppingTokenSource = new CancellationTokenSource();
            StoppedTokenSource = new CancellationTokenSource();
            ShutdownTokenSource = new CancellationTokenSource();

            _context = new Lazy<BackgroundProcessContext>(
                () => new BackgroundProcessContext(ServerId, Storage.Object, Properties, ExecutionId,
                    StoppingTokenSource.Token, StoppedTokenSource.Token, ShutdownTokenSource.Token));
        }

        public BackgroundProcessContext Object => _context.Value;

        public string ServerId { get; set; }
        public Mock<JobStorage> Storage { get; set; }
        public IDictionary<string, object> Properties { get; set; } 
        public Guid ExecutionId { get; set; }
        public CancellationTokenSource StoppingTokenSource { get; set; }
        public CancellationTokenSource StoppedTokenSource { get; set; }
        public CancellationTokenSource ShutdownTokenSource { get; set; }
    }
}
