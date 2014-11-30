using System.Collections.Generic;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    public class ServerComponentFactory : IServerComponentFactory
    {
        private readonly JobActivator _jobActivator;

        public ServerComponentFactory()
            : this(JobActivator.Current)
        {
        }

        public ServerComponentFactory(JobActivator jobActivator)
        {
            _jobActivator = jobActivator;
        }

        public IEnumerable<IServerComponent> Create(JobStorage storage, string serverId, BackgroundJobServerOptions options)
        {
            var stateMachineFactory = new StateMachineFactory(storage);
            var sharedWorkerContext = new SharedWorkerContext(
                serverId,
                options.Queues,
                storage,
                new JobPerformanceProcess(),
                _jobActivator,
                stateMachineFactory);

            yield return new WorkerManager(sharedWorkerContext, options.WorkerCount);
            yield return new ServerHeartbeat(storage, serverId);
            yield return new ServerWatchdog(storage);
            yield return new SchedulePoller(storage, stateMachineFactory, options.SchedulePollingInterval);

            yield return new RecurringJobScheduler(
                storage,
                new BackgroundJobClient(storage, stateMachineFactory),
                new ScheduleInstantFactory(),
                new EveryMinuteThrottler());
        }
    }
}