@redis
Feature: Re-queueing of timed out jobs

Scenario: Not checked jobs at fail point #1 should be marked as checked
    Given a dequeued job
     When the watcher runs
     Then it marks the job as 'checked'

Scenario: Checked and not timed out jobs at fail point #1 should be leaved as is
    Given a dequeued job
      And it was checked a millisecond ago
     When the watcher runs
     Then the dequeued jobs list still contains the job
      And the queue does not contain the job
      And the job has the 'checked' flag set

Scenario: Checked and timed out jobs at fail point #1 should be re-queued
   Given a dequeued job
      And it was checked a day ago
     When the watcher runs
     Then the queue contains the job
      And the dequeued jobs list does not contain the job anymore
      And the job does not have the 'checked' flag set

Scenario: Only fetched flag value is being considered for the job's timeout after fail point #2
    Given a dequeued job
      And it was checked a day ago
      And it was fetched a millisecond ago
     When the watcher runs
     Then the dequeued jobs list still contains the job
      And the queue does not contain the job
      And the job has the 'checked' flag set
      And the job has the 'fetched' flag set

Scenario: Timed out jobs at fail point #2 should be re-queued
    Given a dequeued job
      And it was fetched a day ago
     When the watcher runs
     Then the queue contains the job
      And the dequeued jobs list does not contain the job anymore
      And the job does not have the 'checked' flag set
      And the job does not have the 'fetched' flag set

Scenario: Job's state is changed to the Enqueued when the job is being timed out after proceeding to the Processing state
    Given a dequeued job
      And it's state is Processing
      And it was fetched a day ago
     When the watcher runs
     Then the job moved to the Enqueued state
      And the dequeued jobs list does not contain the job anymore

Scenario: Timed out job in the Succeeded state does not move to the Enqueued state
    Given a dequeued job
      And it's state is Succeeded
      And it was fetched a day ago
     When the watcher runs
     Then the job remains to be in the Succeeded state
      But the dequeued jobs list does not contain the job anymore

Scenario: Job is being enqueued on it's actual queue after timing out
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
      And it's state is Succeeded
      And it was fetched a day ago
     When the watcher runs
     Then the job remains to be in the Succeeded state
      But the dequeued jobs list does not contain the job anymore