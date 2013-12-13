using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HangFire;
using HangFire.States;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;

namespace ConsoleSample
{
    public class Services
    {
        private static readonly Random _random = new Random();

        public void EmptyDefault()
        {
        }

        [Queue("critical")]
        public void EmptyCritical()
        {
        }

        [Retry(Attempts = 0)]
        public void Error()
        {
            Console.WriteLine("Beginning error task...");
            throw new InvalidOperationException(null, new FileLoadException());
        }

        [Queue("critical"), Recurring(intervalInSeconds: 30)]
        public void Recurring()
        {
            Console.WriteLine("Performing recurring task...");
        }

        [Queue("critical")]
        public void Random(int number)
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
            Console.WriteLine("Finished task: " + number);
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
            
            using (var server = new BackgroundJobServer(25, "critical", "default"))
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

                    if (command.StartsWith("in", StringComparison.OrdinalIgnoreCase))
                    {
                        var seconds = int.Parse(command.Substring(2));
                        var number = count++;
                        BackgroundJob.Schedule<Services>(x => x.Random(number), TimeSpan.FromSeconds(seconds));
                    }

                    if (command.StartsWith("recurring", StringComparison.OrdinalIgnoreCase))
                    {
                        BackgroundJob.Enqueue<Services>(x => x.Recurring());
                        Console.WriteLine("Recurring job added");
                    }

                    if (command.StartsWith("fast", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(5));
                            Parallel.For(0, workCount, i =>
                            {
                                if (i % 2 == 0)
                                {
                                    BackgroundJob.Enqueue<Services>(x => x.EmptyCritical());
                                }
                                else
                                {
                                    BackgroundJob.Enqueue<Services>(x => x.EmptyDefault());
                                }
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
