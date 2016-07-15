using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;

namespace ConsoleSample
{
    public class Services
    {
        private static readonly Random Rand = new Random();

        public void EmptyDefault()
        {
        }

        public async Task Async(CancellationToken cancellationToken)
        {
            await Task.Yield();
            await Task.Delay(TimeSpan.FromDays(1), cancellationToken);
        }

        [Queue("critical")]
        public void EmptyCritical()
        {
        }

        [AutomaticRetry(Attempts = 0), LatencyTimeout(30)]
        public void Error()
        {
            Console.WriteLine("Beginning error task...");
            throw new InvalidOperationException(null, new FileLoadException());
        }

        [Queue("critical")]
        public void Random(int number)
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

        public void Cancelable(int iterationCount, IJobCancellationToken token)
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
        public void Args(string name, int authorId, DateTime createdAt)
        {
            Console.WriteLine($"{name}, {authorId}, {createdAt}");
        }

        public void Custom(int id, string[] values, CustomObject objects, DayOfWeek dayOfWeek)
        {
        }

        public void FullArgs(
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

        public class CustomObject
        {
            public int Id { get; set; }
            public CustomObject[] Children { get; set; }
        }

        public void Write(char character)
        {
            Console.Write(character);
        }

        public void WriteBlankLine()
        {
            Console.WriteLine();
        }
    }
}