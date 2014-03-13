@redis
Feature: Enqueued state

Background: 
    Given a job
      And the Enqueued state

Scenario: State name should be 'Enqueued'
     Then the state name should be equal to 'Enqueued'

Scenario: It should have the correct properties set
     Then properties table should contain the following items:
          | Name       | Value              |
          | EnqueuedAt | <UtcNow timestamp> |
          | Queue      | default            |

Scenario: After applying, it should enqueue the job to the given queue
     When I apply it
     Then the 'default' queue should contain the job
      And the 'default' queue should be added to the queues set

Scenario: After unapplying, it should not remove the job from the queue
     When I apply it
      And after I unapply it
     Then the 'default' queue should contain the job