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
	 Then the fetcher returns null

Scenario: Fetcher listens only specified queue
    Given a job in the 'another' queue
	  And the fetcher listening the 'default' queue
	 When it dequeues a job
	 Then the fetcher returns null

Scenario: Fetcher sets the 'fetched' flag when it dequeues a job
    Given an enqueued job
	  And the fetcher listening the queue
	 When it dequeues a job
	 Then the job has the 'fetched' flag set