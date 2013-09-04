using System;
using HangFire.Web;

[assembly: WebActivatorEx.PostApplicationStartMethod(
    typeof($rootnamespace$.HangFireConfig), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethod(
    typeof($rootnamespace$.HangFireConfig), "Stop")]

namespace $rootnamespace$
{
    public class HangFireConfig
    {
        private static HangFireAspNetServer _server;

        public static void Start()
        {
            // If you have custom Redis installation, use the
            // following method to configure HangFire:
            // HangFireConfiguration.Configure(x => { /* ... */ });

            _server = new HangFireAspNetServer
            {
                ServerName = Environment.MachineName,
                QueueName = "default",
                Concurrency = Environment.ProcessorCount * 2
            };

            _server.Start();
        }

        public static void Stop()
        {
            _server.Dispose();
        }
    }
}