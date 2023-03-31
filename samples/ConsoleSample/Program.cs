using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Hangfire.States;
using Microsoft.Owin.Hosting;

namespace ConsoleSample
{
    public static class Program
    {
        public static void Main()
        {
            GlobalConfiguration.Configuration
                .UseColouredConsoleLogProvider()
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseIgnoredAssemblyVersionTypeResolver()
                .UseRecommendedSerializerSettings()
                .UseResultsInContinuations()
                .UseJobDetailsRenderer(10, dto => throw new InvalidOperationException())
                .UseJobDetailsRenderer(10, dto => new NonEscapedString("<h4>Hello, world!</h4>"))
                .UseDefaultCulture(CultureInfo.CurrentCulture)
                .UseSqlServerStorage(@"Server=.\;Database=Hangfire.Sample;Trusted_Connection=True;", new SqlServerStorageOptions
                {
                    EnableHeavyMigrations = true
                });

            Console.WriteLine(SerializationHelper.Serialize(new ExceptionInfo(new OperationCanceledException()), SerializationOption.Internal));

            var backgroundJobs = new BackgroundJobClient();
            backgroundJobs.RetryAttempts = 5;

            NewFeatures.Test(throwException: false);
            NewFeatures.Test(throwException: true);

            var job1 = BackgroundJob.Enqueue<Services>(x => x.WriteIndex(0));
            var job2 = BackgroundJob.ContinueJobWith<Services>(job1, "default",  x => x.WriteIndex(default));
            var job3 = BackgroundJob.ContinueJobWith<Services>(job2, "critical", x => x.WriteIndex(default));
            var job4 = BackgroundJob.ContinueJobWith<Services>(job3, "default",  x => x.WriteIndex(default));
            var job5 = BackgroundJob.ContinueJobWith<Services>(job4, "critical", x => x.WriteIndex(default));

            RecurringJob.AddOrUpdate("seconds", () => Console.WriteLine("Hello, seconds!"), "*/15 * * * * *");
            RecurringJob.AddOrUpdate("Console.WriteLine", () => Console.WriteLine("Hello, world!"), Cron.Minutely);
            RecurringJob.AddOrUpdate("hourly", () => Console.WriteLine("Hello"), "25 15 * * *");
            RecurringJob.AddOrUpdate("neverfires", () => Console.WriteLine("Can only be triggered"), "0 0 31 2 *");

            RecurringJob.AddOrUpdate("Hawaiian", () => Console.WriteLine("Hawaiian"), "15 08 * * *", new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time")
            });

            RecurringJob.AddOrUpdate("UTC", "critical", () => Console.WriteLine("UTC"), "15 18 * * *");

            RecurringJob.AddOrUpdate("Russian", () => Console.WriteLine("Russian"), "15 21 * * *", new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

            using (WebApp.Start<Startup>("http://localhost:12345"))
            {
                var count = 1;

                while (true)
                {
                    var command = Console.ReadLine();

                    if (command == null || command.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (command.StartsWith("add", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(4));
                            for (var i = 0; i < workCount; i++)
                            {
                                var number = i;
                                BackgroundJob.Enqueue<Services>(x => x.Random(number));
                            }
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("async", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(6));
                            for (var i = 0; i < workCount; i++)
                            {
                                BackgroundJob.Enqueue<Services>(x => x.Async(CancellationToken.None));
                            }
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("static", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(7));
                            for (var i = 0; i < workCount; i++)
                            {
                                BackgroundJob.Enqueue(() => Console.WriteLine("Hello, {0}!", "world"));
                            }
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(6));
                        for (var i = 0; i < workCount; i++)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.Error());
                        }
                    }

                    if (command.StartsWith("args", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(5));
                        for (var i = 0; i < workCount; i++)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.Args(Guid.NewGuid().ToString(), 14442, DateTime.UtcNow));
                        }
                    }

                    if (command.StartsWith("custom", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(7));
                        for (var i = 0; i < workCount; i++)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.Custom(
                                new Random().Next(),
                                new[] { "Hello", "world!" },
                                new Services.CustomObject { Id = 123 },
                                DayOfWeek.Friday
                                ));
                        }
                    }

                    if (command.StartsWith("fullargs", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(9));
                        for (var i = 0; i < workCount; i++)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.FullArgs(
                                false,
                                123,
                                'c',
                                DayOfWeek.Monday,
                                "hello",
                                new TimeSpan(12, 13, 14),
                                new DateTime(2012, 11, 10),
                                new Services.CustomObject { Id = 123 },
                                new[] { "1", "2", "3" },
                                new[] { 4, 5, 6 },
                                new long[0],
                                null,
                                new List<string> { "7", "8", "9" }));
                        }
                    }

                    if (command.StartsWith("in", StringComparison.OrdinalIgnoreCase))
                    {
                        var seconds = int.Parse(command.Substring(2));
                        var number = count++;
                        BackgroundJob.Schedule<Services>("default", x => x.Random(number), TimeSpan.FromSeconds(seconds));
                    }

                    if (command.StartsWith("cancelable", StringComparison.OrdinalIgnoreCase))
                    {
                        var iterations = int.Parse(command.Substring(11));
                        BackgroundJob.Enqueue<Services>(x => x.Cancelable(iterations, JobCancellationToken.Null));
                    }

                    if (command.StartsWith("delete", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(7));
                        var client = new BackgroundJobClient();
                        for (var i = 0; i < workCount; i++)
                        {
                            client.Create<Services>(x => x.EmptyDefault(), new DeletedState(new ExceptionInfo(new OperationCanceledException())));
                        }
                    }

                    if (command.StartsWith("fast", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(5));
                            Parallel.For(0, workCount, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
                            {
                                BackgroundJob.Enqueue<Services>(
                                    i % 2 == 0 ? "critical" : "default",
                                    x => x.EmptyDefault());
                            });
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("generic", StringComparison.OrdinalIgnoreCase))
                    {
                        BackgroundJob.Enqueue<GenericServices<string>>(x => x.Method("hello", 1));
                    }

                    if (command.StartsWith("continuations", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteString("Hello, Hangfire continuations!");
                    }
                }
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        public static void WriteString(string value)
        {
            var lastId = BackgroundJob.Enqueue<Services>(x => x.Write(value[0]));

            for (var i = 1; i < value.Length; i++)
            {
                lastId = BackgroundJob.ContinueJobWith<Services>(lastId, x => x.Write(value[i]));
            }

            BackgroundJob.ContinueJobWith<Services>(lastId, x => x.WriteBlankLine());
        }
    }
}
