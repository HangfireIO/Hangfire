using System.Web.Hosting;

namespace HangFire.Web
{
    /// <summary>
    /// Represents the HangFire server that implements the
    /// <see cref="IRegisteredObject"/> interface. 
    /// </summary>
    public class AspNetBackgroundJobServer : BackgroundJobServer, IRegisteredObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetBackgroundJobServer"/>
        /// class with the number of workers and the list of queues that will 
        /// be processed by this instance of a server.
        /// </summary>
        /// <param name="workerCount">The number of workers.</param>
        /// <param name="queues">The list of queues that will be processed.</param>
        public AspNetBackgroundJobServer(int workerCount, params string[] queues)
            : base(workerCount, queues)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetBackgroundJobServer"/>
        /// class with the default number of workers and the specified list of
        /// queues that will be processed by this instance of a server.
        /// </summary>
        /// <param name="queues">The list of queues that will be processed.</param>
        public AspNetBackgroundJobServer(params string[] queues)
            : base(queues)
        {
        }

        /// <summary>
        /// Starts the server and places it in the list of registered
        /// objects in the application. 
        /// </summary>
        public override void Start()
        {
            base.Start();
            HostingEnvironment.RegisterObject(this);
        }

        /// <summary>
        /// Disposes the server and removes it from the list of registered
        /// objects in the application.
        /// </summary>
        public override bool Stop()
        {
            var wasStopped = base.Stop();
            if (wasStopped)
            {
                HostingEnvironment.UnregisterObject(this);
            }

            return wasStopped;
        }

        void IRegisteredObject.Stop(bool immediate)
        {
            Stop();
        }
    }
}
