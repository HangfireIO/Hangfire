@redis
Feature: Re-queueing of timed out jobs

@checkpoint-1-1
Scenario: A job in the implicit 'Dequeued' state moved to the 'Checked' state
    Given a dequeued job
     When the watcher runs
     Then it marks the job as 'checked'

@checkpoint-1-2
Scenario: Non-timed out job in the 'Checked' state should not be requeued
    Given a dequeued job
      And it was checked a millisecond ago
     When the watcher runs
     Then the dequeued jobs list still contains the job
      And the queue does not contain the job
      And the job has the 'checked' flag set

@checkpoint-1-2 
Scenario: Timed job in the 'Checked' state should be requeued
   Given a dequeued job
      And it was checked a day ago
     When the watcher runs
     Then the queue contains the job
      And the dequeued jobs list does not contain the job anymore
      And the job does not have the 'checked' flag set

@checkpoint-2
Scenario: Timed out job by the 'checked' flag in the 'Fetched' state should not be re-queued
    Given a dequeued job
      And it was checked a day ago
      And it was fetched a millisecond ago
     When the watcher runs
     Then the dequeued jobs list still contains the job
      And the queue does not contain the job
      And the job has the 'checked' flag set
      And the job has the 'fetched' flag set

@checkpoint-2
Scenario: Timed out jobs in the 'Fetched' state should be re-queued
    Given a dequeued job
      And it was fetched a day ago
     When the watcher runs
     Then the queue contains the job
      And the dequeued jobs list does not contain the job anymore
      And the job does not have the 'checked' flag set
      And the job does not have the 'fetched' flag set

@checkpoint-3
Scenario: Job's state is changed to the Enqueued when the job is being timed out after proceeding to the Processing state
    Given a dequeued job
      And its state is Processing
      And it was fetched a day ago
     When the watcher runs
     Then the job moved to the Enqueued state
      And the dequeued jobs list does not contain the job anymore

@checkpoint-4
Scenario: Timed out job in the Succeeded state does not move to the Enqueued state
    Given a dequeued job
      And its state is Succeeded
      And it was fetched a day ago
     When the watcher runs
     Then the job remains to be in the Succeeded state
      But the dequeued jobs list does not contain the job anymore

Scenario: Job is being enqueued on its actual queue after timing out
      And a dequeued job from the 'test' queue
      And it was fetched a day ago
     When the watcher runs
     Then the queue contains the job

Scenario: When the server could not find the job's type, the job is moved to the Failed state
    Given a dequeued job of the 'NonExisting' type
      And it was fetched a day ago
     When the watcher runs
     Then the job moved to the Failed state
      And the dequeued jobs list does not contain the job anymore

Scenario: Succeeded job of non-existing type will not be moved to the failed state
    Given a dequeued job of the 'NonExisting' type
      And its state is Succeeded
      And it was fetched a day ago
     When the watcher runs
     Then the job remains to be in the Succeeded state
      But the dequeued jobs list does not contain the job anymore