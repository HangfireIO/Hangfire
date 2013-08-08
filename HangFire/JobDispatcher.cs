using System.Threading;

namespace HangFire
{
    internal class JobDispatcher
    {
        private readonly Manager _manager;
        private readonly JobProcessor _processor = new JobProcessor();
        private Thread _thread;
        private readonly ManualResetEventSlim _jobIsReady 
            = new ManualResetEventSlim(false);

        private volatile string _currentJob;

        public JobDispatcher(Manager manager)
        {
            _manager = manager;
        }

        public void Start()
        {
            _thread = new Thread(DoWork);
            _thread.Start();
        }

        public void Process(string serializedJob)
        {
            // TODO: Possible race condition
            _currentJob = serializedJob;
            _jobIsReady.Set();
        }

        private void DoWork()
        {
            while (true)
            {
                _manager.NotifyFreeProcessor(this);
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
