@redis
Feature: Enqueued state

Background: 
    Given a job
      And the Enqueued state with the 'test' value for the 'queue' argument

Scenario: State name is 'Enqueued'
     Then the state name is equal to 'Enqueued'

Scenario: It has the correct properties set
     Then properties table contains the following items:
          | Name       | Value              |
          | EnqueuedAt | <UtcNow timestamp> |
          | Queue      | test               |

Scenario: After applying, it should enqueue the job to the given queue
     When I apply it
     Then the 'test' queue contains the job
      And the 'test' queue was added to the queues set

Scenario: After unapplying, it should not remove the job from the queue
     When I apply it
      And after I unapply it
     Then the 'test' queue still contains the job