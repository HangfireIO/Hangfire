@redis
Feature: Re-queueing of timed out jobs

    As an Administrator, I want the jobs to recovered automatically 
    after a server failure, which took them into the processing.

@checkpoint-1-1
Scenario: A job in the implicit 'Dequeued' state should be moved to the 'Checked' state
    Given a dequeued job
     When the watcher runs
     Then it should mark the job as 'checked'

@checkpoint-1-2
Scenario: Non-timed out job in the 'Checked' state should not be requeued
    Given a dequeued job
      And it was checked a millisecond ago
     When the watcher runs
     Then the dequeued jobs list should contain the job
      And the queue should not contain the job
      And the job should have the 'checked' flag set

@checkpoint-1-2 
Scenario: Timed job in the 'Checked' state should be requeued
   Given a dequeued job
      And it was checked a day ago
     When the watcher runs
     Then the queue should contain the job
      And the job should be removed from the dequeued list
      And the job should not have the 'checked' flag set

@checkpoint-2
Scenario: Timed out job by the 'checked' flag in the 'Fetched' state should not be re-queued
    Given a dequeued job
      And it was checked a day ago
      And it was fetched a millisecond ago
     When the watcher runs
     Then the dequeued jobs list should contain the job
      And the queue should not contain the job
      And the job should have the 'checked' flag set
      And the job should have the 'fetched' flag set

@checkpoint-2
Scenario: Timed out jobs in the 'Fetched' state should be re-queued
    Given a dequeued job
      And it was fetched a day ago
     When the watcher runs
     Then the queue should contain the job
      And the job should be removed from the dequeued list
      And the job should not have the 'checked' flag set
      And the job should not have the 'fetched' flag set

@checkpoint-3
Scenario: Job's state should be changed to the Enqueued when the job is being timed out after proceeding to the Processing state
    Given a dequeued job
      And its state is Processing
      And it was fetched a day ago
     When the watcher runs
     Then the job should be moved to the Enqueued state
      And it should be removed from the dequeued list

@checkpoint-4
Scenario: Timed out job in the Succeeded state should not move to the Enqueued state
    Given a dequeued job
      And its state is Succeeded
      And it was fetched a day ago
     When the watcher runs
     Then the job should be in the Succeeded state
      But it should be removed from the dequeued list

Scenario: Job should be enqueued on its actual queue after timing out
      And a dequeued job from the 'test' queue
      And it was fetched a day ago
     When the watcher runs
     Then the queue should contain the job