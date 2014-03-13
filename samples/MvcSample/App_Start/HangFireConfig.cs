using HangFire;
using HangFire.Redis;
using HangFire.SqlServer;
using HangFire.Storage;
using HangFire.Web;

[assembly: WebActivatorEx.PostApplicationStartMethod(
    typeof(MvcSample.HangFireConfig), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethod(
    typeof(MvcSample.HangFireConfig), "Stop")]

namespace MvcSample
{
    public class HangFireConfig
    {
        private static AspNetBackgroundJobServer _server;

        public static void Start()
        {
            // If you have custom Redis installation, use the
            // following method to configure HangFire:
            //JobStorage.SetCurrent(new RedisJobStorage("localhost:6379", 3));
            JobStorage.SetCurrent(new SqlServerStorage(@"Server=.\sqlexpress;Database=HangFire.SqlServer.Tests;Trusted_Connection=True;"));

            _server = new AspNetBackgroundJobServer();
            //_server.Start();
        }

        public static void Stop()
        {
            _server.Stop();
        }
    }
}