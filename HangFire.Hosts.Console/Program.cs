using System;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;

namespace HangFire.Hosts
{
    public class ConsoleWorker : Worker
    {
        public static int Processed = 0;

        public override void Perform()
        {
            Console.WriteLine("Running task: " + Args["Number"]);
            Interlocked.Add(ref Processed, 1);
            //Thread.Sleep(100);
        }
    }

    public static class Program
    {
        public static void Main()
        {
            const int concurrency = 100;
            LogManager.LogFactory = new ConsoleLogFactory();

            using (var manager = new Manager(concurrency))
            {
                manager.Start();
                Console.WriteLine("HangFire Server has been started. Press Ctrl+C to exit...");

                var count = 1;

                //var timer = new Timer(state => Console.WriteLine("Jobs processed: " + ConsoleWorker.Processed));
                //timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

                while (true)
                {
                    var command = Console.ReadLine();

                    if (command == null ||
                        command.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (command.StartsWith("add", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(4));
                        for (var i = 0; i < workCount; i++)
                        {
                            Perform.Async<ConsoleWorker>(new { Number = count++ });
                        }
                    }
                }

                manager.Stop();
            }
        }
    }
}
