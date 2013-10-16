using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobManager
    {
        private readonly ServerContext _context;
        private readonly Thread _managerThread;

        private readonly IRedisClient _redis = RedisFactory.Create();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ILog _logger = LogManager.GetLogger(typeof (JobManager));

        private WorkerPool _pool;
        private JobFetcher _fetcher;

        private bool _stopSent;
        
        public JobManager(ServerContext context)
        {
            _context = context;

            _managerThread = new Thread(Work)
                {
                    Name = typeof(JobManager).Name,
                    IsBackground = true
                };
            _managerThread.Start();
        }

        public void SendStop()
        {
            _stopSent = true;

            _cts.Cancel();
            _pool.SendStop();
        }

        public void Dispose()
        {
            if (!_stopSent)
            {
                SendStop();
            }

            if (_pool != null)
            {
                _pool.Dispose();
                _pool = null;
            }

            _managerThread.Join();
            
            _redis.Dispose();
            _cts.Dispose();
        }

        private void Work()
        {
            try
            {
                _pool = new WorkerPool(_context);
                _fetcher = new JobFetcher(_redis, _context.Queues);

                while (true)
                {
                    var worker = _pool.TakeFree(_cts.Token);

                    JobPayload jobId;

                    do
                    {
                        jobId = _fetcher.DequeueJob();
                        if (jobId == null)
                        {
                            _cts.Token.ThrowIfCancellationRequested();
                        }
                    } while (jobId == null);

                    worker.Process(jobId);
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: log it
            }
            catch (Exception ex)
            {
                _logger.Fatal(
                    String.Format(
                        "Unexpected exception caught. Jobs from the queue '{0}' will not be processed by this server.", 
                        _context.Queues.First()),
                    ex);
            }
        }
    }
}
