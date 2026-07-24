using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config =>
    config.UseMemoryStorage());
builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseHangfireDashboard();

RecurringJob.AddOrUpdate<MyJob>(
    "simple-job",
    job => job.Run(),
    Cron.Minutely());

app.Run();

public class MyJob
{
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public void Run()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        Console.WriteLine($"[{now}] [Thread {threadId}] Job started.");

        Thread.Sleep(10000); // Simula job lento

        now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        Console.WriteLine($"[{now}] [Thread {threadId}] Job completed.");
    }
}
