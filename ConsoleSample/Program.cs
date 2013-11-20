using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HangFire;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;

namespace ConsoleSample
{
    [Queue("critical")]
    public class FastCriticalJob : BackgroundJob
    {
        public override void Perform()
        {
        }
    }

    public class FastDefaultJob : BackgroundJob
    {
        public override void Perform()
        {
        }
    }

    [Queue("critical")]
    public class ConsoleJob : BackgroundJob
    {
        private static readonly Random _random = new Random();

        public int Number { get; set; }

        [Queue("critical")]
        public void Execute(string name)
        {
            Console.WriteLine("Hello, {0}!", name);
        }

        public override void Perform()
        {
            int time;
            lock (_random)
            {
                time = _random.Next(10);
            }

            if (time < 5)
            {
                throw new Exception();
            }

            Thread.Sleep(TimeSpan.FromSeconds(5 + time));
            Console.WriteLine("Finished task: " + Number);
        }
    }

    [Retry(Attempts = 0)]
    public class ErrorJob : BackgroundJob
    {
        public override void Perform()
        {
            Console.WriteLine("Beginning error task...");
            throw new InvalidOperationException(null, new FileLoadException());
        }
    }

    [Queue("critical"), Recurring(intervalInSeconds: 30)]
    public class RecurringJob : BackgroundJob
    {
        public override void Perform()
        {
            Console.WriteLine("Performing recurring task...");
        }
    }

    public static class Program
    {
        public static void Main()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            
            RedisFactory.Db = 3;

            GlobalJobFilters.Filters.Add(new HistoryStatisticsAttribute(), 20);
            GlobalJobFilters.Filters.Add(new RetryAttribute());

            using (var server = new BackgroundJobServer("critical", "default"))
            {
                server.Start();

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
                                Perform.Async<ConsoleJob>(new { Number = count++ });
                            }
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("console", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(8));
                            for (var i = 0; i < workCount; i++)
                            {
                                Perform.Async(() => Console.WriteLine("Hello, {0}!", "world"));
                                Perform.Async<ConsoleJob>(x => x.Execute("world"));
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
                            Perform.Async<ErrorJob>(new { ArticleId = 2, Product = "Casio Privia PX-850" });
                        }
                    }

                    if (command.StartsWith("in", StringComparison.OrdinalIgnoreCase))
                    {
                        var seconds = int.Parse(command.Substring(2));
                        Perform.In<ConsoleJob>(TimeSpan.FromSeconds(seconds), new { Number = count++ });
                    }

                    if (command.StartsWith("recurring", StringComparison.OrdinalIgnoreCase))
                    {
                        Perform.Async<RecurringJob>();
                        Console.WriteLine("Recurring job added");
                    }

                    if (command.StartsWith("fast", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(5));
                            Parallel.For(0, workCount, i =>
                                {
                                    var type = i % 2 == 0 ? typeof (FastCriticalJob) : typeof (FastDefaultJob);
                                    Perform.Async(type, new { Number = i });
                                });
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
