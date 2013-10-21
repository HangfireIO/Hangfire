@redis
Feature: State machine

Scenario: The job's state should be changed after applying the state
    Given a job
      And a 'Test' state
     When I apply the state
     Then the job state is changed to 'Test'

Scenario: State's apply method is being called while applying the state
    Given a 'Test' state
     When I apply the state
     Then Apply method has called

Scenario: The job's state entry should contain the corresponding values after applying the state
    Given a job
      And a 'Test' state with the following properties:
          | Name      | Value  |
          | Property1 | Value1 |
          | Property2 | Value2 |
     When I apply the state
     Then the job's state entry contains the following items:
          | Name      | Value  |
          | State     | Test   |
          | Property1 | Value1 |
          | Property2 | Value2 |

Scenario: The job's history list should contain the history entry after applying the state
    Given a job
      And a 'Test' state with the following properties:
          | Name     | Value |
          | Property | Value |
     When I apply the state
     Then the last history entry contains the following items:
          | Name      | Value              |
          | State     | Test               |
          | Reason    | SomeReason         |
          | CreatedAt | <UtcNow timestamp> |
          | Property  | Value              |