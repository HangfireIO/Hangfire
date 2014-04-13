using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using HangFire.SqlServer;
using HangFire.Web;

[assembly: WebActivatorEx.PostApplicationStartMethod(
    typeof(HangFire.Sample.Highlighter.HangFireConfig), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethod(
    typeof(HangFire.Sample.Highlighter.HangFireConfig), "Stop")]

namespace HangFire.Sample.Highlighter
{
    public class HangFireConfig
    {
        private static AspNetBackgroundJobServer _server;

        public static void Start()
        {
            JobStorage.Current = new SqlServerStorage(
                ConfigurationManager.ConnectionStrings["HighlighterDb"].ConnectionString);

            _server = new AspNetBackgroundJobServer();
            _server.Start();
        }

        public static void Stop()
        {
            _server.Stop();
        }
    }
}