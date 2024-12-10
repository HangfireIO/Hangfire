using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;

namespace ConsoleSample
{
    public class Services
    {
        private static readonly Random Rand = new Random();

        public async Task<int> WriteIndex([FromResult] int? index)
        {
            if (index == null) throw new ArgumentNullException(nameof(index));

            Console.Write("Hello, world!\r\n"[index.Value]);
            await Task.Yield();
            return index.Value + 1;
        }

        public async Task EmptyDefault()
        {
            await Task.Yield();
        }

        public async Task Async(CancellationToken cancellationToken)
        {
            await Task.Yield();
            await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
        }

        [Obsolete("Please use EmptyDefault method instead with `critical` queue directly")]
        [Queue("critical")]
        public async Task EmptyCritical()
        {
            await Task.Yield();
        }

        [AutomaticRetry(Attempts = 0), LatencyTimeout(30)]
        public async Task Error()
        {
            await Task.Yield();
            Console.WriteLine("Beginning error task...");
            throw new InvalidOperationException(null, new FileLoadException());
        }

        [Queue("critical")]
        public async Task<int> Random([FromResult] int? number)
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

            await Task.Delay(TimeSpan.FromSeconds(5 + time));
            Console.WriteLine("Finished task: " + number);
            return time;
        }

        public async Task Cancelable(int iterationCount, IJobCancellationToken token)
        {
            try
            {
                for (var i = 1; i <= iterationCount; i++)
                {
                    await Task.Delay(1000);
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
            await Task.Yield();
            Console.WriteLine($"{name}, {authorId}, {createdAt}");
        }

        public async Task Custom(int id, string[] values, CustomObject objects, DayOfWeek dayOfWeek)
        {
            await Task.Yield();
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
            await Task.Yield();
        }

        public async Task LongRunning(IJobCancellationToken token)
        {
            await Task.Delay(TimeSpan.FromMinutes(30), token.ShutdownToken);
        }

        public class CustomObject
        {
            public int Id { get; set; }
            public CustomObject[] Children { get; set; }
        }

        public async Task Write(char character)
        {
            await Task.Yield();
            Console.Write(character);
        }

        public async Task WriteBlankLine()
        {
            await Task.Yield();
            Console.WriteLine();
        }

        [IdempotentCompletion]
        public static async Task <IState> WriteLine(string value)
        {
            await Task.Yield();
            Console.WriteLine(value);
            return new AwaitingState("asfafs", new EnqueuedState("criticalll"));
        }
    }
}