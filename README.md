HangFire
========

Background job processing for ASP.NET applications. 

Install
-------

You can install HangFile using NuGet Package Manager Console.
```
Install-Package HangFire
```

Use
---

First, you need to create a worker. Worker is a class that contains the code that will be executed in the background.

```cs
// ConsoleWorker.cs
public class ConsoleWorker : Worker
{
  public override void Process()
  {
    Console.WriteLine("Hello, " + Args["Word"]);
  }
}
```

Second, you need to send a job to the worker. The job will be added to the queue.

```cs
Perform.Async<ConsoleWorker>(new { Word = "world" });
```
