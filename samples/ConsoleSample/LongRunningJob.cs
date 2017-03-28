using System;
using System.Threading;

namespace ConsoleSample
{
   public class LongRunningJob
    {
        public void RunFor2Min()
        {
            Console.WriteLine(@"Starting Job timer");
            Thread.Sleep(120000);
            Console.WriteLine(@"Stoping Job timer");
        }
    }
}
