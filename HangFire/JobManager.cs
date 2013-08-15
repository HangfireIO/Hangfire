using System;
using System.IO;
using System.Threading;

using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire
{
    public class JobManager
    {
        private readonly Thread _managerThread;
        private readonly JobDispatcherPool _pool;

        private readonly TimeSpan _reconnectTimeout = TimeSpan.FromSeconds(5);

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        public JobManager(int concurrency)
        {
            _managerThread = new Thread(Work)
                {
                    Name = "HangFire.Manager", 
                    IsBackground = true
                };

            _pool = new JobDispatcherPool(concurrency);
        }

        private void Work()
        {
            var blockingClient = new RedisClient();
            
            while (true)
            {
                var dispatcher = _pool.TakeFree();

                try
                {
                    var redis = blockingClient.Connection;
                    var job = redis.BlockingDequeueItemFromList("hangfire:queue:default", null);

                    dispatcher.Process(job);
                }
                catch (IOException ex)
                {
                    _logger.Error("Exception occured while fetching the job.", ex);
                    Thread.Sleep(_reconnectTimeout);
                    blockingClient.Reconnect();
                }
                catch (RedisException)
                {
                    Thread.Sleep(_reconnectTimeout);
                    blockingClient.Reconnect();
                }
            }
        }

        public void Start()
        {
            _managerThread.Start();
        }
    }
}