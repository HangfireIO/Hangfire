using System;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.UnitOfWork;
using Moq;

namespace Hangfire.Core.Tests
{
    public class SharedWorkerContextMock
    {
        private readonly Lazy<SharedWorkerContext> _context; 

        public SharedWorkerContextMock()
        {
            ServerId = "server";
            Queues = new[] { "default" };
            Storage = new Mock<JobStorage>();
            PerformanceProcess = new Mock<IJobPerformanceProcess>();
            Activator = new Mock<JobActivator>();
            UnitOfWorkManager = new Mock<IUnitOfWorkManager>();
            StateMachineFactory = new Mock<IStateMachineFactory>();

            _context = new Lazy<SharedWorkerContext>(
                () => new SharedWorkerContext(
                    ServerId,
                    Queues,
                    Storage.Object,
                    PerformanceProcess.Object,
                    Activator.Object,
                    UnitOfWorkManager.Object,
                    StateMachineFactory.Object));
        }

        public SharedWorkerContext Object {get { return _context.Value; }}

        public string ServerId { get; set; }
        public string[] Queues { get; set; }
        public Mock<JobStorage> Storage { get; set; }
        public Mock<IJobPerformanceProcess> PerformanceProcess { get; set; } 
        public Mock<JobActivator> Activator { get; set; }
        public Mock<IUnitOfWorkManager> UnitOfWorkManager { get; set; }
        public Mock<IStateMachineFactory> StateMachineFactory { get; set; } 
    }
}
