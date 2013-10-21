@redis
Feature: Scheduled state

Background:
    Given a job
      And the Scheduled state with the date set to tomorrow

Scenario: State name is 'Scheduled'
     Then the state name is equal to 'Scheduled'

Scenario: It has the correct properties set
     Then properties table contains the following items:
          | Name        | Value                |
          | ScheduledAt | <UtcNow timestamp>   |
          | EnqueueAt   | <Tomorrow timestamp> |

Scenario: After applying, it should add the job to the schedule at tomorrow
     When I apply it
     Then the schedule contains the job that will be enqueued tomorrow

Scenario: After unapplying, it should remove the job from the schedule
     When I apply it
      And after I unapply it
     Then the schedule does not contain the job
