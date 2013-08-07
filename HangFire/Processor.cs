using System;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    internal class Processor
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof (Processor));

        private readonly Manager _manager;
        private Thread _thread;
        private readonly ManualResetEventSlim _jobIsReady 
            = new ManualResetEventSlim(false);

        private volatile string _currentJob;

        public Processor(Manager manager)
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
                    var job = JsonHelper.Deserialize<Job>(_currentJob);

                    using (var worker = Factory.CreateWorker(job.WorkerType))
                    {
                        worker.Args = job.Args;

                        // TODO: server middleware
                        worker.Perform();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        "Failed to process the job: unexpected exception caught. Job JSON:"
                        + Environment.NewLine
                        + _currentJob, 
                        ex);
                }
                finally
                {
                    _jobIsReady.Reset();
                }
            }
        }
    }
}
