using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HangFire.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHangfire(x =>
                    {
                        x.UseSqlServerStorage("Server=tcp:cklmqvce6u.database.windows.net,1433;Database=HANGFIRE;User ID=truetech@cklmqvce6u;Password=MacP!@101214MacP;Trusted_Connection=False;Encrypt=True;",
                            new SqlServerStorageOptions()
                            {
                                QueuePollInterval = TimeSpan.FromSeconds(15),
                                DisableGlobalLocks = true,
                                UsePageLocksOnDequeue = false
                            });

                        x.UseDefaultActivator();
                    });


                    //JobStorage.Current = redisStorage;

                    services.AddHangfireServer(options =>
                    {
                        options.Queues = new[] { $"job_110" };
                    });

                    //Scheduler.Schedule();
                });
    }
}
