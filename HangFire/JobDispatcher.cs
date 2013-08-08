using System;
using System.Threading;

namespace HangFire
{
    internal class JobDispatcher
    {
        private readonly JobDispatcherPool _pool;
        private readonly JobProcessor _processor = new JobProcessor();
        private Thread _thread;
        private readonly ManualResetEventSlim _jobIsReady 
            = new ManualResetEventSlim(false);

        private volatile string _currentJob;

        public JobDispatcher(JobDispatcherPool pool)
        {
            _pool = pool;
            
            _thread = new Thread(DoWork) { IsBackground = true };
            _thread.Start();
        }

        public void Process(string serializedJob)
        {
            _currentJob = serializedJob;
            _jobIsReady.Set();
        }

        private void DoWork()
        {
            while (true)
            {
                _pool.NotifyReady(this);
                _jobIsReady.Wait();

                try
                {
                    _processor.ProcessJob(_currentJob);
                }
                finally
                {
                    _jobIsReady.Reset();
                }
            }
        }
    }
}
