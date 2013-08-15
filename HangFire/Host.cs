using System;
using System.Linq;
using System.Threading;

using BookSleeve;

using ServiceStack.Logging;

namespace HangFire
{
    public class Host
    {
        private readonly Thread _managerThread;
        private readonly JobDispatcherPool _pool;

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        public Host(int concurrency)
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
                    var redis = blockingClient.GetConnection();
                    var result = redis.Lists.BlockingRemoveLastString(0, new[] { "hangfire:queue:default" }, 0);
                    result.Wait();
                    var job = result.Result.Item2;

                    dispatcher.Process(job);
                }
                catch (RedisException ex)
                {
                    _logger.Error("Во время извлечения следующей задачи возникло исключение.", ex);
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        public void Start()
        {
            _managerThread.Start();
        }
    }
}