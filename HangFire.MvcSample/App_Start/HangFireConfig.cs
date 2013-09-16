using System;
using System.Web.Mvc;
using HangFire.Web;

[assembly: WebActivatorEx.PostApplicationStartMethod(
    typeof(HangFire.MvcSample.HangFireConfig), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethod(
    typeof(HangFire.MvcSample.HangFireConfig), "Stop")]

namespace HangFire.MvcSample
{
    public class HangFireConfig
    {
        private static HangFireAspNetServer _server;

        public static void Start()
        {
            // If you have custom Redis installation, use the
            // following method to configure HangFire:
            HangFireConfiguration.Configure(x => { x.RedisDb = 3; });
            
            _server = new HangFireAspNetServer
            {
                ServerName = Environment.MachineName,
                QueueName = "default",
                Concurrency = Environment.ProcessorCount * 2
            };

            //_server.Start();
        }

        public static void Stop()
        {
            _server.Dispose();
        }
    }
}