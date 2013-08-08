using System;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;

namespace HangFire.Hosts
{
    public class ConsoleWorker : Worker
    {
        public override void Perform()
        {
            Console.WriteLine("Running task: " + Args["Number"]);
        }
    }

    public class ErrorWorker : Worker
    {
        public override void Perform()
        {
            throw new InvalidOperationException("Error!");
        }
    }

    public static class Program
    {
        public static void Main()
        {
            const int concurrency = 100;
            LogManager.LogFactory = new ConsoleLogFactory();

            var manager = new Manager(concurrency);
            manager.Start();
            Console.WriteLine("HangFire Server has been started. Press Ctrl+C to exit...");

            var count = 1;

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

                if (command.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                {
                    var workCount = int.Parse(command.Substring(6));
                    for (var i = 0; i < workCount; i++)
                    {
                        Perform.Async<ErrorWorker>();
                    }
                }
            }
        }
    }
}
