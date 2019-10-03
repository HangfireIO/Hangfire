using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace ConsoleSample
{
    public class Services
    {
        private static readonly Random Rand = new Random();

        public async Task EmptyDefault()
        {
        }

        public async Task Async(CancellationToken cancellationToken)
        {
            await Task.Yield();
            await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
        }

        [Queue("critical")]
        public async Task EmptyCritical()
        {
        }

        [AutomaticRetry(Attempts = 0), LatencyTimeout(30)]
        public async Task Error()
        {
            Console.WriteLine("Beginning error task...");
            throw new InvalidOperationException(null, new FileLoadException());
        }

        [Queue("critical")]
        public async Task Random(int number)
        {
            int time;
            lock (Rand)
            {
                time = Rand.Next(10);
            }

            if (time < 5)
            {
                throw new Exception();
            }

            Thread.Sleep(TimeSpan.FromSeconds(5 + time));
            Console.WriteLine("Finished task: " + number);
        }

        public async Task Cancelable(int iterationCount, IJobCancellationToken token)
        {
            try
            {
                for (var i = 1; i <= iterationCount; i++)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Performing step {0} of {1}...", i, iterationCount);

                    token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancellation requested, exiting...");
                throw;
            }
        }

        [DisplayName("Name: {0}")]
        public async Task Args(string name, int authorId, DateTime createdAt)
        {
            Console.WriteLine($"{name}, {authorId}, {createdAt}");
        }

        public async Task Custom(int id, string[] values, CustomObject objects, DayOfWeek dayOfWeek)
        {
        }

        public async Task FullArgs(
            bool b,
            int i,
            char c,
            DayOfWeek e,
            string s,
            TimeSpan t,
            DateTime d,
            CustomObject o,
            string[] sa,
            int[] ia,
            long[] ea,
            object[] na,
            List<string> sl)
        {
        }

        public async Task LongRunning(IJobCancellationToken token)
        {
            token.ShutdownToken.Wait(TimeSpan.FromMinutes(30));
            token.ShutdownToken.ThrowIfCancellationRequested();
            //Thread.Sleep(TimeSpan.FromMinutes(10));
        }

        public class CustomObject
        {
            public int Id { get; set; }
            public CustomObject[] Children { get; set; }
        }

        public async Task Write(char character)
        {
            Console.Write(character);
        }

        public async Task WriteBlankLine()
        {
            Console.WriteLine();
        }

        [IdempotentCompletion]
        public static async Task <IState> WriteLine(string value)
        {
            Console.WriteLine(value);
            return new AwaitingState("asfafs", new EnqueuedState("criticalll"));
        }
    }
}