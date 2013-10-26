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

Scenario: I should be able to set a job parameter in the client filter
    Given the client filter 'first' that sets the following parameters in the OnCreating method:
          | Name      | Value |
          | Culture   | en-US |
          | UICulture | ru-RU |
     When I create a job
     Then it should have all of the above parameters encoded as JSON string

Scenario: When I specify an empty or null parameter name, an exception should be thrown
    Given the client filter 'first' that sets the following parameters in the OnCreating method:
          | Name | Value |
          |      | en-US |
     When I create a job
     Then the CreateJobFailedException should be thrown by the client

Scenario: Client filter should be able to read the parameters that were set by one of the previous filters
    Given the client filter 'first' that sets the following parameters in the OnCreating method:
          | Name    | Value |
          | Culture | en-GB |
      And the client filter 'second' that reads all of the above parameters
     When I create a job
     Then the 'second' client filter got the actual values of the parameters

Scenario: When I specify an empty or null parameter name in the GetParameter method call, an exception should be thrown
    Given the client filter 'first' that gets the following parameters in the OnCreating method:
          | Name | Value |
          |      | en-GB |
     When I create a job
     Then the CreateJobFailedException should be thrown by the client

Scenario: I should not be able to set parameters after the job was created
    Given the client filter 'first' that sets the following parameters in the OnCreated method:
          | Name    | Value |
          | Culture | en-US |
     When I create a job
     Then the CreateJobFailedException should be thrown by the client

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