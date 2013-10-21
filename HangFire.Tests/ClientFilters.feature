@redis
Feature: Client filters

Background:
    Given a client

Scenario: Client filters are executed when I create a job
    Given the client filter 'test'
     When I create a job
     Then the client filter methods were executed in the following order:
          | Method           |
          | test::OnCreating |
          | test::OnCreated  |
      And the storage contains the job

Scenario: Multiple client filters are executed depending on their order
    Given the client filter 'first'
      And the client filter 'second'
     When I create a job
     Then the client filter methods were executed in the following order:
          | Method             |
          | first::OnCreating  |
          | second::OnCreating |
          | second::OnCreated  |
          | first::OnCreated   |

Scenario: When client filter can cancel the creation of the job
    Given the client filter 'first'
      And the client filter 'second' that cancels the job
      And the client filter 'third'
     When I create a job
     Then the storage does not contain the job
      And only the following client filter methods were executed:
          | Method                                        |
          | first::OnCreating                             |
          | second::OnCreating                            |
          | first::OnCreated (with the canceled flag set) |

Scenario: Client filter's OnCreated could be skipped if there was an exception
    Given the client filter 'first'
      And the client filter 'second' that throws an exception
     When I create a job
     Then only the following client filter methods were executed:
          | Method             |
          | first::OnCreating  |
          | second::OnCreating |
          | first::OnCreated   |

Scenario: Client filter can handle the exception
    Given the client filter 'first'
      And the client filter 'second' that handles an exception
      And the client filter 'third' that throws an exception
     When I create a job
     Then the client filter methods were executed in the following order:
          | Method             |
          | first::OnCreating  |
          | second::OnCreating |
          | third::OnCreating  |
          | second::OnCreated  |
          | first::OnCreated   |
      And no exception were thrown

Scenario: Client exception filters are executed when there was an exception while creating a job
    Given the exception filter 'test'
     When there is a buggy filter (for example)
      And I create a job
     Then the client exception filter was executed
      And a 'System.Exception' was thrown

Scenario: Multiple exception filters are executed depending on their order
    Given the exception filter 'first'
      And the exception filter 'second'
     When there is a buggy filter (for example)
      And I create a job
     Then the client exception filters were executed in the following order:
          | Filter |
          | first  |
          | second |
      And a 'System.Exception' was thrown

Scenario: Exception filter can handle the exception
    Given the exception filter 'first'
      And the exception filter 'second' that handles an exception
      And the exception filter 'third'
     When there is a buggy filter (for example)
      And I create a job
     Then the following exceptions filter were executed:
          | Filter |
          | first  |
          | second |
          | third  |
      And no exception were thrown