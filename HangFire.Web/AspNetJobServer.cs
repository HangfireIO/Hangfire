using System.Web.Hosting;

namespace HangFire.Web
{
    /// <summary>
    /// Represents the HangFire server that designed specifically to
    /// work within the ASP.NET application.
    /// </summary>
    public class AspNetBackgroundJobServer : BackgroundJobServer, IRegisteredObject
    {
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
