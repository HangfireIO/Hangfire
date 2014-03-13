@redis
Feature: Failed state

Background: 
    Given a job
      And the Failed state

Scenario: State name should be 'Failed'
     Then the state name should be equal to 'Failed'

Scenario: It should have the correct properties set
     Then properties table should contain the following items:
          | Name             | Value                            |
          | FailedAt         | <UtcNow timestamp>               |
          | ExceptionType    | System.InvalidOperationException |
          | ExceptionMessage | Hello                            |
          | ExceptionDetails | <Non-empty>                      |

Scenario: After applying it should add the job to the failed set
     When I apply it
     Then the job should be added to the failed set

Scenario: After unapplying it should remove the job from the failed set
     When I apply it
      And after I unapply it
     Then the job should be removed from the failed set