@redis
Feature: Client

Background:
    Given the following job type:
          """
          public class TestJob : BackgroundJob
          {
              public int ArticleId { get; set; }
              public string Author { get; set; }

              public override void Perform()
              {
              }
          }          
          """

# As a Developer, I should be able to enqueue jobs of the needed types with the
# ability to specify the optional arguments.

Scenario: the `Perform.Async<TJob>()` method should enqueue a job of the given type
     When I call the `Perform.Async<TestJob>()`
     Then the argumentless 'TestJob' should be added to the default queue

Scenario: the `Perform.Async<TJob>(object args)` method should enqueue job with the given arguments
     When I call the `Perform.Async<TestJob>(new { ArticleId = 3, Author = "odinserj" })`
     Then the 'TestJob' should be added to the default queue with the following arguments:
          | Name      | Value    |
          | ArticleId | 3        |
          | Author    | odinserj |

Scenario: the `Perform.Async(Type type)` method should enqueue a job of the given type
     When I call the `Perform.Async(typeof(TestJob))`
     Then the argumentless 'TestJob' should be added to the default queue

Scenario: Passing the null type argument to the `Perform.Async(Type type)` method should cause exception
     When I call the `Perform.Async(null)`
     Then a 'System.ArgumentNullException' should be thrown

Scenario: the `Perform.Async(Type type, object args)` method should enqueue job of the given type
     When I call the `Perform.Async(typeof(TestJob), new { ArticleId = 3 })`
     Then the 'TestJob' should be added to the default queue with the following arguments:
          | Name      | Value |
          | ArticleId | 3     |

Scenario: Passing the null type argument to the `Perform.Async(Type type, object args)` method should cause exception
     When I call the `Perform.Async(null, new { ArticleId = 3 })`
     Then a 'System.ArgumentNullException' should be thrown

# As a Developer, I should be able to specify the queue for a job type
# via the QueueAttribute class.

Scenario: the `Perform.Async<TJob>()` method should enqueue a job to its actual queue
    Given the following job type:
          """
          [Queue("critical")]
          public class CriticalQueueJob : BackgroundJob
          {
              public override void Perform()
              {
              }
          }
          """
     When I call the `Perform.Async<CriticalQueueJob>()`
     Then the argumentless 'CriticalQueueJob' should be added to the 'critical' queue

Scenario: the queue name should contain only lowercase letters, digits and underscores
    Given the following job type:
          """
          [Queue(" $InvalidQueue")]
          public class InvalidQueueJob : BackgroundJob
          {
              public override void Perform()
              {
              }
          }
          """
     When I call the `Perform.Async<InvalidQueueJob>()`
     Then a 'System.InvalidOperationException' should be thrown

Scenario: if the QueueAttribute contains an empty or null string, then the actual queue should be the default queue
    Given the following job type:
          """
          [Queue("")]
          public class EmptyQueueJob : BackgroundJob
          {
              public override void Perform()
              {
              }
          }
          """
     When I call the `Perform.Async<EmptyQueueJob>()`
     Then the argumentless 'EmptyQueueJob' should be added to the default queue

# As a Developer, I should be able to schedule jobs of the needed types with the
# ability to specify the optional arguments.

Scenario: the `Perform.In<TestJob>(TimeSpan delay)` method should schedule a job of the given type
     When I call the `Perform.In<TestJob>(TimeSpan.FromDays(1))`
     Then the argumentless 'TestJob' should be scheduled for tomorrow

Scenario: the `Perform.In<TestJob>(TimeSpan delay, object args)` method should schedule a job of the given type
     When I call the `Perform.In<TestJob>(TimeSpan.FromDays(1), new { ArticleId = 3 })`
     Then the 'TestJob' should be scheduled for tomorrow with the following arguments:
          | Name      | Value |
          | ArticleId | 3     |

Scenario: the `Perform.In(TimeSpan delay, Type type)` method should schedule a job of the given type
     When I call the `Perform.In(TimeSpan.FromDays(1), typeof(TestJob))`
     Then the argumentless 'TestJob' should be scheduled for tomorrow

Scenario: The `Perform.In(TimeSpan delay, Type type, object args)` method should schedule a job of the given type
     When I call the `Perform.In(TimeSpan.FromDays(1), typeof(TestJob), new { ArticleId = 3 })`
     Then the 'TestJob' should be scheduled for tomorrow with the following arguments:
          | Name      | Value |
          | ArticleId | 3     |
