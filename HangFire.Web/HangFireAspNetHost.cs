using System;
using System.Web.Hosting;

namespace HangFire.Web
{
    public class HangFireAspNetHost : IRegisteredObject
    {
        private static readonly HangFireAspNetHost _instance = new HangFireAspNetHost();

        static HangFireAspNetHost()
        {
        }

        public static HangFireAspNetHost Instance
        {
            get { return _instance; }
        }

        private JobManager _manager;

        public void Start()
        {
            _manager = new JobManager(
                Environment.MachineName,
                Environment.ProcessorCount * 2,
                "default");
            HostingEnvironment.RegisterObject(this);
        }

        public void Stop(bool immediate)
        {
            _manager.Dispose();
            HostingEnvironment.UnregisterObject(this);
        }
    }
}
