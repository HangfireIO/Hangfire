using System;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;

namespace HangFire.Hosts
{
    [Queue("qqq")]
    public class ConsoleWorker : Worker
    {
        private static readonly Random _random = new Random();

        public override void Perform()
        {
            int time;
            lock (_random)
            {
                time = _random.Next(10);
            }

            if (time < 5)
            {
                throw new Exception("Unknown error");
            }

            Thread.Sleep(TimeSpan.FromSeconds(5 + time));
            Console.WriteLine("Finished task: " + Args["Number"]);
        }
    }

    [Queue("qqq")]
    public class ErrorWorker : Worker
    {
        public override void Perform()
        {
            Console.WriteLine("Beginning error task...");
            throw new InvalidOperationException("Error!");
        }
    }

    public static class Program
    {
        public static void Main()
        {
            int concurrency = Environment.ProcessorCount * 20;
            LogManager.LogFactory = new ConsoleLogFactory();

            HangFireConfiguration.Configure(
                x =>
                {
                    x.RedisPort = 6379;
                    x.AddFilter(new BasicRetryFilter());
                });

            using (var server = new HangFireServer("hijack!", "qqq", concurrency, TimeSpan.FromSeconds(15)))
            {
                Console.WriteLine("HangFire Server has been started. Press Ctrl+C to exit...");

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
                                HangFireClient.PerformAsync<ConsoleWorker>(new { Number = count++ });
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
                            HangFireClient.PerformAsync<ErrorWorker>();
                        }
                    }

                    if (command.StartsWith("in", StringComparison.OrdinalIgnoreCase))
                    {
                        var seconds = int.Parse(command.Substring(2));
                        HangFireClient.PerformIn<ConsoleWorker>(TimeSpan.FromSeconds(seconds), new { Number = count++ });
                    }
                }
            }
        }
    }
}
