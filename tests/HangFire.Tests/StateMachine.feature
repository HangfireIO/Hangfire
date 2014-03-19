@redis
Feature: State machine

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