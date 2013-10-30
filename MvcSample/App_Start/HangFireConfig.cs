using System;

using HangFire;
using HangFire.Server;
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
            RedisFactory.Db = 3;

            _server = new AspNetBackgroundJobServer();

            GlobalJobFilters.Filters.Add(new PreserveCultureAttribute());

            //_server.Start();
        }

        public static void Stop()
        {
            _server.Stop();
        }
    }
}