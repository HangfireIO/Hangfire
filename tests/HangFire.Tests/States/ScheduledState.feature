@redis
Feature: Scheduled state

Background:
    Given a job
      And the Scheduled state with the date set to tomorrow

Scenario: After applying, it should add the job to the schedule at tomorrow
     When I apply it
     Then the schedule should contain the job that will be enqueued tomorrow

Scenario: After unapplying, it should remove the job from the schedule
     When I apply it
      And after I unapply it
     Then the schedule should not contain the job
