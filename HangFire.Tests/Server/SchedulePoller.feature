@redis
Feature: SchedulePoller

    As an Administrator (or a Developer), I want the scheduled 
    jobs to added to their actual queues when their time has come.

Scenario: Scheduled jobs should be enqueued on their actual queues
    Given a scheduled job
     When the poller runs
     Then the job should be in the Enqueued state
      And the schedule should not contain it anymore
      But the queue should contain the job
      And schedule poller should return 'true'

Scenario: Future jobs should not be enqueued
    Given a future job
     When the poller runs
     Then the job should be in the Scheduled state
      And the schedule should contain the job
      And the queue should not contain the job
      And schedule poller should return 'false'

Scenario: Poller should enqueue only jobs in the Scheduled state
    Given a scheduled job
      And its state is Succeeded
     When the poller runs
     Then the job should be in the Succeeded state
      And the queue should not contain the job

Scenario: Should return false when there are no jobs in the schedule
     When the poller runs
     Then schedule poller should return 'false'

Scenario: Poller move the job to the Failed state when it could not find its type
    Given a scheduled job of the 'NonExisting' type
     When the poller runs
     Then the job should be moved to the Failed state

Scenario: Poller will fail only jobs in the Scheduled state
    Given a scheduled job of the 'NonExisting' type
      And its state is Succeeded
     When the poller runs
     Then the job should be in the Succeeded state