using System;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    internal class Processor
    {
        // TODO: change name corresponding to the processor name
        private readonly ILog _logger = LogManager.GetLogger(typeof (Processor));

        private readonly Manager _manager;
        private Thread _thread;
        private readonly ManualResetEventSlim _jobIsReady 
            = new ManualResetEventSlim(false);

        // TODO: does ManualResetEventSlim provides memory barrier?
        private string _currentJob;

        public Processor(Manager manager)
        {
            _manager = manager;
        }

        public void Start()
        {
            _thread = new Thread(DoWork);
            _thread.Start();
        }

        public void Stop()
        {
            
        }

        public void Wait()
        {
        }

        public void Process(string serializedJob)
        {
            _currentJob = serializedJob;
            _jobIsReady.Set();
        }

        private void DoWork()
        {
            _manager.NotifyFreeProcessor(this);

            while (true)
            {
                // TODO: handle manager stop.
                _jobIsReady.Wait();

                try
                {
                    // TODO: add deserialization exception handling. It does no sense to restart them.
                    var job = JsonHelper.Deserialize<Job>(_currentJob);

                    // TODO: handle activation errors. It does no sense to restart them.
                    var worker = Factory.CreateWorker(job.WorkerType);
                    worker.Args = job.Args;

                    // TODO: add user code exception handling. And restart it if possible.
                    worker.Perform();
                }
                catch (Exception ex)
                {
                    
                }
                finally
                {
                    _jobIsReady.Reset();
                    _manager.NotifyFreeProcessor(this);    
                }
            }
        }
    }
}
