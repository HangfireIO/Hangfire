Feature: JobActivator

Scenario: Activator returns the job instance when its type has default constructor
    Given the following job type:
          """
          public class TestJob : BackgroundJob
          {
              public override void Perform()
              {
              }
          }
          """
     When I call the `Activate` method with the 'TestJob' type argument
     Then Activator should return an instance of the 'TestJob' type

Scenario: Activator throws an exception when the specified job type has no default constructor
    Given the following job type:
          """
          public class CustomConstructorJob : BackgroundJob
          {
              public CustomConstructorJob(string parameter)
              {
              }

              public override void Perform()
              {
              }
          }
          """
     When I call the `Activate` method with the 'CustomConstructorJob' type argument
     Then Activator throws a 'System.MissingMethodException'
