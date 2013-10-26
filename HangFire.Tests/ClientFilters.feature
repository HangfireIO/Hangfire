@redis
Feature: Client filters

    As a user, I expect that all defined client filters are executing
    using the following rules, when I create a job. 

Background:
    Given a client
    
Scenario: Client filters should be executed when I create a job
    Given the client filter 'test'
     When I create a job
     Then the client filter methods should be executed in the following order:
          | Method           |
          | test::OnCreating |
          | test::OnCreated  |
      And the storage should contain the job

Scenario: Multiple client filters should be executed depending on their order
    Given the client filter 'first'
      And the client filter 'second'
     When I create a job
     Then the client filter methods should be executed in the following order:
          | Method             |
          | first::OnCreating  |
          | second::OnCreating |
          | second::OnCreated  |
          | first::OnCreated   |

Scenario: When client filter cancels the creation of the job, it should not be created
    Given the client filter 'first'
      And the client filter 'second' that cancels the job
      And the client filter 'third'
     When I create a job
     Then the storage should not contain the job
      And only the following client filter methods should be executed:
          | Method                                        |
          | first::OnCreating                             |
          | second::OnCreating                            |
          | first::OnCreated (with the canceled flag set) |

Scenario: Client filter's OnCreated could be skipped if there was an exception
    Given the client filter 'first'
      And the client filter 'second' that throws an exception
     When I create a job
     Then only the following client filter methods should be executed:
          | Method             |
          | first::OnCreating  |
          | second::OnCreating |
          | first::OnCreated   |

Scenario: When a client filter handles an exception, it should not be thrown outside
    Given the client filter 'first'
      And the client filter 'second' that handles an exception
      And the client filter 'third' that throws an exception
     When I create a job
     Then the client filter methods should be executed in the following order:
          | Method             |
          | first::OnCreating  |
          | second::OnCreating |
          | third::OnCreating  |
          | second::OnCreated  |
          | first::OnCreated   |
      And an exception should not be thrown by the client

Scenario: Client exception filters should be executed when there was an exception while creating a job
    Given the exception filter 'test'
     When there is a buggy filter (for example)
      And I create a job
     Then the client exception filter should be executed
      And the CreateJobFailedException should be thrown by the client

Scenario: Multiple exception filters should be executed depending on their order
    Given the exception filter 'first'
      And the exception filter 'second'
     When there is a buggy filter (for example)
      And I create a job
     Then the client exception filters should be executed in the following order:
          | Filter |
          | first  |
          | second |
      And the CreateJobFailedException should be thrown by the client

Scenario: When a client exception filter handles an exception, it should not be thrown outside
    Given the exception filter 'first'
      And the exception filter 'second' that handles an exception
      And the exception filter 'third'
     When there is a buggy filter (for example)
      And I create a job
     Then the following client exception filters should be executed:
          | Filter |
          | first  |
          | second |
          | third  |
      And an exception should not be thrown by the client