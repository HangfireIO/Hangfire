using HangFire;
using HangFire.Web;

[assembly: WebActivatorEx.PostApplicationStartMethod(
    typeof($rootnamespace$.HangFireConfig), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethod(
    typeof($rootnamespace$.HangFireConfig), "Stop")]

namespace $rootnamespace$
{
    public class HangFireConfig
    {
        private static AspNetBackgroundJobServer _server;

        public static void Start()
        {
            _server = new AspNetBackgroundJobServer();
            _server.Start();
        }

        public static void Stop()
        {
            _server.Stop();
        }
    }
}
