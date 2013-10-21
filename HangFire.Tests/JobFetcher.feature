@redis
Feature: JobFetcher

Scenario: Fetcher returns job id when it dequeues a job
    Given an enqueued job
      And the fetcher listening the queue
     When it dequeues a job
     Then the fetcher returns the job

Scenario: Fetcher removes the dequeued job from the queue and adds it to the dequeued list
    Given an enqueued job
      And the fetcher listening the queue
     When it dequeues a job
     Then the queue does not contain the job anymore
      But the dequeued jobs list contains it

Scenario: Fetcher returns null when it tries to dequeue a job from an empty queue
    Given an empty queue
      And the fetcher listening the queue
     When it dequeues a job
     Then the fetcher does not return any job

Scenario: Fetcher dequeues jobs in the FIFO order
    Given the 'first' job in the queue
      And the 'second' job in the queue
      And the fetcher listening the queue
     When it dequeues a job for the first time
     Then the fetcher returns the 'first' job
     When it dequeues a job for the second time
     Then the fetcher returns the 'second' job

Scenario: Fetcher listens only specified queue
    Given a job in the 'another' queue
     And the fetcher listening the 'default' queue
     When it dequeues a job
     Then the fetcher does not return any job

Scenario: Fetcher sets the 'fetched' flag when it dequeues a job
    Given an enqueued job
      And the fetcher listening the queue
     When it dequeues a job
     Then the job has the 'fetched' flag set