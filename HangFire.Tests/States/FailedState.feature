@redis
Feature: Failed state

Background: 
    Given a job
      And the Failed state

Scenario: State name is 'Failed'
     Then the state name is equal to 'Failed'

Scenario: It has the correct properties set
     Then properties table contains the following items:
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