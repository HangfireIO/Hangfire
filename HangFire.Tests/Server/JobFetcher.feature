@redis
Feature: JobFetcher

    As a user, I expect that my enqueued jobs will be dequeued by the Server.

Scenario: Fetcher should return the job payload when it dequeues a job
    Given an enqueued job
      And the fetcher listening the queue
     When it dequeues a job
     Then the fetcher should return the payload

Scenario: Fetcher should remove the dequeued job from the queue and adds it to the dequeued list
    Given an enqueued job
      And the fetcher listening the queue
     When it dequeues a job
     Then the queue should not contain the job anymore
      But the dequeued jobs list should contain it

Scenario: Fetcher should not return any job when it tries to dequeue a job from an empty queue
    Given an empty queue
      And the fetcher listening the queue
     When it dequeues a job
     Then the fetcher should not return any job

Scenario: Fetcher should dequeue jobs in the FIFO order
    Given the 'first' job in the queue
      And the 'second' job in the queue
      And the fetcher listening the queue
     When it dequeues a job for the first time
     Then the fetcher should return the 'first' job
     When it dequeues a job for the second time
     Then the fetcher should return the 'second' job

Scenario: Fetcher should listen only specified queue
    Given a job in the 'another' queue
     And the fetcher listening the 'default' queue
     When it dequeues a job
     Then the fetcher should not return any job

Scenario: Fetcher should set the 'fetched' flag when it dequeues a job
    Given an enqueued job
      And the fetcher listening the queue
     When it dequeues a job
     Then the job should have the 'fetched' flag set