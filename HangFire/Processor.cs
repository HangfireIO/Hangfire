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
            while (true)
            {
                _manager.NotifyFreeProcessor(this);
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
                    // Возможные причины:
                    // 1. Произошла ошибка при десериализации.
                    //    - Записан какой-то мусор.
                    //    - Старый формат класса Job.
                    // 2. Ошибка при создании экземпляра нужного Worker'а.
                    //    - Невозможно найти нужный класс
                    //    - Невозможно сконструировать нужный класс
                    // 3. Произошла необработанная ошибка в пользовательском коде Worker'а.
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
