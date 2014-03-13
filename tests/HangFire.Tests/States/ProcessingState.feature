@redis
Feature: Processing state

Background:
    Given a job
      And the Processing state

Scenario: State name should be 'Processing'
     Then the state name should be equal to 'Processing'

Scenario: It should have the correct properties set
     Then properties table should contain the following items:
          | Name       | Value              |
          | StartedAt  | <UtcNow timestamp> |
          | ServerName | TestServer         |

Scenario: After applying, it should add the job to the processing set
     When I apply it
     Then the processing set should contain the job
      And processing timestamp should be set to UtcNow

Scenario: After unapplying, it should remove the job from the processing set
     When I apply it
      And after I unapply it
     Then the processing set should not contain the job