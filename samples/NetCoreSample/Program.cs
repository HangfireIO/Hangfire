using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.Server;
using Hangfire.SqlServer;
using Hangfire.States;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace NetCoreSample
{
    class Program
    {
        // To use in-memory store instead of database:
        //   dotnet run -- --UseInMemory true

        // To show trace console exporter output: 
        //   dotnet run -- --TraceConsoleExporter true

        public static readonly ActivitySource ActivitySource = new ActivitySource(nameof(NetCoreSample));

        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(x => x
                    .AddSimpleConsole()
                    .SetMinimumLevel(LogLevel.Information))
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<HostOptions>(option =>
                    {
                        option.ShutdownTimeout = TimeSpan.FromSeconds(60);
                    });

                    services.TryAddSingleton<SqlServerStorageOptions>(new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromTicks(1),
                        UseRecommendedIsolationLevel = true,
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(1)
                    });

                    services.TryAddSingleton<IBackgroundJobFactory>(x => new CustomBackgroundJobFactory(
                        new BackgroundJobFactory(x.GetRequiredService<IJobFilterProvider>())));

                    services.TryAddSingleton<IBackgroundJobPerformer>(x => new CustomBackgroundJobPerformer(
                        new BackgroundJobPerformer(
                            x.GetRequiredService<IJobFilterProvider>(),
                            x.GetRequiredService<JobActivator>(),
                            TaskScheduler.Default)));

                    services.TryAddSingleton<IBackgroundJobStateChanger>(x => new CustomBackgroundJobStateChanger(
                            new BackgroundJobStateChanger(x.GetRequiredService<IJobFilterProvider>())));

                    var useInMemory = hostContext.Configuration.GetValue<bool>("UseInMemory");
                    services.AddHangfire((provider, configuration) => {
                        configuration
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                        .UseSimpleAssemblyNameTypeSerializer();
                        if (useInMemory) {
                            configuration.UseInMemoryStorage();
                        }
                        else
                        {
                            configuration.UseSqlServerStorage(
                                @"Server=.\;Database=Hangfire.Sample;Trusted_Connection=True;", 
                                provider.GetRequiredService<SqlServerStorageOptions>());
                        }
                    });

                    services.AddHostedService<RecurringJobsService>();
                    services.AddHostedService<BackgroundJobsService>();
                    services.AddHangfireServer(options =>
                    {
                        options.StopTimeout = TimeSpan.FromSeconds(15);
                        options.ShutdownTimeout = TimeSpan.FromSeconds(30);
                    });

                    var traceConsoleExporter = hostContext.Configuration.GetValue<bool>("TraceConsoleExporter");
                    services.AddOpenTelemetry()
                        .WithTracing(tracing => {
                            tracing.AddSource(DiagnosticsActivityFilter.DefaultListenerName);
                            tracing.AddSource(nameof(NetCoreSample));
                            if (traceConsoleExporter)
                            {
                                tracing.AddConsoleExporter();
                            }
                        });
                })
                .Build();

            await host.RunAsync();
        }
    }

    internal class CustomBackgroundJobFactory : IBackgroundJobFactory
    {
        private readonly IBackgroundJobFactory _inner;

        public CustomBackgroundJobFactory([NotNull] IBackgroundJobFactory inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IStateMachine StateMachine => _inner.StateMachine;

        public BackgroundJob Create(CreateContext context)
        {
            Console.WriteLine($"Create: {context.Job.Type.FullName}.{context.Job.Method.Name} in {context.InitialState?.Name} state");
            return _inner.Create(context);
        }
    }

    internal class CustomBackgroundJobPerformer : IBackgroundJobPerformer
    {
        private readonly IBackgroundJobPerformer _inner;

        public CustomBackgroundJobPerformer([NotNull] IBackgroundJobPerformer inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public object Perform(PerformContext context)
        {
            Console.WriteLine($"Perform {context.BackgroundJob.Id} ({context.BackgroundJob.Job.Type.FullName}.{context.BackgroundJob.Job.Method.Name})");
            return _inner.Perform(context);
        }
    }

    internal class CustomBackgroundJobStateChanger : IBackgroundJobStateChanger
    {
        private readonly IBackgroundJobStateChanger _inner;

        public CustomBackgroundJobStateChanger([NotNull] IBackgroundJobStateChanger inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IState ChangeState(StateChangeContext context)
        {
            Console.WriteLine($"ChangeState {context.BackgroundJobId} to {context.NewState}");
            return _inner.ChangeState(context);
        }
    }

    internal class RecurringJobsService : BackgroundService
    {
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IRecurringJobManager _recurringJobs;
        private readonly ILogger<RecurringJobsService> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public RecurringJobsService(
            [NotNull] IBackgroundJobClient backgroundJobs,
            [NotNull] IRecurringJobManager recurringJobs,
            [NotNull] ILogger<RecurringJobsService> logger,
            ILoggerFactory loggerFactory)
        {
            _backgroundJobs = backgroundJobs ?? throw new ArgumentNullException(nameof(backgroundJobs));
            _recurringJobs = recurringJobs ?? throw new ArgumentNullException(nameof(recurringJobs));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Creating recurring jobs");

                using (var activity =  Program.ActivitySource.StartActivity("enqueue seconds"))
                {
                    _logger.LogInformation("Creating job seconds, trace_id={ActivityTraceId}", activity.TraceId);
                    _recurringJobs.AddOrUpdate("seconds", () => Hello("seconds"), "*/15 * * * * *");
                }

                using (var activity =  Program.ActivitySource.StartActivity("enqueue minutely"))
                {
                    _logger.LogInformation("Creating job minutely (hello world), trace_id={ActivityTraceId}", activity.TraceId);
                    _recurringJobs.AddOrUpdate("minutely", () => Hello("world"), Cron.Minutely);
                }

                _recurringJobs.AddOrUpdate("hourly", () => Console.WriteLine("Hello"), "25 15 * * *");
                _recurringJobs.AddOrUpdate("neverfires", () => Console.WriteLine("Can only be triggered"), "0 0 31 2 *");

                _recurringJobs.AddOrUpdate("Hawaiian", () => Console.WriteLine("Hawaiian"),  "15 08 * * *", new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time")
                });
                _recurringJobs.AddOrUpdate("UTC", () => Console.WriteLine("UTC"), "15 18 * * *");
                _recurringJobs.AddOrUpdate("Russian", () => Console.WriteLine("Russian"), "15 21 * * *", new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occurred while creating recurring jobs.");
            }

            return Task.CompletedTask;
        }

        public void Hello(string name)
        {
            Console.WriteLine($"Hello, {name}!");
            var logger = _loggerFactory.CreateLogger<RecurringJobsService>();
            logger.LogInformation("Hello, {Name}! trace_id={ActivityTraceId}", name, Activity.Current?.TraceId);
        }
    }

    internal class BackgroundJobsService : BackgroundService
    {
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public BackgroundJobsService(
            [NotNull] IBackgroundJobClient backgroundJobs,
            [NotNull] ILogger<BackgroundJobsService> logger,
            ILoggerFactory loggerFactory)
        {
            _backgroundJobs = backgroundJobs ?? throw new ArgumentNullException(nameof(backgroundJobs));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Creating backgriound jobs");

                using (var activity =  Program.ActivitySource.StartActivity("enqueue"))
                {
                    _logger.LogInformation("Creating job 10, trace_id={ActivityTraceId}", activity.TraceId);
                    var jobId1 = _backgroundJobs.Enqueue(() => Job(10));
                }
                using (var activity =  Program.ActivitySource.StartActivity("schedule"))
                {
                    _logger.LogInformation("Scheduling job 20, continue with 30, trace_id={ActivityTraceId}", activity.TraceId);
                    var jobId2 = _backgroundJobs.Schedule(() => Job(20), TimeSpan.FromSeconds(30));
                    var jobId3 = _backgroundJobs.ContinueJobWith(jobId2, () => Job(30));
                }
                using (var activity =  Program.ActivitySource.StartActivity("error"))
                {
                    _logger.LogInformation("Scheduling error job 40, trace_id={ActivityTraceId}", activity.TraceId);
                    var jobId4 = _backgroundJobs.Schedule(() => Job(40), TimeSpan.FromSeconds(60));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occurred while creating recurring jobs.");
            }

            return Task.CompletedTask;
        }

        public void Job(int counter) {
            Console.WriteLine("Hello, job {0}!", counter);
            var logger = _loggerFactory.CreateLogger<BackgroundJobsService>();
            logger.LogInformation("Hello, job {Counter} trace_id={ActivityTraceId}", counter, Activity.Current?.TraceId);
            if (counter == 40)
            {
                throw new InvalidOperationException("Counter 40 is invalid.");
            }
        }
    }

}
