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
            var performanceProcess = new DefaultJobPerformanceProcess(_jobActivator);
            var stateMachineFactory = new StateMachineFactory(storage);

            for (var i = 0; i < options.WorkerCount; i++)
            {
                var context = new WorkerContext(serverId, options.Queues, i + 1);
                yield return new Worker(context, storage, performanceProcess, stateMachineFactory);
            }

            yield return new ServerHeartbeat(storage, serverId);
            yield return new SchedulePoller(storage, stateMachineFactory, options.SchedulePollingInterval);
            yield return new ServerWatchdog(storage, options.ServerWatchdogOptions);

            yield return new RecurringJobScheduler(
                storage,
                new BackgroundJobClient(storage, stateMachineFactory),
                new ScheduleInstantFactory(),
                new EveryMinuteThrottler());
        }
    }
}