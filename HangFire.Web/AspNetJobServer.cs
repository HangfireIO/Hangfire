using System;
using System.Web.Hosting;

namespace HangFire.Web
{
    /// <summary>
    /// Represents the HangFire server that designed specifically to
    /// work within the ASP.NET application.
    /// </summary>
    public class AspNetJobServer : IRegisteredObject, IDisposable
    {
        private JobServer _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetJobServer"/>.
        /// </summary>
        public AspNetJobServer()
        {
            ServerName = Environment.MachineName;
            Concurrency = Environment.ProcessorCount * 2;
            QueueName = "default";
            PollInterval = TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// Gets or sets the server name. It should be unique across
        /// all server instances.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the queue to listen.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets maximum amount of jobs that being processed in parallel.
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// Gets or sets the poll interval for scheduled jobs.
        /// </summary>
        public TimeSpan PollInterval { get; set; }

        /// <summary>
        /// Get or sets an instance of the <see cref="HangFireJobActivator"/> class
        /// that will be used to instantinate jobs.
        /// </summary>
        public HangFireJobActivator JobActivator { get; set; }

        /// <summary>
        /// Starts the server and places it in the list of registered
        /// objects in the application. 
        /// </summary>
        public void Start()
        {
            HostingEnvironment.RegisterObject(this);
            _manager = new JobServer(ServerName, QueueName, Concurrency, PollInterval, JobActivator);
        }

        /// <summary>
        /// Disposes the server and removes it from the list of registered
        /// objects in the application.
        /// </summary>
        public void Dispose()
        {
            if (_manager != null)
            {
                _manager.Dispose();
                _manager = null;
                HostingEnvironment.UnregisterObject(this);
            }
        }

        void IRegisteredObject.Stop(bool immediate)
        {
            Dispose();
        }
    }
}
