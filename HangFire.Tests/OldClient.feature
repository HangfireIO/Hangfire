@redis
Feature: Old Client

    As a Developer, I want to define and create jobs.

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

Scenario: When one or more of the job arguments can not be converted using the custom TypeConverter, an exception should be raised
    Given the custom types:
          """ 
          [TypeConverter(typeof(CustomTypeConverter))
          public class CustomType {}

          public class CustomTypeConverter : TypeConverter
          {
              public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
              {
                  throw new NotSupportedException();
              }
          }
          """
     When I call the `Perform.Async<TestJob>(new { Author = new CustomType() })`
     Then a 'System.InvalidOperationException' should be thrown

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
     Then a CreateJobFailedException should be thrown

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