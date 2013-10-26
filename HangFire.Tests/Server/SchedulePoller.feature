@redis
Feature: SchedulePoller

    As an Administrator (or a Developer), I want the scheduled 
    jobs to added to their actual queues when their time has come.

Scenario: Scheduled jobs are being enqueued on their actual queues
    Given a scheduled job
     When the poller runs
     Then the job becomes Enqueued
      And the schedule does not contain it anymore
      But the queue contains the job
      And schedule poller returns 'true'

Scenario: Future jobs are not being enqueued
    Given a future job
     When the poller runs
     Then the job remains to be in the Scheduled state
      And the schedule contains the job
      And the queue does not contain the job
      And schedule poller returns 'false'

Scenario: Poller will enqueue only jobs in the Scheduled state
    Given a scheduled job
      And its state is Succeeded
     When the poller runs
     Then the job remains to be in the Succeeded state
      And the queue does not contain the job

Scenario: Returns false when there are no jobs in the schedule
     When the poller runs
     Then schedule poller returns 'false'

Scenario: Poller move the job to the Failed state when it could not find its type
    Given a scheduled job of the 'NonExisting' type
     When the poller runs
     Then the job moved to the Failed state

Scenario: Poller will fail only jobs in the Scheduled state
    Given a scheduled job of the 'NonExisting' type
      And its state is Succeeded
     When the poller runs
     Then the job remains to be in the Succeeded state