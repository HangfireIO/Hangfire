@redis
Feature: State machine

Scenario: The state of the job should be changed
    Given a 'Test' state
      And a job
     When I change the state of the job to the 'Test'
     Then the job state should be changed to 'Test'

Scenario: The new state should be applied
    Given a 'Test' state
      And a job
     When I change the state of the job to the 'Test'
     Then the 'Test' state should be applied to the job

Scenario: An old state should be unapplied
    Given a 'Test' state
      And a job in the 'Old' state with registered descriptor
     When I change the state of the job to the 'Test'
     Then the old state should be unapplied

Scenario: The state entry should contain the state name
    Given a 'Test' state
      And a job
     When I change the state of the job to the 'Test'
     Then the job's state entry should contain the following items:
          | Name  | Value |
          | State | Test  |

Scenario: The job history should be appended
    Given a 'Test' state
      And a job
     When I change the state of the job to the 'Test'
     Then the last history entry should contain the following items:
          | Name      | Value              |
          | CreatedAt | <UtcNow timestamp> |
          | State     | Test               |
          | Reason    | SomeReason         |

Scenario: the job history record and the state entry should contain the state properties
    Given a 'Test' state with the following properties:
          | Name   | Value      |
          | Server | TestServer |
          | Worker | #3         |
      And a job
     When I change the state of the job to the 'Test'
     Then the last history entry should contain all of the above properties
      And the state entry should contain all of the above properties

Scenario: the state should be changed to the Failed state when it could not find its type
    Given a job of the 'NonExisting' type
      And a 'Test' state
     When I change the state of the job to the 'Test'
     Then the job should be moved to the Failed state

Scenario: the state should not be changed if allowed current states array does not contain the current state
    Given a 'Test' state
      And a job in the 'Old' state with registered descriptor
     When I change the state of the job to the 'Test' allowing only transition from the 'Enqueued' state
     Then the job should be in the Old state
      And the old state should not be unapplied
      And the 'Test' state should not be applied to the job

Scenario: State changing filters should be executed in the order they were defined
    Given a job
      And a 'Test' state
      And a state changing filter 'first'
      And a state changing filter 'second'
     When I change the state of the job to the 'Test'
     Then changing filters should be executed in the following order:
          | Filter |
          | first  |
          | second |

Scenario: The state changing filters should be able to modify the state
    Given a job
      And a 'Test' state
      And a state changing filter 'first' that changes the state to the 'AnotherTest'
     When I change the state of the job to the 'Test'
     Then the job state should be changed to 'AnotherTest'
      And the 'Test' state should not be applied to the job
      And the 'AnotherTest' state should be applied to the job
      And the job's state entry should contain the following items:
          | Name  | Value       |
          | State | AnotherTest |

Scenario: The job history should contain every changed state
    Given a job
      And a 'Test' state
      And a state changing filter 'first' that changes the state to the 'AnotherTest'
      And a state changing filter 'second' that changes the state to the 'YetAnotherTest'
     When I change the state of the job to the 'Test'
     Then the history for the following states should be added:
          | State          |
          | YetAnotherTest |
          | AnotherTest    |
          | Test           |

Scenario: The state of the unexisting job should not be changed
    Given a 'Test' state
     When I change the state of the 'unexisting' job to the 'Test'
     Then the 'Test' state should not be applied to the job

Scenario: Changing filters are should not be executed for unexisting job
    Given a 'Test' state
      And a state changing filter 'first'
     When I change the state of the 'unexisting' job to the 'Test'
     Then changing filters should not be executed

Scenario: Changing filters should not be executed if the transition is not allowed for the current state
    Given a 'Test' state
      And a job in the 'Old' state with registered descriptor
      And a state changing filter 'first'
     When I change the state of the job to the 'Test' allowing only transition from the 'Enqueued' state
     Then changing filters should not be executed

Scenario: State applied filters should be executed in the order they were defined
    Given a 'Test' state
      And a job in the 'Old' state with registered descriptor
      And a state applied filter 'first'
      And a state applied filter 'second'
     When I change the state of the job to the 'Test'
     Then state applied filter methods should be executed in the following order:
          | Method                   |
          | first::OnStateUnapplied  |
          | second::OnStateUnapplied |
          | first::OnStateApplied    |
          | second::OnStateApplied   |

Scenario: OnStateUnapplied method should not be called when the state is empty
    Given a 'Test' state
      And a job with empty state
      And a state applied filter 'first'
     When I change the state of the job to the 'Test'
     Then state applied filter methods should be executed in the following order:
          | Method                |
          | first::OnStateApplied |