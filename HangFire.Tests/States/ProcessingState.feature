@redis
Feature: Processing state

Background:
    Given a job
      And the Processing state

Scenario: State name is 'Processing'
     Then the state name is equal to 'Processing'

Scenario: It has the correct properties set
     Then properties table contains the following items:
          | Name       | Value              |
          | StartedAt  | <UtcNow timestamp> |
          | ServerName | TestServer         |

Scenario: After applying, it should add the job to the processing set
     When I apply it
     Then the processing set contains the job
      And processing timestamp is set to UtcNow

Scenario: After unapplying, it should remove the job from the processing set
     When I apply it
      And after I unapply it
     Then the processing set does not contain the job